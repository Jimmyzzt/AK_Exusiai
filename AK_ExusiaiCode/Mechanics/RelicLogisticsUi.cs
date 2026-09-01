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

    public static void NotifyChanged(RelicModel relic)
    {
        InvokeDisplayAmountChangedMethod.Invoke(relic, null);

        NRelicInventory? inventory = NRun.Instance?.GlobalUi.RelicInventory;
        if (inventory != null)
            RelicLogisticsInventoryUi.Sort(inventory);
    }
}
