using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class RelicSelectionLayoutPatch : IPatchMethod
{
    private const int Columns = 8;

    private static readonly AccessTools.FieldRef<NChooseARelicSelection, Control> RelicRow =
        AccessTools.FieldRefAccess<NChooseARelicSelection, Control>("_relicRow");

    private static readonly AccessTools.FieldRef<NChooseARelicSelection, NChoiceSelectionSkipButton> SkipButton =
        AccessTools.FieldRefAccess<NChooseARelicSelection, NChoiceSelectionSkipButton>("_skipButton");

    public static string PatchId => "wrap-relic-selection-layout";
    public static string Description => "Wrap large relic selections into three scrollable rows";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NChooseARelicSelection>("_Ready"),
    ];

    public static void Postfix(NChooseARelicSelection __instance)
    {
        Control row = RelicRow(__instance);
        NRelicBasicHolder[] holders = row.GetChildren()
            .OfType<NRelicBasicHolder>()
            .ToArray();
        if (holders.Length <= Columns)
            return;

        RelicSelectionScrollController controller = new();
        controller.Name = "AKExusiaiRelicSelectionScrollController";
        controller.Configure(holders, SkipButton(__instance));
        __instance.AddChild(controller);
    }
}

internal sealed class RelicSelectionDefaultFocusPatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<NChooseARelicSelection, Control> RelicRow =
        AccessTools.FieldRefAccess<NChooseARelicSelection, Control>("_relicRow");

    public static string PatchId => "focus-visible-relic-selection";
    public static string Description => "Keep initial focus inside the visible relic selection rows";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Getter<NChooseARelicSelection>("DefaultFocusedControl"),
    ];

    public static void Postfix(NChooseARelicSelection __instance, ref Control __result)
    {
        NRelicBasicHolder[] visible = RelicRow(__instance).GetChildren()
            .OfType<NRelicBasicHolder>()
            .Where(holder => holder.Visible)
            .ToArray();
        if (visible.Length > 0 && !__result.Visible)
            __result = visible[visible.Length / 2];
    }
}

internal partial class RelicSelectionScrollController : Node
{
    private const int Columns = 8;
    private const int VisibleRows = 3;
    private const float HorizontalSpacing = 200f;
    private const float VerticalSpacing = 155f;
    private const double OriginalTweenDuration = 1.05;

    private NRelicBasicHolder[] _holders = [];
    private NChoiceSelectionSkipButton? _skipButton;
    private Vector2 _origin;
    private int _scrollRow;
    private double _settleTimeRemaining = OriginalTweenDuration;

    public void Configure(
        NRelicBasicHolder[] holders,
        NChoiceSelectionSkipButton skipButton)
    {
        _holders = holders;
        _skipButton = skipButton;
        _origin = holders[0].Position;
    }

    public override void _Ready()
    {
        ProcessPriority = 1000;
        SetProcess(true);
        SetProcessUnhandledInput(true);
        ApplyLayout(keepFocusVisible: false);
    }

    public override void _Process(double delta)
    {
        _settleTimeRemaining -= delta;
        ApplyLayout(keepFocusVisible: false);
        if (_settleTimeRemaining <= 0)
            SetProcess(false);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (TotalRows <= VisibleRows || inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;

        int delta = mouseButton.ButtonIndex switch
        {
            MouseButton.WheelUp => -1,
            MouseButton.WheelDown => 1,
            _ => 0,
        };
        if (delta == 0)
            return;

        int nextRow = Math.Clamp(_scrollRow + delta, 0, TotalRows - VisibleRows);
        if (nextRow == _scrollRow)
            return;

        _scrollRow = nextRow;
        ApplyLayout(keepFocusVisible: true);
        GetViewport().SetInputAsHandled();
    }

    private int TotalRows => (_holders.Length + Columns - 1) / Columns;

    private void ApplyLayout(bool keepFocusVisible)
    {
        if (_skipButton == null)
            return;

        int visibleRowCount = Math.Min(VisibleRows, TotalRows);
        for (int index = 0; index < _holders.Length; index++)
        {
            NRelicBasicHolder holder = _holders[index];
            int row = index / Columns;
            int column = index % Columns;
            bool visible = row >= _scrollRow && row < _scrollRow + VisibleRows;
            holder.Visible = visible;
            holder.FocusMode = visible
                ? Control.FocusModeEnum.All
                : Control.FocusModeEnum.None;
            if (!visible)
                continue;

            int rowStart = row * Columns;
            int itemsInRow = Math.Min(Columns, _holders.Length - rowStart);
            float x = (column - (itemsInRow - 1) * 0.5f) * HorizontalSpacing;
            float y = (row - _scrollRow - (visibleRowCount - 1) * 0.5f) * VerticalSpacing;
            holder.Position = _origin + new Vector2(x, y);
            holder.Modulate = Colors.White;
        }

        UpdateNavigation();
        if (keepFocusVisible)
            MoveFocusIntoView();
    }

    private void UpdateNavigation()
    {
        if (_skipButton == null)
            return;

        int firstVisibleRow = _scrollRow;
        int lastVisibleRow = Math.Min(TotalRows - 1, _scrollRow + VisibleRows - 1);
        for (int row = firstVisibleRow; row <= lastVisibleRow; row++)
        {
            int rowStart = row * Columns;
            int itemsInRow = Math.Min(Columns, _holders.Length - rowStart);
            for (int column = 0; column < itemsInRow; column++)
            {
                NRelicBasicHolder holder = _holders[rowStart + column];
                holder.FocusNeighborLeft = _holders[rowStart + (column + itemsInRow - 1) % itemsInRow].GetPath();
                holder.FocusNeighborRight = _holders[rowStart + (column + 1) % itemsInRow].GetPath();
                holder.FocusNeighborTop = row == firstVisibleRow
                    ? holder.GetPath()
                    : GetHolderInRow(row - 1, column).GetPath();
                holder.FocusNeighborBottom = row == lastVisibleRow
                    ? _skipButton.GetPath()
                    : GetHolderInRow(row + 1, column).GetPath();
            }
        }

        int lastRowStart = lastVisibleRow * Columns;
        int lastRowCount = Math.Min(Columns, _holders.Length - lastRowStart);
        _skipButton.FocusNeighborTop = _holders[lastRowStart + lastRowCount / 2].GetPath();
    }

    private NRelicBasicHolder GetHolderInRow(int row, int preferredColumn)
    {
        int rowStart = row * Columns;
        int itemsInRow = Math.Min(Columns, _holders.Length - rowStart);
        return _holders[rowStart + Math.Min(preferredColumn, itemsInRow - 1)];
    }

    private void MoveFocusIntoView()
    {
        Control? focused = GetViewport().GuiGetFocusOwner();
        if (focused is not NRelicBasicHolder relicHolder || relicHolder.Visible)
            return;

        NRelicBasicHolder[] visible = _holders.Where(holder => holder.Visible).ToArray();
        if (visible.Length > 0)
            visible[visible.Length / 2].GrabFocus();
    }
}
