using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class RelicLogisticsLifecyclePatch : IPatchMethod
{
    public static string PatchId => "advance-relic-logistics-after-combat";
    public static string Description =>
        "Advance Delivery and Transit counters once after each participating combat";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<Player>(nameof(Player.AfterCombatEnd)),
    ];

    public static void Postfix(Player __instance)
    {
        RelicLogisticsCmd.EndCombat(__instance);
    }
}
