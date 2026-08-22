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
        _patcher.RegisterPatch<InterferencePatch>();
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
            $"{ResPath}/images/character/exusiai_icon.png",
            $"{ResPath}/images/relics/ExusiaiBadge.png",
            $"{ResPath}/images/ui/ammo.svg",
            $"{ResPath}/scenes/character/exusiai_visuals.tscn",
        ];

        string[] missing = requiredPaths.Where(path => !ResourceLoader.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"AK_Exusiai is missing required packaged assets: {string.Join(", ", missing)}");
        }
    }
}
