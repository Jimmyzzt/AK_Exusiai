using System.Reflection;
using AK_Exusiai.Characters;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Patches;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace AK_Exusiai;

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    public const string ModId = "AK_Exusiai";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);
    private static ModPatcher? _patcher;

    public static void Initialize()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ExusiaiAppearanceManager.Initialize();
        Effects.CardEffectStore.Initialize();
        ExusiaiKeywords.Register();
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        _patcher = RitsuLibFramework.CreatePatcher(ModId, "gameplay");
        _patcher.RegisterPatch<AngelFreeCostPatch>();
        _patcher.RegisterPatch<CompassionTransferPatch>();
        _patcher.RegisterPatch<InterferencePatch>();
        _patcher.RegisterPatch<RelicLogisticsHookPatch>();
        _patcher.RegisterPatch<RelicLogisticsLifecyclePatch>();
        _patcher.RegisterPatch<RelicLogisticsUiPatch>();
        _patcher.RegisterPatch<RelicLogisticsInventorySortPatch>();
        _patcher.RegisterPatch<RelicSelectionLayoutPatch>();
        _patcher.RegisterPatch<RelicSelectionDefaultFocusPatch>();
        _patcher.RegisterPatch<ExusiaiDeathAnimationPatch>();
        _patcher.RegisterPatch<ExusiaiGameOverAnimationPatch>();
        _patcher.RegisterPatch<ExusiaiNewCovenantCombatAnimationPatch>();
        _patcher.RegisterPatch<ExusiaiMerchantCharacterReadyPatch>();
        _patcher.RegisterPatch<ExusiaiMerchantCharacterPlayAnimationPatch>();
        _patcher.RegisterPatch<ExusiaiRestSiteCharacterReadyPatch>();
        if (!_patcher.PatchAll())
            throw new InvalidOperationException("AK_Exusiai gameplay patches failed to apply.");

        ValidatePackagedAssets();
        ModHelper.SubscribeForCombatStateHooks(
            $"{ModId}.CharacterCombatHooks",
            GetCharacterCombatHookModels);
        AmmoResource.Register();
        Logger.Info("AK_Exusiai initialized for Exusiai.");
    }

    private static IEnumerable<AbstractModel> GetCharacterCombatHookModels(CombatState combatState)
    {
        return combatState.Players
            .Select(player => player.Character)
            .OfType<Exusiai>()
            .Distinct();
    }

    private static void ValidatePackagedAssets()
    {
        string[] requiredPaths =
        [
            $"{ResPath}/images/character/exusiai_stand.png",
            $"{ResPath}/images/character/exusiai_select_bg.png",
            $"{ResPath}/images/character/exusiai_select_icon.jpg",
            $"{ResPath}/images/character/exusiai_icon.png",
            $"{ResPath}/images/relics/ExusiaiBadge.png",
            $"{ResPath}/images/relics/ExusiaiBadgeOutline.png",
            $"{ResPath}/images/relics/ExusiaiSurprise.png",
            $"{ResPath}/images/relics/ExusiaiSurpriseOutline.png",
            $"{ResPath}/images/relics/EtchedRound.png",
            $"{ResPath}/images/relics/EtchedRoundOutline.png",
            $"{ResPath}/images/relics/ApplePieLogistics.png",
            $"{ResPath}/images/relics/ApplePieLogisticsOutline.png",
            $"{ResPath}/images/relics/BossMedal.png",
            $"{ResPath}/images/relics/BossMedalOutline.png",
            $"{ResPath}/images/relics/LordServer.png",
            $"{ResPath}/images/relics/LordServerOutline.png",
            $"{ResPath}/images/relics/PocketSlotMachine.png",
            $"{ResPath}/images/relics/PocketSlotMachineOutline.png",
            $"{ResPath}/images/relics/FluorescentLight.png",
            $"{ResPath}/images/relics/FluorescentLightOutline.png",
            $"{ResPath}/images/relics/FirepowerFm.png",
            $"{ResPath}/images/relics/FirepowerFmOutline.png",
            $"{ResPath}/images/potions/PotionShapedMag.png",
            $"{ResPath}/images/potions/PotionShapedMagOutline.png",
            $"{ResPath}/images/potions/EmperorsStash.png",
            $"{ResPath}/images/potions/EmperorsStashOutline.png",
            $"{ResPath}/images/potions/BottledHalo.png",
            $"{ResPath}/images/potions/BottledHaloOutline.png",
            $"{ResPath}/images/ui/ammo.svg",
            $"{ResPath}/scenes/character/exusiai_visuals.tscn",
            $"{ResPath}/scenes/character/exusiai_merchant.tscn",
            $"{ResPath}/scenes/character/exusiai_rest_site.tscn",
            $"{ResPath}/scenes/character/exusiai_character_select_bg.tscn",
            $"{ResPath}/images/character/appearance/Exusiai.png",
            $"{ResPath}/images/character/appearance/Exusiai_the_New_Covenant.png",
            $"{ResPath}/images/character/spine/exusiai/default/combat_skeleton_data.tres",
            $"{ResPath}/images/character/spine/exusiai/default/build_skeleton_data.tres",
            $"{ResPath}/images/character/spine/exusiai/midnight_delivery/combat_skeleton_data.tres",
            $"{ResPath}/images/character/spine/exusiai/midnight_delivery/build_skeleton_data.tres",
            $"{ResPath}/images/character/spine/exusiai/wild_operation/combat_skeleton_data.tres",
            $"{ResPath}/images/character/spine/exusiai/wild_operation/build_skeleton_data.tres",
            $"{ResPath}/images/character/spine/exusiai/city_rider/combat_skeleton_data.tres",
            $"{ResPath}/images/character/spine/exusiai/city_rider/build_skeleton_data.tres",
            $"{ResPath}/images/character/spine/new_covenant/default/combat_skeleton_data.tres",
            $"{ResPath}/images/character/spine/new_covenant/default/build_skeleton_data.tres",
            $"{ResPath}/images/character/spine/new_covenant/wingseekers_song/combat_skeleton_data.tres",
            $"{ResPath}/images/character/spine/new_covenant/wingseekers_song/build_skeleton_data.tres",
        ];

        string[] missing = requiredPaths.Where(path => !ResourceLoader.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"AK_Exusiai is missing required packaged assets: {string.Join(", ", missing)}");
        }
    }
}
