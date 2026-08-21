using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using StsLogger = MegaCrit.Sts2.Core.Logging.Logger;

namespace AK_Exusiai.AK_ExusiaiCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "AK_Exusiai";
    public const string ResPath = $"res://{ModId}";

    public static StsLogger Logger { get; private set; } = null!;

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        Logger = RitsuLibFramework.CreateLogger(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
    }
}
