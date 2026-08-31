using AK_Exusiai.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class RelicLogisticsUiPatch : IPatchMethod
{
    private const string DeliveryLabelName = "AKExusiaiDeliveryAmount";
    private const string TransitLabelName = "AKExusiaiTransitAmount";

    private static readonly AccessTools.FieldRef<NRelicInventoryHolder, MegaLabel> AmountLabel =
        AccessTools.FieldRefAccess<NRelicInventoryHolder, MegaLabel>("_amountLabel");

    public static string PatchId => "show-relic-logistics-counters";
    public static string Description => "Show Delivery and Transit counts on relic icons";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NRelicInventoryHolder>("RefreshAmount"),
    ];

    public static void Postfix(NRelicInventoryHolder __instance)
    {
        RelicLogisticsCapability? state =
            __instance.Relic.Model.Capability<RelicLogisticsCapability>();
        MegaLabel delivery = GetOrCreateLabel(__instance, DeliveryLabelName, new Vector2(-18f, -34f));
        MegaLabel transit = GetOrCreateLabel(__instance, TransitLabelName, new Vector2(18f, -34f));

        SyncLabel(delivery, state?.DeliveryRemaining ?? 0, "D");
        SyncLabel(transit, state?.IsTransit == true ? state.TransitRemaining : -1, "T");
    }

    private static MegaLabel GetOrCreateLabel(
        NRelicInventoryHolder holder,
        string name,
        Vector2 positionOffset)
    {
        MegaLabel source = AmountLabel(holder);
        MegaLabel? label = source.GetParent().GetNodeOrNull<MegaLabel>(name);
        if (label != null)
            return label;

        label = (MegaLabel)source.Duplicate();
        label.Name = name;
        label.Position = source.Position + positionOffset;
        label.Visible = false;
        source.GetParent().AddChild(label);
        return label;
    }

    private static void SyncLabel(MegaLabel label, int amount, string prefix)
    {
        label.Visible = amount >= 0;
        if (label.Visible)
            label.SetTextAutoSize($"{prefix}{amount}");
    }
}
