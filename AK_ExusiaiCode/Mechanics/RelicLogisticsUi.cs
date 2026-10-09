using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace AK_Exusiai.Mechanics;

internal static class RelicLogisticsUi
{
    private static readonly MethodInfo InvokeDisplayAmountChangedMethod =
        AccessTools.Method(typeof(RelicModel), "InvokeDisplayAmountChanged");

    private static readonly MethodInfo InventoryAddMethod =
        AccessTools.Method(
            typeof(NRelicInventory),
            "Add",
            [typeof(RelicModel), typeof(bool), typeof(int)]);

    private static readonly MethodInfo InventoryRemoveMethod =
        AccessTools.Method(
            typeof(NRelicInventory),
            "Remove",
            [typeof(RelicModel)]);

    public static void NotifyChanged(RelicModel relic)
    {
        InvokeDisplayAmountChangedMethod.Invoke(relic, null);

        NRelicInventory? inventory = NRun.Instance?.GlobalUi.RelicInventory;
        if (inventory != null)
            RelicLogisticsInventoryUi.Sort(inventory);
    }

    public static void ReplaceRelicModel(
        RelicModel expired,
        RelicModel refreshed,
        int index)
    {
        NRelicInventory? inventory = NRun.Instance?.GlobalUi.RelicInventory;
        if (inventory == null)
            return;

        // The underlying player inventory replacement is silent so no obtain
        // or removal observer can mistake this state refresh for reacquisition.
        // Update only the local visual inventory and suppress the newly-acquired
        // animation for the replacement model.
        InventoryRemoveMethod.Invoke(inventory, [expired]);
        InventoryAddMethod.Invoke(inventory, [refreshed, true, index]);
        RelicLogisticsInventoryUi.Sort(inventory);
    }
}
