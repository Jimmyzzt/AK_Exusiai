using AK_Exusiai.Mechanics;
using Godot;
using HarmonyLib;
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
    public static string Description =>
        "Show Delivery and Transit state on inventory and relic selection icons";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NRelicInventoryHolder>("RefreshAmount"),
        PatchTarget.Method<NRelicInventoryHolder>("RefreshStatus"),
        PatchTarget.Method<NRelicBasicHolder>("_Ready"),
    ];

    public static void Postfix(object __instance)
    {
        switch (__instance)
        {
            case NRelicInventoryHolder inventoryHolder:
                Sync(inventoryHolder.Relic, AmountLabel(inventoryHolder));
                break;
            case NRelicBasicHolder basicHolder:
                Sync(basicHolder.Relic, null);
                break;
        }
    }

    private static void Sync(NRelic relic, MegaLabel? amountLabel)
    {
        RelicLogisticsCapability? state =
            relic.Model.Capability<RelicLogisticsCapability>();
        if (state?.HasVisibleState != true)
        {
            HideExistingLabel(relic, DeliveryLabelName);
            HideExistingLabel(relic, TransitLabelName);
            return;
        }

        MegaLabel delivery = GetOrCreateLabel(
            relic,
            amountLabel,
            DeliveryLabelName,
            isRightAligned: false);
        MegaLabel transit = GetOrCreateLabel(
            relic,
            amountLabel,
            TransitLabelName,
            isRightAligned: true);

        bool dimExpiredTransit = state?.IsExpiredTransit == true &&
                                 state.DeliveryRemaining <= 0;
        SyncLabel(
            delivery,
            state?.DeliveryRemaining ?? 0,
            showZero: false,
            DeliveryTextColor,
            dimExpiredTransit);
        SyncLabel(
            transit,
            state?.TransitRemaining ?? 0,
            showZero: state?.IsTransit == true,
            TransitTextColor,
            dimExpiredTransit);
        SyncTint(relic.Icon, state);
    }

    private static void HideExistingLabel(NRelic relic, string name)
    {
        if (relic.GetNodeOrNull<MegaLabel>(name) is { } label)
            label.Visible = false;
    }

    private static MegaLabel GetOrCreateLabel(
        NRelic relic,
        MegaLabel? source,
        string name,
        bool isRightAligned)
    {
        MegaLabel? label = relic.GetNodeOrNull<MegaLabel>(name);
        if (label != null)
        {
            PositionLabel(relic, label, isRightAligned);
            return label;
        }

        label = source == null
            ? new MegaLabel()
            : (MegaLabel)source.Duplicate();
        label.Name = name;
        label.UniqueNameInOwner = false;
        label.AutoSizeEnabled = false;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.Size = new Vector2(34f, 30f);
        label.Scale = Vector2.One;
        label.ZIndex = source?.ZIndex ?? 1;
        label.ZAsRelative = source?.ZAsRelative ?? true;
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_outline_color", CounterOutlineColor);
        label.Visible = false;
        relic.AddChild(label);
        PositionLabel(relic, label, isRightAligned);
        return label;
    }

    private static void PositionLabel(
        NRelic relic,
        MegaLabel label,
        bool isRightAligned)
    {
        TextureRect icon = relic.Icon;
        float x = isRightAligned
            ? icon.Position.X + icon.Size.X - label.Size.X + 7f
            : icon.Position.X - 7f;
        label.Position = new Vector2(x, icon.Position.Y - 9f);
    }

    private static void SyncLabel(
        MegaLabel label,
        int amount,
        bool showZero,
        Color baseColor,
        bool dimmed)
    {
        label.Visible = amount > 0 || showZero;
        if (!label.Visible)
            return;

        label.AddThemeColorOverride(
            "font_color",
            dimmed ? MultiplyRgb(baseColor, 0.55f) : baseColor);
        label.AddThemeColorOverride(
            "font_outline_color",
            dimmed ? MultiplyRgb(CounterOutlineColor, 0.55f) : CounterOutlineColor);
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
        if (state?.IsExpiredTransit == true && !hasDelivery)
            tint = MultiplyRgb(tint, 0.55f);
        tint.A = icon.Modulate.A;
        icon.Modulate = tint;
    }

    private static Color MultiplyRgb(Color color, float factor) =>
        new(color.R * factor, color.G * factor, color.B * factor, color.A);
}
