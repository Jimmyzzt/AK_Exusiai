using System.Reflection;
using AK_Exusiai.Characters;
using AK_Exusiai.Mechanics;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace AK_Exusiai;

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    public const string ModId = "AK_Exusiai";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        ValidatePackagedAssets();
        AmmoResource.Register();
        Logger.Info("AK_Exusiai initialized for Exusiai.");
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
