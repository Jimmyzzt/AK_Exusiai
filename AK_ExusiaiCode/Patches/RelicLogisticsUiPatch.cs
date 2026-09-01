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

    private static readonly Color DeliveryTint = new("#ff7373");
    private static readonly Color TransitTint = new("#73a9ff");
    private static readonly Color CombinedTint = new("#c17bde");
    private static readonly Color DeliveryTextColor = new("#ff4f4f");
    private static readonly Color TransitTextColor = new("#4f9dff");
    private static readonly Color CounterOutlineColor = new("#261b24");

    private static readonly AccessTools.FieldRef<NRelicInventoryHolder, MegaLabel> AmountLabel =
        AccessTools.FieldRefAccess<NRelicInventoryHolder, MegaLabel>("_amountLabel");

    public static string PatchId => "show-relic-logistics-counters";
    public static string Description => "Show Delivery and Transit counts on relic icons";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NRelicInventoryHolder>("RefreshAmount"),
        PatchTarget.Method<NRelicInventoryHolder>("RefreshStatus"),
    ];

    public static void Postfix(NRelicInventoryHolder __instance)
    {
        RelicLogisticsCapability? state =
            __instance.Relic.Model.Capability<RelicLogisticsCapability>();
        MegaLabel delivery = GetOrCreateLabel(
            __instance,
            DeliveryLabelName,
            DeliveryTextColor,
            isRightAligned: false);
        MegaLabel transit = GetOrCreateLabel(
            __instance,
            TransitLabelName,
            TransitTextColor,
            isRightAligned: true);

        SyncLabel(delivery, state?.DeliveryRemaining ?? 0, showZero: false);
        SyncLabel(
            transit,
            state?.TransitRemaining ?? 0,
            showZero: state?.IsTransit == true);
        SyncTint(__instance.Relic.Icon, state);
    }

    private static MegaLabel GetOrCreateLabel(
        NRelicInventoryHolder holder,
        string name,
        Color textColor,
        bool isRightAligned)
    {
        MegaLabel source = AmountLabel(holder);
        MegaLabel? label = holder.Relic.GetNodeOrNull<MegaLabel>(name);
        if (label != null)
        {
            PositionLabel(holder, label, isRightAligned);
            return label;
        }

        label = (MegaLabel)source.Duplicate();
        label.Name = name;
        label.UniqueNameInOwner = false;
        label.AutoSizeEnabled = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.Size = new Vector2(34f, 30f);
        label.Scale = Vector2.One;
        label.ZIndex = source.ZIndex;
        label.ZAsRelative = source.ZAsRelative;
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_color", textColor);
        label.AddThemeColorOverride("font_outline_color", CounterOutlineColor);
        label.Visible = false;
        holder.Relic.AddChild(label);
        PositionLabel(holder, label, isRightAligned);
        return label;
    }

    private static void PositionLabel(
        NRelicInventoryHolder holder,
        MegaLabel label,
        bool isRightAligned)
    {
        TextureRect icon = holder.Relic.Icon;
        float x = isRightAligned
            ? icon.Position.X + icon.Size.X - label.Size.X + 7f
            : icon.Position.X - 7f;
        label.Position = new Vector2(x, icon.Position.Y - 9f);
    }

    private static void SyncLabel(MegaLabel label, int amount, bool showZero)
    {
        label.Visible = amount > 0 || showZero;
        if (label.Visible)
            label.SetTextAutoSize(amount.ToString());
    }

    private static void SyncTint(TextureRect icon, RelicLogisticsCapability? state)
    {
        bool hasDelivery = state?.DeliveryRemaining > 0;
        bool hasTransit = state?.IsTransit == true;
        if (!hasDelivery && !hasTransit)
            return;

        Color tint = hasDelivery && hasTransit
            ? CombinedTint
            : hasDelivery
                ? DeliveryTint
                : TransitTint;
        tint.A = icon.Modulate.A;
        icon.Modulate = tint;
    }
}
