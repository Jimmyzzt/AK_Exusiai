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
    private static readonly AccessTools.FieldRef<NMapScreen, Dictionary<MapCoord, NMapPoint>> PointsRef =
        AccessTools.FieldRefAccess<NMapScreen, Dictionary<MapCoord, NMapPoint>>("_mapPointDictionary");

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
        var points = PointsRef(__instance);
        HashSet<MapCoord> markedCombats = [];
        foreach (BeaconOfNations beacon in run.Players.SelectMany(p => p.Relics).OfType<BeaconOfNations>())
        {
            if (beacon.MarkedActIndex != run.CurrentActIndex)
                continue;
            beacon.EnsureRoute();
            IReadOnlyList<MapCoord> route = beacon.GetPath();
            foreach (MapCoord coord in route)
                if (run.Map.GetPoint(coord) is { PointType: MapPointType.Monster or MapPointType.Elite })
                    markedCombats.Add(coord);
            for (int i = 0; i + 1 < route.Count; i++)
            {
                if (paths.TryGetValue((route[i], route[i + 1]), out var dots))
                {
                    foreach (TextureRect dot in dots)
                        dot.Modulate = new Color("CDD1D6");
                }
            }
        }
        foreach ((MapCoord coord, NMapPoint point) in points)
            if (point is NNormalMapPoint normal)
                BeaconMapPointIconPatch.SyncIcon(normal, markedCombats.Contains(coord));
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
        IRunState? run = PointRunRef(__instance);
        bool marked = run is not null && __instance.Point.PointType is MapPointType.Monster or MapPointType.Elite &&
            run.Players.SelectMany(p => p.Relics).OfType<BeaconOfNations>()
                .Any(b => b.MarkedActIndex == run.CurrentActIndex && b.GetPath().Contains(__instance.Point.coord));
        SyncIcon(__instance, marked);
    }

    public static void SyncIcon(NNormalMapPoint point, bool marked)
    {
        Control? container = point.GetNodeOrNull<Control>("%IconContainer");
        if (container is null)
            return;
        TextureRect? existing = container.GetNodeOrNull<TextureRect>("BeaconIcon");
        if (!marked)
        {
            existing?.QueueFree();
            return;
        }
        if (existing is not null)
            return;
        TextureRect? roomIcon = point.GetNodeOrNull<TextureRect>("%Icon");
        if (roomIcon is null)
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
