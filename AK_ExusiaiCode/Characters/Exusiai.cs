using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using AK_Exusiai.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace AK_Exusiai.Characters;

[RegisterCharacter]
public sealed class Exusiai : ModCharacterTemplate<ExusiaiCardPool, ExusiaiRelicPool, ExusiaiPotionPool>
{
    private static readonly ConditionalWeakTable<Exusiai, AmmoData> AmmoDataByCharacter = new();

    private sealed class AmmoData
    {
        public Dictionary<CardPlay, AmmoAttackInfo> AttackModes { get; } = [];
        public Stack<int> PrepaidMultipliers { get; } = [];
    }

    public override CharacterGender Gender => CharacterGender.Feminine;
    public override Color NameColor => new("F04B61");
    public override int StartingHp => 77;
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.2f;
    public override bool ShouldReceiveCombatHooks => true;
    public override bool RequiresEpochAndTimeline => false;
    public override string? PlaceholderCharacterId => "IRONCLAD";

    public override Color EnergyLabelOutlineColor => new("6E1722FF");
    public override Color DialogueColor => new("55222A");
    public override VfxColor SpeechBubbleColor => VfxColor.Red;
    public override Color MapDrawingColor => new("E95369");
    public override Color RemoteTargetingLineColor => new("FF788A");
    public override Color RemoteTargetingLineOutline => new("6E1722FF");

    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            VisualsPath: $"{Entry.ResPath}/scenes/character/exusiai_visuals.tscn"),
        Ui: new CharacterUiAssetSet(
            IconTexturePath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            IconOutlineTexturePath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            IconPath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            CharacterSelectBgPath: $"{Entry.ResPath}/images/character/exusiai_select_bg.png",
            CharacterSelectIconPath: $"{Entry.ResPath}/images/character/exusiai_stand.png",
            MapMarkerPath: $"{Entry.ResPath}/images/character/exusiai_icon.png"),
        VisualCues: VisualCueSetBuilder.Create()
            .Single("idle", $"{Entry.ResPath}/images/character/exusiai_stand.png")
            .Single("relaxed", $"{Entry.ResPath}/images/character/exusiai_stand.png")
            .Single("attack", $"{Entry.ResPath}/images/character/exusiai_stand.png", 0.2f)
            .Single("cast", $"{Entry.ResPath}/images/character/exusiai_stand.png", 0.2f)
            .Single("hit", $"{Entry.ResPath}/images/character/exusiai_stand.png", 0.2f)
            .Single("dead", $"{Entry.ResPath}/images/character/exusiai_stand.png")
            .Build());

    public override Task BeforeCombatStart()
    {
        GetAmmoData().AttackModes.Clear();
        GetAmmoData().PrepaidMultipliers.Clear();
        return Task.CompletedTask;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Player.Character is not Exusiai || cardPlay.Card.Type != CardType.Attack)
            return;

        AmmoData data = GetAmmoData();
        if (data.PrepaidMultipliers.TryPeek(out int prepaidMultiplier))
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(
                AmmoAttackMode.Prepaid,
                prepaidMultiplier);
            return;
        }

        if (cardPlay.Card is IAmmoFreeAttack)
        {
            int multiplier = SecondaryResourceCmd.Get(cardPlay.Player, AmmoResource.Id) > 0 ? 1 : 0;
            data.AttackModes[cardPlay] = new AmmoAttackInfo(AmmoAttackMode.Free, multiplier);
            return;
        }

        int ammoToSpend = cardPlay.Card is IMultiAmmoAttack multi
            ? Math.Min(SecondaryResourceCmd.Get(cardPlay.Player, AmmoResource.Id), multi.MaxAmmoSpend)
            : 1;
        if (ammoToSpend <= 0)
            return;

        if (await SecondaryResourceCmd.Spend(
                cardPlay.Player,
                AmmoResource.Id,
                ammoToSpend,
                cardPlay.Card,
                this))
        {
            data.AttackModes[cardPlay] = new AmmoAttackInfo(AmmoAttackMode.Paid, ammoToSpend);
        }
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
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
            if (!GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo info))
                return 0m;

            int extraTriggers = dealer.Powers.OfType<TemporaryExtraAmmoTriggerPower>()
                .Sum(power => power.Amount);
            return GetAmmoDamageBonus(dealer) * (info.Multiplier + extraTriggers);
        }

        int previewMultiplier = cardSource is IMultiAmmoAttack multi
            ? Math.Min(SecondaryResourceCmd.Get(dealer.Player, AmmoResource.Id), multi.MaxAmmoSpend)
            : SecondaryResourceCmd.Get(dealer.Player, AmmoResource.Id) > 0 ? 1 : 0;
        if (previewMultiplier > 0 && cardSource is IExtraAmmoTriggerPreview preview)
            previewMultiplier += preview.ExtraAmmoTriggers;
        return GetAmmoDamageBonus(dealer) * previewMultiplier;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        GetAmmoData().AttackModes.Clear();
        GetAmmoData().PrepaidMultipliers.Clear();
        return Task.CompletedTask;
    }

    public static bool DidSpendAmmo(CardPlay cardPlay)
    {
        return cardPlay.Player.Character is Exusiai exusiai &&
               exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo info) &&
               info.Mode == AmmoAttackMode.Paid;
    }

    public static int GetAmmoBonus(CardPlay? cardPlay)
    {
        if (cardPlay?.Player.Character is not Exusiai exusiai ||
            !exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo info))
        {
            return 0;
        }

        return GetAmmoDamageBonus(cardPlay.Player.Creature) * info.Multiplier;
    }

    public static int GetAmmoMultiplier(CardPlay? cardPlay)
    {
        if (cardPlay?.Player.Character is not Exusiai exusiai ||
            !exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo info))
        {
            return 0;
        }

        return info.Multiplier;
    }

    public static bool HasAmmoBackedBonus(CardPlay? cardPlay)
    {
        return cardPlay?.Player.Character is Exusiai exusiai &&
               exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo info) &&
               info.Mode != AmmoAttackMode.Free &&
               info.Multiplier > 0;
    }

    public static IDisposable BeginPrepaidAmmo(Player player, int multiplier)
    {
        if (player.Character is not Exusiai exusiai)
            return EmptyScope.Instance;

        AmmoData data = exusiai.GetAmmoData();
        data.PrepaidMultipliers.Push(Math.Max(0, multiplier));
        return new PrepaidAmmoScope(data);
    }

    private static int GetAmmoDamageBonus(Creature dealer)
    {
        return AmmoResource.DamageBonus +
               dealer.Powers.OfType<AmmoDamagePower>().Sum(power => power.Amount) +
               dealer.Powers.OfType<TemporaryAmmoDamagePower>().Sum(power => power.Amount);
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
            if (data.PrepaidMultipliers.Count > 0)
                data.PrepaidMultipliers.Pop();
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
