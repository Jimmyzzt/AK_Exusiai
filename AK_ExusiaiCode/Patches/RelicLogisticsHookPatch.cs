using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class RelicLogisticsHookPatch : IPatchMethod
{
    public static string PatchId => "filter-inactive-logistics-relic-hooks";
    public static string Description =>
        "Omit Delivered and expired Transit relics from run and combat hook dispatch";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<RunState>(nameof(RunState.IterateHookListeners), typeof(ICombatState)),
        PatchTarget.Method<CombatState>(nameof(CombatState.IterateHookListeners)),
    ];

    public static void Postfix(ref IEnumerable<AbstractModel> __result)
    {
        __result = Filter(__result);
    }

    private static IEnumerable<AbstractModel> Filter(IEnumerable<AbstractModel> models)
    {
        foreach (AbstractModel model in models)
        {
            if (model is RelicModel relic && !RelicLogisticsCmd.IsOperational(relic))
                continue;

            yield return model;
        }
    }
}
