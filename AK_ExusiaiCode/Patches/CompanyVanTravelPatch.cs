using AK_Exusiai.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class CompanyVanTravelPatch : IPatchMethod
{
    public static string PatchId => "company-van-same-row-travel";
    public static string Description => "Offer unused map rooms in the current row to the van owner";
    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(MapTravel), nameof(MapTravel.GetTravelablePointsFrom)),
    ];

    public static void Postfix(IRunState runState, MapPoint currentPoint, ref IEnumerable<MapPoint> __result)
    {
        if (runState is not RunState run || run.Players.Count != 1 || run.CurrentMapPoint != currentPoint ||
            !run.Players[0].Relics.OfType<CompanyVan>().Any(v => !v.IsUsedUp))
            return;
        HashSet<MapCoord> visited = run.VisitedMapCoords.ToHashSet();
        __result = __result.Concat(run.Map.GetPointsInRow(currentPoint.coord.row)
            .Where(point => !visited.Contains(point.coord)))
            .Distinct();
    }
}
