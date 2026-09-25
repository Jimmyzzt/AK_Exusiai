using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class BelugaRandomTargetPatch : IPatchMethod
{
    public static string PatchId => "beluga-randomize-single-target-attacks";
    public static string Description => "Treat single-enemy attacks as random-enemy attacks while Beluga is active";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<CardModel>("get_TargetType"),
    ];

    public static void Postfix(CardModel __instance, ref TargetType __result)
    {
        if (__result == TargetType.AnyEnemy &&
            __instance.Type == CardType.Attack &&
            __instance.IsMutable &&
            __instance.Owner?.Creature.HasPower<BelugaRandomTargetPower>() == true)
        {
            __result = TargetType.RandomEnemy;
        }
    }
}
