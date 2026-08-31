using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class RelicLogisticsInventorySortPatch : IPatchMethod
{
    public static string PatchId => "sort-relic-logistics-inventory";
    public static string Description => "Keep permanent relics ahead of Transit relics and Circlets";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NRelicInventory>("Initialize"),
        PatchTarget.Method<NRelicInventory>("OnRelicObtained"),
    ];

    public static void Postfix(NRelicInventory __instance)
    {
        RelicLogisticsInventoryUi.Sort(__instance);
    }
}
