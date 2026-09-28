using AK_Exusiai.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class BeaconMapVisualPatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<NMapScreen, RunState> RunStateRef =
        AccessTools.FieldRefAccess<NMapScreen, RunState>("_runState");
    private static readonly AccessTools.FieldRef<NMapScreen,
        Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>> PathsRef =
        AccessTools.FieldRefAccess<NMapScreen,
            Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>>("_paths");

    public static string PatchId => "show-beacon-route";
    public static string Description => "Tint Beacon route dots light gray when the map is drawn or opened";
    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NMapScreen>(nameof(NMapScreen.SetMap)),
        PatchTarget.Method<NMapScreen>(nameof(NMapScreen.Open)),
    ];

    public static void Postfix(NMapScreen __instance)
    {
        RunState? run = RunStateRef(__instance);
        if (run is null)
            return;
        var paths = PathsRef(__instance);
        foreach (BeaconOfNations beacon in run.Players.SelectMany(p => p.Relics).OfType<BeaconOfNations>())
        {
            if (beacon.MarkedActIndex != run.CurrentActIndex)
                continue;
            IReadOnlyList<MapCoord> route = beacon.GetPath();
            for (int i = 0; i + 1 < route.Count; i++)
            {
                if (paths.TryGetValue((route[i], route[i + 1]), out var dots))
                {
                    foreach (TextureRect dot in dots)
                        dot.Modulate = new Color("CDD1D6");
                }
            }
        }
    }
}

internal sealed class BeaconMapPointIconPatch : IPatchMethod
{
    public static string PatchId => "show-beacon-combat-icon";
    public static string Description => "Use the Beacon icon in marked combat map nodes";
    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NNormalMapPoint>("_Ready"),
    ];

    public static void Postfix(NNormalMapPoint __instance)
    {
        if (!__instance.Point.Quests.OfType<BeaconOfNations>().Any())
            return;
        TextureRect? icon = __instance.GetNodeOrNull<TextureRect>("%QuestIcon");
        if (icon is not null)
        {
            icon.Texture = ResourceLoader.Load<Texture2D>($"{Entry.ResPath}/images/map/BeaconOfNations.svg");
            icon.Visible = true;
        }
    }
}
