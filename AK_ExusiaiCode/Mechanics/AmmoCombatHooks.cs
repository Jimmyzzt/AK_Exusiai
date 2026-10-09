using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using AK_Exusiai.Powers;
using AK_Exusiai.Relics;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Mechanics;

/// <summary>
/// Owns ammo combat rules independently of the selected character. This is
/// required because events can give Exusiai cards to other characters.
/// </summary>
[RegisterSingleton]
internal sealed class AmmoCombatHooks : SingletonModel, ISecondaryResourceHookListener
{
    private static readonly ConditionalWeakTable<Player, AmmoData> AmmoDataByPlayer = new();

    private sealed class AmmoData
    {
        public Dictionary<CardPlay, AmmoAttackInfo> AttackModes { get; } = [];
        public Stack<PrepaidAmmoInfo> PrepaidAmmo { get; } = [];
    }

    private readonly record struct PrepaidAmmoInfo(int AmmoSpent, decimal DamagePerAmmo);

    public override bool ShouldReceiveCombatHooks => true;

    public override Task BeforeCombatStart()
    {
        AmmoDataByPlayer.Clear();
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player)
            return;

        foreach (Player player in participants
                     .Select(creature => creature.Player)
                     .OfType<Player>()
                     .Distinct())
        {
            if (player.Creature.HasPower<OverloadPower>() ||
                SecondaryResourceCmd.Get(player, AmmoResource.Id) < AmmoResource.MaxAmount)
            {
                continue;
            }

            await PowerCmd.Apply<OverloadPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                null);
        }
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Player.Character is Characters.Exusiai)
            await Effects.CardEffectRuntime.BeforeCard(cardPlay);

        PrepareAmmoCard(cardPlay);
    }

    private static void PrepareAmmoCard(CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Attack)
            return;

        AmmoData data = GetAmmoData(cardPlay.Player);
        data.AttackModes.Remove(cardPlay);
        if (data.PrepaidAmmo.TryPeek(out PrepaidAmmoInfo prepaid))
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(
                AmmoAttackMode.Prepaid,
                prepaid.DamagePerAmmo * GetCardAmmoDamageMultiplier(cardPlay.Card) * prepaid.AmmoSpent,
                spendLimit: 0);
            return;
        }

        if (cardPlay.Card is IAmmoFreeAttack)
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(AmmoAttackMode.Free, 0m, spendLimit: 0);
            return;
        }

        if (cardPlay.Player.Creature.HasPower<OverloadPower>())
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(
                AmmoAttackMode.Overloaded,
                GetAmmoDamagePerAmmo(cardPlay.Player, cardPlay.Card),
                spendLimit: AmmoResource.MaxAmount);
            return;
        }

        int availableAmmo = SecondaryResourceCmd.Get(cardPlay.Player, AmmoResource.Id);
        if (availableAmmo > 0)
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(
                AmmoAttackMode.Paid,
                GetAmmoDamagePerAmmo(cardPlay.Player, cardPlay.Card),
                spendLimit: availableAmmo);
        }
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker?.Player is not { } player ||
            command.CardPlay is not { Card.Type: CardType.Attack } cardPlay ||
            !command.DamageProps.IsPoweredAttack())
        {
            return Task.CompletedTask;
        }

        AmmoData data = GetAmmoData(player);
        if (!data.AttackModes.ContainsKey(cardPlay))
        {
            int availableAmmo = SecondaryResourceCmd.Get(player, AmmoResource.Id);
            if (player.Creature.HasPower<OverloadPower>() && cardPlay.Card is not IAmmoFreeAttack)
            {
                data.AttackModes[cardPlay] = new AmmoAttackInfo(
                    AmmoAttackMode.Overloaded,
                    GetAmmoDamagePerAmmo(player, cardPlay.Card),
                    spendLimit: AmmoResource.MaxAmount);
            }
            else if (availableAmmo > 0 && cardPlay.Card is not IAmmoFreeAttack)
            {
                data.AttackModes[cardPlay] = new AmmoAttackInfo(
                    AmmoAttackMode.Paid,
                    GetAmmoDamagePerAmmo(player, cardPlay.Card),
                    spendLimit: availableAmmo);
            }
        }

        command.BeforeDamage(() => ApplyAmmoForNextDamageInstance(command));
        if (player.Character is Characters.Exusiai)
            Effects.CardEffectRuntime.Configure(command);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        GetAmmoData(cardPlay.Player).AttackModes.Remove(cardPlay);
        if (cardPlay.Player.Character is Characters.Exusiai)
            await Effects.CardEffectRuntime.AfterCard(cardPlay);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer?.Player is not { } player ||
            cardSource?.Type != CardType.Attack ||
            !props.IsPoweredAttack())
        {
            return 0m;
        }

        if (cardPlay != null)
        {
            return GetAmmoData(player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info)
                ? info.CurrentHitAmmoDamage
                : 0m;
        }

        return PreviewAmmoDamage(player, cardSource);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer?.Player is null ||
            cardSource?.Type != CardType.Attack ||
            !props.IsPoweredAttack() ||
            !dealer.HasPower<OverloadPower>())
        {
            return 1m;
        }

        decimal bonus = 0.5m + dealer.Powers
            .OfType<FlammableAndExplosivePower>()
            .Sum(power => power.Amount / 100m);
        return 1m + bonus;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AmmoDataByPlayer.Clear();
        return Task.CompletedTask;
    }

    public bool ShouldGainSecondaryResource(SecondaryResourceContext context, decimal amount)
    {
        return context.Definition.Id != AmmoResource.Id ||
               !context.Player.Creature.HasPower<OverloadPower>();
    }

    public async Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
    {
        if (context.Definition.Id != AmmoResource.Id ||
            context.Delta <= 0 ||
            context.NewAmount < AmmoResource.MaxAmount ||
            context.Player.Creature.HasPower<OverloadPower>())
        {
            return;
        }

        await PowerCmd.Apply<OverloadPower>(
            new BlockingPlayerChoiceContext(),
            context.Player.Creature,
            1m,
            context.Player.Creature,
            null);
    }

    internal static bool DidSpendAmmo(CardPlay cardPlay) => GetEffectiveAmmoSpent(cardPlay) > 0;

    internal static int GetEffectiveAmmoSpent(CardPlay? cardPlay)
    {
        if (cardPlay == null ||
            !GetAmmoData(cardPlay.Player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
        {
            return 0;
        }

        return info.Mode == AmmoAttackMode.Overloaded
            ? info.AmmoBackedHitCount
            : info.AmmoSpent;
    }

    internal static decimal GetAmmoBonus(CardPlay? cardPlay)
    {
        return cardPlay != null &&
               GetAmmoData(cardPlay.Player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info)
            ? info.TotalAmmoDamage
            : 0m;
    }

    internal static IReadOnlyList<decimal> GetAmmoHitBonuses(CardPlay? cardPlay)
    {
        return cardPlay != null &&
               GetAmmoData(cardPlay.Player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info)
            ? info.HitAmmoDamage
            : [];
    }

    internal static int GetAmmoMultiplier(CardPlay? cardPlay)
    {
        return cardPlay != null &&
               GetAmmoData(cardPlay.Player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info)
            ? info.AmmoSpent
            : 0;
    }

    internal static int GetAmmoSpent(CardPlay? cardPlay) => GetAmmoMultiplier(cardPlay);

    internal static bool HasAmmoBackedBonus(CardPlay? cardPlay)
    {
        return cardPlay != null &&
               GetAmmoData(cardPlay.Player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info) &&
               info.TotalAmmoDamage > 0m;
    }

    internal static bool UsedOverloadAmmoBonus(CardPlay? cardPlay)
    {
        return cardPlay != null &&
               GetAmmoData(cardPlay.Player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info) &&
               info.Mode == AmmoAttackMode.Overloaded &&
               info.TotalAmmoDamage > 0m;
    }

    internal static decimal GetAmmoDamagePerAmmo(Player player, CardModel? cardSource = null)
    {
        return AmmoResource.GetCurrentDamageBreakdown(
            player,
            GetCardAmmoDamageMultiplier(cardSource)).DamagePerAmmo;
    }

    internal static IDisposable BeginPrepaidAmmo(Player player, int ammoSpent, decimal damagePerAmmo)
    {
        AmmoData data = GetAmmoData(player);
        data.PrepaidAmmo.Push(new PrepaidAmmoInfo(Math.Max(0, ammoSpent), Math.Max(0m, damagePerAmmo)));
        return new PrepaidAmmoScope(data);
    }

    internal static async Task EnterOverload(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel source,
        bool retainAmmoAtTurnEnd = false)
    {
        await SecondaryResourceCmd.Set(player, AmmoResource.Id, AmmoResource.MaxAmount, source);

        if (!player.Creature.HasPower<OverloadPower>())
        {
            await PowerCmd.Apply<OverloadPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                source as CardModel);
        }

        if (retainAmmoAtTurnEnd)
        {
            await PowerCmd.Apply<OverloadAmmoRetentionPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                source as CardModel);
        }
    }

    internal static void ClearAmmoTransientState(Player player)
    {
        AmmoData data = GetAmmoData(player);
        data.AttackModes.Clear();
        data.PrepaidAmmo.Clear();
    }

    private static async Task ApplyAmmoForNextDamageInstance(AttackCommand command)
    {
        CardPlay? cardPlay = command.CardPlay;
        if (cardPlay == null ||
            !GetAmmoData(cardPlay.Player).AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
        {
            return;
        }

        info.ClearCurrentHit();
        if (info.Mode == AmmoAttackMode.Free)
            return;

        if (info.Mode == AmmoAttackMode.Prepaid)
        {
            info.TryApplyPrepaidHit();
            return;
        }

        if (cardPlay.Card is IAmmoSpendAllAttack)
            await info.TrySpendAllForHit(cardPlay.Player, AmmoResource.Id, cardPlay.Card, ModelDb.Singleton<AmmoCombatHooks>());
        else
            await info.TrySpendForHit(cardPlay.Player, AmmoResource.Id, cardPlay.Card, ModelDb.Singleton<AmmoCombatHooks>());

        if (info.Mode == AmmoAttackMode.Overloaded && info.CurrentHitLogicalAmmoSpent > 0)
            await NotifyOverloadAmmoSpent(cardPlay.Player, info.CurrentHitLogicalAmmoSpent);
    }

    internal static async Task NotifyOverloadAmmoSpent(Player player, int amount)
    {
        if (amount <= 0 || !player.Creature.HasPower<OverloadPower>())
            return;

        foreach (IOverloadAmmoSpendListener listener in player.Creature.Powers
                     .OfType<IOverloadAmmoSpendListener>()
                     .ToArray())
        {
            await listener.AfterOverloadAmmoSpent(amount);
        }

        foreach (IOverloadAmmoSpendListener listener in player.Relics
                     .Where(RelicLogisticsCmd.IsOperational)
                     .OfType<IOverloadAmmoSpendListener>()
                     .ToArray())
        {
            await listener.AfterOverloadAmmoSpent(amount);
        }
    }

    private static decimal PreviewAmmoDamage(Player player, CardModel? cardSource)
    {
        if (GetAmmoData(player).PrepaidAmmo.TryPeek(out PrepaidAmmoInfo prepaid))
        {
            return prepaid.DamagePerAmmo *
                   GetCardAmmoDamageMultiplier(cardSource) *
                   prepaid.AmmoSpent;
        }

        if (cardSource is IAmmoFreeAttack || SecondaryResourceCmd.Get(player, AmmoResource.Id) <= 0)
            return 0m;

        decimal perAmmo = GetAmmoDamagePerAmmo(player, cardSource);
        return cardSource is IAmmoSpendAllAttack
            ? perAmmo * SecondaryResourceCmd.Get(player, AmmoResource.Id)
            : perAmmo;
    }

    private static decimal GetCardAmmoDamageMultiplier(CardModel? cardSource)
    {
        return cardSource is IAmmoDamageMultiplier cardBonus
            ? Math.Max(0m, cardBonus.AmmoDamageMultiplier)
            : 1m;
    }

    private static AmmoData GetAmmoData(Player player) => AmmoDataByPlayer.GetOrCreateValue(player);

    private sealed class PrepaidAmmoScope(AmmoData data) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (data.PrepaidAmmo.Count > 0)
                data.PrepaidAmmo.Pop();
        }
    }
}
