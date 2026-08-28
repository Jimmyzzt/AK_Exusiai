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
        ExusiaiKeywords.Register();
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        _patcher = RitsuLibFramework.CreatePatcher(ModId, "gameplay");
        _patcher.RegisterPatch<AngelEnergyCostPatch>();
        _patcher.RegisterPatch<InterferencePatch>();
        _patcher.RegisterPatch<ExusiaiDeathAnimationPatch>();
        _patcher.RegisterPatch<ExusiaiGameOverAnimationPatch>();
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
            $"{ResPath}/images/relics/Confess47.png",
            $"{ResPath}/images/relics/Confess47Outline.png",
            $"{ResPath}/images/relics/FluorescentLight.png",
            $"{ResPath}/images/relics/FluorescentLightOutline.png",
            $"{ResPath}/images/relics/FirepowerRadio.png",
            $"{ResPath}/images/relics/FirepowerRadioOutline.png",
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
        ];

        string[] missing = requiredPaths.Where(path => !ResourceLoader.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"AK_Exusiai is missing required packaged assets: {string.Join(", ", missing)}");
        }
    }
}
