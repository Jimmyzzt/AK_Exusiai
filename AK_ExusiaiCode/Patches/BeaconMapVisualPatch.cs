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
    private static readonly AccessTools.FieldRef<NMapPoint, IRunState> PointRunRef =
        AccessTools.FieldRefAccess<NMapPoint, IRunState>("_runState");
    public static string PatchId => "show-beacon-combat-icon";
    public static string Description => "Use the Beacon icon in marked combat map nodes";
    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NNormalMapPoint>("_Ready"),
    ];

    public static void Postfix(NNormalMapPoint __instance)
    {
        // Old saves may still contain Beacon as a vanilla quest marker. Remove only
        // our marker so Fur Coat and other mods retain their own quest overlays.
        foreach (BeaconOfNations oldMarker in __instance.Point.Quests.OfType<BeaconOfNations>().ToList())
            __instance.Point.RemoveQuest(oldMarker);
        if (__instance.Point.PointType is not (MapPointType.Monster or MapPointType.Elite))
            return;
        IRunState? run = PointRunRef(__instance);
        if (run is null || !run.Players.SelectMany(p => p.Relics).OfType<BeaconOfNations>()
                .Any(b => b.MarkedActIndex == run.CurrentActIndex && b.GetPath().Contains(__instance.Point.coord)))
            return;
        Control? container = __instance.GetNodeOrNull<Control>("%IconContainer");
        TextureRect? roomIcon = __instance.GetNodeOrNull<TextureRect>("%Icon");
        if (container is null || roomIcon is null || container.HasNode("BeaconIcon"))
            return;
        Texture2D? texture = ResourceLoader.Load<Texture2D>($"{Entry.ResPath}/images/map/BeaconOfNations.svg");
        if (texture is null)
            return;
        TextureRect marker = new()
        {
            Name = "BeaconIcon",
            Texture = texture,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Position = roomIcon.Position + new Vector2(-10, -10),
            Size = new Vector2(24, 24),
        };
        container.AddChild(marker);
    }
}
