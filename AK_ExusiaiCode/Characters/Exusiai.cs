using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
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
public sealed class Exusiai : ModCharacterTemplate<ExusiaiCardPool, ExusiaiRelicPool, ExusiaiPotionPool>
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
        public Stack<int> PrepaidMultipliers { get; } = [];
    }

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
        AnimState attackBegin = new("Attack_Begin");
        AnimState attackLoop = new("Attack_Loop");
        AnimState attackEnd = new("Attack_End");
        AnimState cast = new("Skill_3_Skill");
        AnimState hit = new("Idle");
        AnimState dead = new("Die");
        AnimState relaxed = new("Idle", isLooping: true);

        start.NextState = idle;
        attackBegin.NextState = attackLoop;
        attackLoop.NextState = attackEnd;
        attackEnd.NextState = idle;
        cast.NextState = idle;
        hit.NextState = idle;
        relaxed.AddBranch("Idle", idle);

        CreatureAnimator animator = new(start, controller);
        animator.AddAnyState("Start", start);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Attack", attackBegin);
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
        return ModAnimStateMachines.StandardMerchantCue(
            merchantRoot,
            character,
            idleName: ExusiaiAppearanceManager.SelectedMerchantAnimation,
            relaxedName: ExusiaiAppearanceManager.SelectedMerchantAnimation);
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

        int baseAmmoLimit = cardPlay.Card is IMultiAmmoAttack multi
            ? multi.MaxAmmoSpend
            : 1;
        int extraAmmoLimit = cardPlay.Player.Creature.Powers
            .OfType<RockNGospelPower>()
            .Sum(power => power.Amount);
        int ammoLimit = baseAmmoLimit == int.MaxValue
            ? int.MaxValue
            : Math.Min(int.MaxValue, baseAmmoLimit + Math.Max(0, extraAmmoLimit));
        int ammoToSpend = Math.Min(
            SecondaryResourceCmd.Get(cardPlay.Player, AmmoResource.Id),
            ammoLimit);
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
            if (!GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo info))
                return 0m;

            if (info.Multiplier <= 0)
                return 0m;

            return GetAmmoDamageBonus(dealer) * info.Multiplier *
                   GetAmmoDamageMultiplier(dealer, cardSource);
        }

        int basePreviewLimit = cardSource is IMultiAmmoAttack multi
            ? multi.MaxAmmoSpend
            : 1;
        int extraPreviewLimit = dealer.Powers
            .OfType<RockNGospelPower>()
            .Sum(power => power.Amount);
        int previewLimit = basePreviewLimit == int.MaxValue
            ? int.MaxValue
            : Math.Min(int.MaxValue, basePreviewLimit + Math.Max(0, extraPreviewLimit));
        int previewMultiplier = Math.Min(
            SecondaryResourceCmd.Get(dealer.Player, AmmoResource.Id),
            previewLimit);
        return GetAmmoDamageBonus(dealer) * previewMultiplier *
               GetAmmoDamageMultiplier(dealer, cardSource);
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

        return GetAmmoDamageBonus(cardPlay.Player.Creature) * info.Multiplier *
               GetAmmoDamageMultiplier(cardPlay.Player.Creature, cardPlay.Card);
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

    public static int GetAmmoSpent(CardPlay? cardPlay)
    {
        if (cardPlay?.Player.Character is not Exusiai exusiai ||
            !exusiai.GetAmmoData().AttackModes.TryGetValue(cardPlay, out AmmoAttackInfo info) ||
            info.Mode != AmmoAttackMode.Paid)
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
        int bonus = AmmoResource.DamageBonus +
               dealer.Powers.OfType<FirepowerPower>().Sum(power => power.Amount);
        FirepowerRadio? radio = dealer.Player?.Relics.OfType<FirepowerRadio>().FirstOrDefault();
        return radio == null ? bonus : bonus * radio.DamageMultiplier;
    }

    private static int GetAmmoDamageMultiplier(Creature dealer, CardModel? cardSource)
    {
        int cardMultiplier = cardSource is IAmmoDamageMultiplier cardBonus
            ? cardBonus.AmmoDamageMultiplier
            : 1;
        int temporaryMultiplier = 1 + dealer.Powers
            .OfType<TemporaryAmmoDamageMultiplierPower>()
            .Sum(power => power.Amount);
        return cardMultiplier * temporaryMultiplier;
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
