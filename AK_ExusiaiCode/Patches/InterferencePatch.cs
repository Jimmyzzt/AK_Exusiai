using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class InterferencePatch : IPatchMethod
{
    public static string PatchId => "suppress-enemy-passive-powers";
    public static string Description => "Temporarily omit positive enemy powers from combat hook dispatch";

    public static ModPatchTarget[] GetTargets() =>
    [
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
            if (model is PowerModel power &&
                power is not InterferencePower &&
                power.Owner.HasPower<InterferencePower>() &&
                power.TypeForCurrentAmount == PowerType.Buff)
            {
                continue;
            }

            yield return model;
        }
    }
}
