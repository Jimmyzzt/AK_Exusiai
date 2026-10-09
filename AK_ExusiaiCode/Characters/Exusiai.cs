using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace AK_Exusiai.Characters;

[RegisterCharacter]
public sealed class Exusiai :
    ModCharacterTemplate<ExusiaiCardPool, ExusiaiRelicPool, ExusiaiPotionPool>
{
    internal const string CustomCharacterSelectSfxToken =
        "ak_exusiai:/sfx/character_select";

    private const string CharacterScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_visuals.tscn";
    private const string EnergyCounterScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_energy_counter.tscn";
    private const string MerchantScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_merchant.tscn";
    private const string RestSiteScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_rest_site.tscn";
    private const string CharacterSelectBgScenePath =
        $"{Entry.ResPath}/scenes/character/exusiai_character_select_bg.tscn";
    private const string CharacterSelectTransitionMaterialPath =
        $"{Entry.ResPath}/materials/transitions/exusiai_transition_mat.tres";

    public override CharacterGender Gender => CharacterGender.Feminine;
    public override Color NameColor => new("F04B61");
    public override int StartingHp => 77;
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.35f;
    public override float CastAnimDelay => 0.35f;
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
            EnergyCounterPath: EnergyCounterScenePath,
            MerchantAnimPath: MerchantScenePath,
            RestSiteAnimPath: RestSiteScenePath),
        Ui: new CharacterUiAssetSet(
            IconTexturePath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            IconOutlineTexturePath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            IconPath: $"{Entry.ResPath}/images/character/exusiai_icon.png",
            CharacterSelectBgPath: CharacterSelectBgScenePath,
            CharacterSelectIconPath: $"{Entry.ResPath}/images/character/exusiai_select_icon.jpg",
            CharacterSelectTransitionPath: CharacterSelectTransitionMaterialPath,
            MapMarkerPath: $"{Entry.ResPath}/images/character/exusiai_icon.png"),
        Audio: new CharacterAudioAssetSet(
            CharacterSelectSfx: CustomCharacterSelectSfxToken),
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

    public static bool DidSpendAmmo(CardPlay cardPlay) =>
        AmmoCombatHooks.DidSpendAmmo(cardPlay);

    public static int GetEffectiveAmmoSpent(CardPlay? cardPlay) =>
        AmmoCombatHooks.GetEffectiveAmmoSpent(cardPlay);

    public static decimal GetAmmoBonus(CardPlay? cardPlay) =>
        AmmoCombatHooks.GetAmmoBonus(cardPlay);

    public static IReadOnlyList<decimal> GetAmmoHitBonuses(CardPlay? cardPlay) =>
        AmmoCombatHooks.GetAmmoHitBonuses(cardPlay);

    public static int GetAmmoMultiplier(CardPlay? cardPlay) =>
        AmmoCombatHooks.GetAmmoMultiplier(cardPlay);

    public static int GetAmmoSpent(CardPlay? cardPlay) =>
        AmmoCombatHooks.GetAmmoSpent(cardPlay);

    public static bool HasAmmoBackedBonus(CardPlay? cardPlay) =>
        AmmoCombatHooks.HasAmmoBackedBonus(cardPlay);

    public static bool UsedOverloadAmmoBonus(CardPlay? cardPlay) =>
        AmmoCombatHooks.UsedOverloadAmmoBonus(cardPlay);

    public static decimal GetAmmoDamagePerAmmo(Player player, CardModel? cardSource = null) =>
        AmmoCombatHooks.GetAmmoDamagePerAmmo(player, cardSource);

    public static IDisposable BeginPrepaidAmmo(
        Player player,
        int ammoSpent,
        decimal damagePerAmmo) =>
        AmmoCombatHooks.BeginPrepaidAmmo(player, ammoSpent, damagePerAmmo);

    public static Task EnterOverload(
        PlayerChoiceContext choiceContext,
        Player player,
        AbstractModel source,
        bool retainAmmoAtTurnEnd = false) =>
        AmmoCombatHooks.EnterOverload(choiceContext, player, source, retainAmmoAtTurnEnd);

    internal static void ClearAmmoTransientState(Player player) =>
        AmmoCombatHooks.ClearAmmoTransientState(player);

    internal static Task NotifyOverloadAmmoSpent(Player player, int amount) =>
        AmmoCombatHooks.NotifyOverloadAmmoSpent(player, amount);

    public override List<string> GetArchitectAttackVfx()
    {
        return ["vfx/vfx_attack_slash"];
    }
}
