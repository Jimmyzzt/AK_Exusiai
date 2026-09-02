using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

internal static class RelicLogisticsInventoryUi
{
    private const string CollapseButtonName = "AKExusiaiRelicCollapseButton";

    private static readonly ConditionalWeakTable<Player, CollapseState> CollapseStates = new();

    private static readonly AccessTools.FieldRef<NRelicInventory, Player?> PlayerField =
        AccessTools.FieldRefAccess<NRelicInventory, Player?>("_player");

    private static readonly AccessTools.FieldRef<NRelicInventory, List<NRelicInventoryHolder>> RelicNodesField =
        AccessTools.FieldRefAccess<NRelicInventory, List<NRelicInventoryHolder>>("_relicNodes");

    private static readonly System.Reflection.MethodInfo UpdateNavigationMethod =
        AccessTools.Method(typeof(NRelicInventory), "UpdateNavigation");

    private static readonly AccessTools.FieldRef<NRelicInventoryHolder, MegaLabel> AmountLabelField =
        AccessTools.FieldRefAccess<NRelicInventoryHolder, MegaLabel>("_amountLabel");

    public static void Sort(NRelicInventory inventory)
    {
        List<NRelicInventoryHolder> nodes = RelicNodesField(inventory);
        Player? player = PlayerField(inventory);
        NRelicInventoryHolder[] ordered = player == null
            ? nodes.ToArray()
            : RelicLogisticsCmd.SortForDisplay(
                    player,
                    nodes.Select(node => node.Relic.Model))
                .Select(model => nodes.First(node =>
                    ReferenceEquals(node.Relic.Model, model)))
                .ToArray();

        if (!nodes.SequenceEqual(ordered))
        {
            nodes.Clear();
            nodes.AddRange(ordered);
            for (int i = 0; i < ordered.Length; i++)
                inventory.MoveChild(ordered[i], i);
        }

        NRelicInventoryHolder[] collapsible = ordered.Where(IsCollapsible).ToArray();
        RelicInventoryCollapseButton? button = GetOrCreateCollapseButton(
            inventory,
            ordered,
            collapsible.Length);
        bool collapsed = player != null && CollapseStates.GetOrCreateValue(player).Collapsed;
        foreach (NRelicInventoryHolder node in ordered)
        {
            bool visible = !collapsed || !IsCollapsible(node);
            node.Visible = visible;
            node.FocusMode = visible
                ? Control.FocusModeEnum.All
                : Control.FocusModeEnum.None;
        }

        if (button != null)
        {
            button.SetState(collapsed, collapsible.Length);
            inventory.MoveChild(button, inventory.GetChildCount() - 1);
        }

        UpdateNavigationMethod.Invoke(inventory, null);
        UpdateVisibleNavigation(inventory, ordered, button);
    }

    private static bool IsCollapsible(NRelicInventoryHolder holder)
    {
        RelicLogisticsCapability? state =
            holder.Relic.Model.Capability<RelicLogisticsCapability>();
        return holder.Relic.Model is Circlet || state?.IsTransit == true;
    }

    private static RelicInventoryCollapseButton? GetOrCreateCollapseButton(
        NRelicInventory inventory,
        IReadOnlyList<NRelicInventoryHolder> nodes,
        int collapsibleCount)
    {
        RelicInventoryCollapseButton? button =
            inventory.GetNodeOrNull<RelicInventoryCollapseButton>(CollapseButtonName);
        if (collapsibleCount <= 0)
        {
            if (button != null)
                button.Visible = false;
            return button;
        }

        if (button != null)
        {
            button.Visible = true;
            return button;
        }

        if (nodes.Count == 0)
            return null;

        Texture2D arrowTexture = PreloadManager.Cache.GetTexture2D(
            ImageHelper.GetImagePath("packed/common_ui/settings_tiny_left_arrow.png"));

        Player? player = PlayerField(inventory);
        button = new RelicInventoryCollapseButton();
        button.Name = CollapseButtonName;
        button.Configure(
            arrowTexture,
            AmountLabelField(nodes[0]),
            () =>
            {
                if (player != null)
                {
                    CollapseState state = CollapseStates.GetOrCreateValue(player);
                    state.Collapsed = !state.Collapsed;
                }
                Sort(inventory);
            });
        inventory.AddChild(button);
        return button;
    }

    private static void UpdateVisibleNavigation(
        NRelicInventory inventory,
        IReadOnlyList<NRelicInventoryHolder> ordered,
        RelicInventoryCollapseButton? button)
    {
        Control[] visible = ordered
            .Where(node => node.Visible)
            .Cast<Control>()
            .Concat(button is { Visible: true } ? [button] : [])
            .ToArray();
        if (visible.Length == 0)
            return;

        for (int i = 0; i < visible.Length; i++)
        {
            visible[i].FocusNeighborLeft = visible[(i + visible.Length - 1) % visible.Length].GetPath();
            visible[i].FocusNeighborRight = visible[(i + 1) % visible.Length].GetPath();
        }

        if (button is not { Visible: true })
            return;

        Control reference = visible.Length > 1 ? visible[^2] : button;
        button.FocusNeighborTop = reference.FocusNeighborTop;
        button.FocusNeighborBottom = reference.FocusNeighborBottom;
    }

    private sealed class CollapseState
    {
        public bool Collapsed { get; set; }
    }
}

internal partial class RelicInventoryCollapseButton : NButton
{
    private static readonly Vector2 ButtonSize = new(68f, 68f);
    private static readonly Vector2 IconSize = new(44f, 44f);

    private static readonly Color CounterColor = new("#f6d36a");
    private static readonly Color CounterOutlineColor = new("#261b24");

    private TextureRect? _icon;
    private MegaLabel? _countLabel;
    private Action? _toggle;

    public void Configure(
        Texture2D arrowTexture,
        MegaLabel sourceAmountLabel,
        Action toggle)
    {
        _toggle = toggle;
        CustomMinimumSize = ButtonSize;
        Size = ButtonSize;
        MouseFilter = Control.MouseFilterEnum.Stop;
        FocusMode = Control.FocusModeEnum.All;

        _icon = new TextureRect
        {
            Name = "Arrow",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = arrowTexture,
            Position = (ButtonSize - IconSize) * 0.5f,
            Size = IconSize,
            CustomMinimumSize = Vector2.Zero,
            PivotOffset = IconSize * 0.5f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(_icon);

        _countLabel = (MegaLabel)sourceAmountLabel.Duplicate();
        _countLabel.Name = "HiddenCount";
        _countLabel.UniqueNameInOwner = false;
        _countLabel.AnchorLeft = 0f;
        _countLabel.AnchorTop = 0f;
        _countLabel.AnchorRight = 0f;
        _countLabel.AnchorBottom = 0f;
        _countLabel.GrowHorizontal = Control.GrowDirection.Begin;
        _countLabel.GrowVertical = Control.GrowDirection.Begin;
        _countLabel.Position = new Vector2(36f, -3f);
        _countLabel.Size = new Vector2(32f, 27f);
        _countLabel.MinFontSize = 10;
        _countLabel.MaxFontSize = 22;
        _countLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _countLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _countLabel.VerticalAlignment = VerticalAlignment.Center;
        _countLabel.AddThemeConstantOverride("outline_size", 5);
        _countLabel.AddThemeColorOverride("font_color", CounterColor);
        _countLabel.AddThemeColorOverride("font_outline_color", CounterOutlineColor);
        _countLabel.Visible = false;
        AddChild(_countLabel);
    }

    public override void _Ready()
    {
        ConnectSignals();
    }

    public void SetState(bool collapsed, int hiddenCount)
    {
        if (_icon != null)
            _icon.FlipH = collapsed;
        if (_countLabel != null)
        {
            _countLabel.Visible = collapsed;
            if (collapsed)
                _countLabel.SetTextAutoSize(hiddenCount.ToString());
        }
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        if (_icon != null)
            _icon.Scale = IsFocused ? Vector2.One * 1.1f : Vector2.One;
        _toggle?.Invoke();
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        if (_icon != null)
            _icon.Scale = Vector2.One * 1.1f;
    }

    protected override void OnUnfocus()
    {
        base.OnUnfocus();
        if (_icon != null)
            _icon.Scale = Vector2.One;
    }

    protected override void OnPress()
    {
        base.OnPress();
        if (_icon != null)
            _icon.Scale = Vector2.One * 0.9f;
    }
}
