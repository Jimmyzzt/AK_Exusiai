using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

internal static class RelicLogisticsInventoryUi
{
    private static readonly AccessTools.FieldRef<NRelicInventory, Player?> PlayerField =
        AccessTools.FieldRefAccess<NRelicInventory, Player?>("_player");

    private static readonly AccessTools.FieldRef<NRelicInventory, List<NRelicInventoryHolder>> RelicNodesField =
        AccessTools.FieldRefAccess<NRelicInventory, List<NRelicInventoryHolder>>("_relicNodes");

    private static readonly System.Reflection.MethodInfo UpdateNavigationMethod =
        AccessTools.Method(typeof(NRelicInventory), "UpdateNavigation");

    public static void Sort(NRelicInventory inventory)
    {
        List<NRelicInventoryHolder> nodes = RelicNodesField(inventory);
        if (nodes.Count <= 1)
            return;

        Player? player = PlayerField(inventory);
        NRelicInventoryHolder[] ordered = nodes
            .Select((node, visualIndex) => new
            {
                Node = node,
                VisualIndex = visualIndex,
                State = node.Relic.Model.Capability<RelicLogisticsCapability>(),
            })
            .OrderBy(item => item.Node.Relic.Model is Circlet ? 1 : 0)
            .ThenBy(item => item.State?.IsTransit == true ? 1 : 0)
            .ThenBy(item => item.State?.DeliveryRemaining ?? 0)
            .ThenByDescending(item => item.State?.IsTransit == true
                ? item.State.TransitRemaining
                : 0)
            .ThenBy(item => player?.Relics.IndexOf(item.Node.Relic.Model) ?? item.VisualIndex)
            .Select(item => item.Node)
            .ToArray();

        if (nodes.SequenceEqual(ordered))
            return;

        nodes.Clear();
        nodes.AddRange(ordered);
        for (int i = 0; i < ordered.Length; i++)
            inventory.MoveChild(ordered[i], i);

        UpdateNavigationMethod.Invoke(inventory, null);
    }
}
