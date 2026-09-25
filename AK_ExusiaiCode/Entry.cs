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
        _patcher.RegisterPatch<BelugaRandomTargetPatch>();
        _patcher.RegisterPatch<BeaconMapVisualPatch>();
        _patcher.RegisterPatch<BeaconMapPointIconPatch>();
        _patcher.RegisterPatch<CompanyVanTravelPatch>();
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
        _patcher.RegisterPatch<ExusiaiFakeMerchantScalePatch>();
        _patcher.RegisterPatch<ExusiaiRestSiteCharacterReadyPatch>();
        _patcher.RegisterPatch<ExusiaiCharacterSelectSfxPatch>();
        _patcher.RegisterPatch<ExusiaiCustomSfxTokenPatch>();
        if (!_patcher.PatchAll())
            throw new InvalidOperationException("AK_Exusiai gameplay patches failed to apply.");

        ValidatePackagedAssets();
        ModHelper.SubscribeForCombatStateHooks(
            $"{ModId}.AmmoCombatHooks",
            GetAmmoCombatHookModels);
        AmmoResource.Register();
        Logger.Info("AK_Exusiai initialized for Exusiai.");
    }

    private static IEnumerable<AbstractModel> GetAmmoCombatHookModels(CombatState combatState)
    {
        return [ModelDb.Singleton<AmmoCombatHooks>()];
    }

    private static void ValidatePackagedAssets()
    {
        string[] requiredPaths =
        [
            $"{ResPath}/images/character/exusiai_stand.png",
            $"{ResPath}/images/character/exusiai_select_bg.png",
            $"{ResPath}/images/character/exusiai_select_icon.jpg",
            $"{ResPath}/images/character/exusiai_icon.png",
            $"{ResPath}/images/character/Exusiai_transition.png",
            $"{ResPath}/materials/transitions/exusiai_transition_mat.tres",
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
            $"{ResPath}/images/ui/exusiai_energy_text.svg",
            $"{ResPath}/images/ui/exusiai_energy_big.svg",
            $"{ResPath}/images/ui/exusiai_energy_counter_apple.svg",
            $"{ResPath}/images/ui/exusiai_energy_counter_halo_back.svg",
            $"{ResPath}/images/ui/exusiai_energy_counter_halo_front.svg",
            $"{ResPath}/audio/exusiai_select.wav",
            $"{ResPath}/scenes/character/exusiai_visuals.tscn",
            $"{ResPath}/scenes/character/exusiai_energy_counter.tscn",
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

        string[] ancientRelics =
        [
            "PhotoWithTheLord", "EntryPermit", "StudyTourCertificate", "LordDrone", "SprayCan",
            "CactusTart", "PrismaticWings", "Confess47", "BeaconOfNations", "TheLaw",
            "PenguinLogisticsId", "AFewFineVintages", "BlackCard", "MasterTape", "IllGottenGains",
            "CompanyVan", "ReturnToSender", "DjDeck", "PrizedRecord", "BossBusinessCard",
        ];
        string[] ancientPotions = ["UrsusBeluga", "GaulChardonnay", "YanFenjiu"];
        IEnumerable<string> ancientAssets = ancientRelics.SelectMany(name => new[]
            {
                $"{ResPath}/images/relics/{name}.png",
                $"{ResPath}/images/relics/{name}Outline.png",
            })
            .Concat(ancientPotions.SelectMany(name => new[]
            {
                $"{ResPath}/images/potions/{name}.png",
                $"{ResPath}/images/potions/{name}Outline.png",
            }))
            .Concat(new[]
            {
                $"{ResPath}/scenes/ancients/laterano_background.tscn",
                $"{ResPath}/scenes/ancients/emperor_background.tscn",
                $"{ResPath}/scenes/ancients/confess47_pet.tscn",
                $"{ResPath}/images/ancients/confess47/front/skeleton_data.tres",
                $"{ResPath}/images/ancients/laterano/background.png",
                $"{ResPath}/images/ancients/laterano/portrait.png",
                $"{ResPath}/images/ancients/laterano/map_icon.png",
                $"{ResPath}/images/ancients/laterano/map_iconOutline.png",
                $"{ResPath}/images/ancients/emperor/background.png",
                $"{ResPath}/images/ancients/emperor/portrait.png",
                $"{ResPath}/images/ancients/emperor/map_icon.png",
                $"{ResPath}/images/ancients/emperor/map_iconOutline.png",
                $"{ResPath}/images/map/BeaconOfNations.svg",
                $"{ResPath}/images/cards/Graffiti.png",
                $"{ResPath}/images/enchantments/Ascension.svg",
                $"{ResPath}/images/powers/StrongBeatPower_64.svg",
                $"{ResPath}/images/powers/StrongBeatPower_256.svg",
                $"{ResPath}/images/powers/WeakBeatPower_64.svg",
                $"{ResPath}/images/powers/WeakBeatPower_256.svg",
            });
        string[] missing = requiredPaths.Concat(ancientAssets)
            .Where(path => !ResourceLoader.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"AK_Exusiai is missing required packaged assets: {string.Join(", ", missing)}");
        }
    }
}
