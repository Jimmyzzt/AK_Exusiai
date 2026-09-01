using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using AK_Exusiai.Powers;
using AK_Exusiai.Relics;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace AK_Exusiai.Characters;

[RegisterCharacter]
public sealed class Exusiai :
    ModCharacterTemplate<ExusiaiCardPool, ExusiaiRelicPool, ExusiaiPotionPool>,
    ISecondaryResourceHookListener
{
    private const string CharacterScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_visuals.tscn";
    private const string MerchantScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_merchant.tscn";
    private const string RestSiteScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_rest_site.tscn";
    private const string CharacterSelectBgScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_character_select_bg.tscn";

    private static readonly ConditionalWeakTable<Exusiai, AmmoData> AmmoDataByCharacter = new();

    private sealed class AmmoData
    {
        public Dictionary<CardPlay, AmmoAttackInfo> AttackModes { get; } = [];
        public Stack<PrepaidAmmoInfo> PrepaidAmmo { get; } = [];
    }

    private readonly record struct PrepaidAmmoInfo(int AmmoSpent, decimal DamagePerAmmo);

    public override CharacterGender Gender => CharacterGender.Feminine;
    public override Color NameColor => new("F04B61");
    public override int StartingHp => 77;
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.35f;
    public override float CastAnimDelay => 0.35f;
    public override bool ShouldReceiveCombatHooks => true;
    public override bool RequiresEpochAndTimeline => false;
    public override string? PlaceholderCharacterId => "IRONCLAD";

    public override Color EnergyLabelOutlineColor => new("6E1722FF");
    public override Color DialogueColor => new("55222A");
    public override VfxColor SpeechBubbleColor => VfxColor.Red;
    public override Color MapDrawingColor => new("CF080B");
    public override Color RemoteTargetingLineColor => new("FF788A");
    public override Color RemoteTargetingLineOutline => new("6E1722FF");

    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            VisualsPath: CharacterScenePath,
            MerchantAnimPath: MerchantScenePath,
            RestSiteAnimPath: RestSiteScenePath),
        Ui: new CharacterUiAssetSet(
            IconTexturePath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            IconOutlineTexturePath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            IconPath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            CharacterSelectBgPath: CharacterSelectBgScenePath,
            CharacterSelectIconPath: $"{Entry.ResPath}/images/character/exusiai_select_icon.jpg",
            MapMarkerPath: $"{Entry.ResPath}/images/character/exusiai_icon.png"),
        Multiplayer: new CharacterMultiplayerAssetSet(
            ArmPointingTexturePath: $"{Entry.ResPath}/images/character/Exusiai_multiplayer_hand_point.png",
            ArmRockTexturePath: $"{Entry.ResPath}/images/character/Exusiai_multiplayer_hand_rock.png",
            ArmPaperTexturePath: $"{Entry.ResPath}/images/character/Exusiai_multiplayer_hand_paper.png",
            ArmScissorsTexturePath: $"{Entry.ResPath}/images/character/Exusiai_multiplayer_hand_scissors.png"));

    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        return ExusiaiAppearanceManager.IsNewCovenant
            ? SetupNewCovenantAnimator(controller)
            : SetupExusiaiAnimator(controller);
    }

    private static CreatureAnimator SetupExusiaiAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState start = new("Start");
        AnimState attack = new("Attack");
        AnimState cast = new("Attack");
        AnimState hit = new("Idle");
        AnimState dead = new("Die");
        AnimState relaxed = new("Idle", isLooping: true);

        start.NextState = idle;
        attack.NextState = idle;
        cast.NextState = idle;
        hit.NextState = idle;
        relaxed.AddBranch("Idle", idle);

        CreatureAnimator animator = new(start, controller);
        animator.AddAnyState("Start", start);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }

    private static CreatureAnimator SetupNewCovenantAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState start = new("Start");
        AnimState attack = new("Attack_Loop");
        AnimState cast = new("Skill_3_Skill");
        AnimState hit = new("Idle");
        AnimState dead = new("Die");
        AnimState relaxed = new("Idle", isLooping: true);

        start.NextState = idle;
        attack.NextState = idle;
        cast.NextState = idle;
        hit.NextState = idle;
        relaxed.AddBranch("Idle", idle);

        CreatureAnimator animator = new(start, controller);
        animator.AddAnyState("Start", start);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Relaxed", relaxed);
        return animator;
    }

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        NCreatureVisuals? visuals =
            RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            CharacterScenePath);
        if (visuals is not null)
            ExusiaiAppearanceManager.ApplyCombatSkin(visuals);

        return visuals;
    }

    protected override ModAnimStateMachine? SetupCustomMerchantAnimationStateMachine(
        Node merchantRoot,
        CharacterModel character)
    {
        // The merchant scene is Spine-driven by the merchant compatibility patches.
        // Returning a second RitsuLib state machine would make both systems own
        // the same track and can leave a queued loop alive while abandoning a run.
        return null;
    }

    protected override ModAnimStateMachine? SetupCustomRestSiteAnimationStateMachine(
        Node restSiteRoot,
        CharacterModel character)
    {
        return ModAnimStateMachines.StandardRestSiteCue(
            restSiteRoot,
            character,
            idleName: "Sit",
            relaxedName: "Sit");
    }

    public override Task BeforeCombatStart()
    {
        GetAmmoData().AttackModes.Clear();
        GetAmmoData().PrepaidAmmo.Clear();
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

        Player? player = participants
            .Select(creature => creature.Player)
            .FirstOrDefault(player => player?.Character == this);
        if (player == null ||
            player.Creature.HasPower<OverloadPower>() ||
            SecondaryResourceCmd.Get(player, AmmoResource.Id) < AmmoResource.MaxAmount)
        {
            return;
        }

        await PowerCmd.Apply<OverloadPower>(
            choiceContext,
            player.Creature,
            1m,
            player.Creature,
            null);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Player.Character is not Exusiai || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;

        AmmoData data = GetAmmoData();
        data.AttackModes.Remove(cardPlay);
        if (data.PrepaidAmmo.TryPeek(out PrepaidAmmoInfo prepaid))
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(
                AmmoAttackMode.Prepaid,
                prepaid.DamagePerAmmo * GetCardAmmoDamageMultiplier(cardPlay.Card) * prepaid.AmmoSpent,
                spendLimit: 0);
            return Task.CompletedTask;
        }

        if (cardPlay.Card is IAmmoFreeAttack)
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(AmmoAttackMode.Free, 0m, spendLimit: 0);
            return Task.CompletedTask;
        }

        if (cardPlay.Player.Creature.HasPower<OverloadPower>())
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(
                AmmoAttackMode.Overloaded,
                GetAmmoDamagePerAmmo(cardPlay.Player, cardPlay.Card),
                spendLimit: AmmoResource.MaxAmount);
            return Task.CompletedTask;
        }

        int availableAmmo = SecondaryResourceCmd.Get(cardPlay.Player, AmmoResource.Id);
        if (availableAmmo > 0)
            data.AttackModes[cardPlay] = new AmmoAttackInfo(
                AmmoAttackMode.Paid,
                GetAmmoDamagePerAmmo(cardPlay.Player, cardPlay.Card),
                spendLimit: availableAmmo);

        return Task.CompletedTask;
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker?.Player?.Character != this ||
            command.CardPlay is not { Card.Type: CardType.Attack } cardPlay ||
            !command.DamageProps.IsPoweredAttack())
        {
            return Task.CompletedTask;
        }

        if (!GetAmmoData().AttackModes.ContainsKey(cardPlay))
        {
            int availableAmmo = SecondaryResourceCmd.Get(cardPlay.Player, AmmoResource.Id);
            if (cardPlay.Player.Creature.HasPower<OverloadPower>() &&
                cardPlay.Card is not IAmmoFreeAttack)
            {
                GetAmmoData().AttackModes[cardPlay] = new AmmoAttackInfo(
                    AmmoAttackMode.Overloaded,
                    GetAmmoDamagePerAmmo(cardPlay.Player, cardPlay.Card),
                    spendLimit: AmmoResource.MaxAmount);
            }
            else if (availableAmmo > 0 && cardPlay.Card is not IAmmoFreeAttack)
            {
                GetAmmoData().AttackModes[cardPlay] = new AmmoAttackInfo(
                    AmmoAttackMode.Paid,
                    GetAmmoDamagePerAmmo(cardPlay.Player, cardPlay.Card),
                    spendLimit: availableAmmo);
            }
        }

        command.BeforeDamage(() => ApplyAmmoForNextDamageInstance(command));
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        GetAmmoData().AttackModes.Remove(cardPlay);
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer?.Player?.Character is not Exusiai ||
            cardSource?.Type != CardType.Attack ||
            !props.IsPoweredAttack())
        {
            return 0m;
        }

        if (cardPlay != null)
        {
            if (!GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
                return 0m;

            return info.CurrentHitAmmoDamage;
        }

        return PreviewAmmoDamage(dealer.Player, cardSource);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer?.Player?.Character != this ||
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

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player)
            return;

        Player? player = participants
            .Select(creature => creature.Player)
            .FirstOrDefault(player => player?.Character == this);
        if (player == null || !player.Creature.HasPower<OverloadPower>())
            return;

        AmmoData data = GetAmmoData();
        bool retainAmmo = player.Creature.HasPower<OverloadAmmoRetentionPower>();
        if (!retainAmmo)
            await SecondaryResourceCmd.Set(player, AmmoResource.Id, 0, this);

        if (player.Creature.GetPower<OverloadAmmoRetentionPower>() is { } retention)
            await PowerCmd.Remove(retention);
        if (player.Creature.GetPower<OverloadPower>() is { } overload)
            await PowerCmd.Remove(overload);

        data.AttackModes.Clear();
        data.PrepaidAmmo.Clear();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        GetAmmoData().AttackModes.Clear();
        GetAmmoData().PrepaidAmmo.Clear();
        return Task.CompletedTask;
    }

    public bool ShouldGainSecondaryResource(SecondaryResourceContext context, decimal amount)
    {
        return context.Definition.Id != AmmoResource.Id ||
               context.Player.Character != this ||
               !context.Player.Creature.HasPower<OverloadPower>();
    }

    public async Task AfterSecondaryResourceChanged(SecondaryResourceChangeContext context)
    {
        if (context.Definition.Id != AmmoResource.Id ||
            context.Player.Character != this ||
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

    public static bool DidSpendAmmo(CardPlay cardPlay)
    {
        return cardPlay.Player.Character is Exusiai exusiai &&
               exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info) &&
               info.AmmoSpent > 0;
    }

    public static decimal GetAmmoBonus(CardPlay? cardPlay)
    {
        if (cardPlay?.Player.Character is not Exusiai exusiai ||
            !exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
        {
            return 0m;
        }

        return info.TotalAmmoDamage;
    }

    public static IReadOnlyList<decimal> GetAmmoHitBonuses(CardPlay? cardPlay)
    {
        if (cardPlay?.Player.Character is not Exusiai exusiai ||
            !exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
        {
            return [];
        }

        return info.HitAmmoDamage;
    }

    public static int GetAmmoMultiplier(CardPlay? cardPlay)
    {
        if (cardPlay?.Player.Character is not Exusiai exusiai ||
            !exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
        {
            return 0;
        }

        return info.AmmoSpent;
    }

    public static int GetAmmoSpent(CardPlay? cardPlay)
    {
        if (cardPlay?.Player.Character is not Exusiai exusiai ||
            !exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
        {
            return 0;
        }

        return info.AmmoSpent;
    }

    public static bool HasAmmoBackedBonus(CardPlay? cardPlay)
    {
        return cardPlay?.Player.Character is Exusiai exusiai &&
               exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info) &&
               info.TotalAmmoDamage > 0m;
    }

    public static decimal GetAmmoDamagePerAmmo(Player player, CardModel? cardSource = null)
    {
        return AmmoResource.GetCurrentDamageBreakdown(
            player,
            GetCardAmmoDamageMultiplier(cardSource)).DamagePerAmmo;
    }

    public static IDisposable BeginPrepaidAmmo(Player player, int ammoSpent, decimal damagePerAmmo)
    {
        if (player.Character is not Exusiai exusiai)
            return EmptyScope.Instance;

        AmmoData data = exusiai.GetAmmoData();
        data.PrepaidAmmo.Push(new PrepaidAmmoInfo(Math.Max(0, ammoSpent), Math.Max(0m, damagePerAmmo)));
        return new PrepaidAmmoScope(data);
    }

    public static async Task EnterOverload(
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
            await PowerCmd.Apply<OverloadAmmoRetentionPower>(
                choiceContext,
                player.Creature,
                1m,
                player.Creature,
                source as CardModel);
    }

    private async Task ApplyAmmoForNextDamageInstance(AttackCommand command)
    {
        CardPlay? cardPlay = command.CardPlay;
        if (cardPlay == null ||
            !GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo? info))
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
            await info.TrySpendAllForHit(cardPlay.Player, AmmoResource.Id, cardPlay.Card, this);
        else
            await info.TrySpendForHit(cardPlay.Player, AmmoResource.Id, cardPlay.Card, this);
    }

    private static decimal PreviewAmmoDamage(Player player, CardModel? cardSource)
    {
        if (player.Character is Exusiai exusiai &&
            exusiai.GetAmmoData().PrepaidAmmo.TryPeek(out PrepaidAmmoInfo prepaid))
        {
            return prepaid.DamagePerAmmo *
                   GetCardAmmoDamageMultiplier(cardSource) *
                   prepaid.AmmoSpent;
        }

        if (cardSource is IAmmoFreeAttack ||
            SecondaryResourceCmd.Get(player, AmmoResource.Id) <= 0)
        {
            return 0m;
        }

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

    private AmmoData GetAmmoData()
    {
        return AmmoDataByCharacter.GetOrCreateValue(this);
    }

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

    private sealed class EmptyScope : IDisposable
    {
        public static EmptyScope Instance { get; } = new();
        public void Dispose()
        {
        }
    }

    public override List<string> GetArchitectAttackVfx()
    {
        return ["vfx/vfx_attack_slash"];
    }
}
