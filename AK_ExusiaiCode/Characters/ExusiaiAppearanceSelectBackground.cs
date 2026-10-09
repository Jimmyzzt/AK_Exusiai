using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace AK_Exusiai.Characters;

public sealed partial class ExusiaiAppearanceSelectBackground : Control
{
    private static readonly string InspectRelicScenePath =
        SceneHelper.GetScenePath("screens/inspect_relic_screen/inspect_relic_screen");

    [Export(PropertyHint.Range, "0.1,1.0,0.05")]
    public float ArrowScale { get; set; } = 0.2f;

    [Export(PropertyHint.Range, "0,80,1")]
    public float ArrowInset { get; set; } = 18f;

    [Export(PropertyHint.Range, "-40,40,1")]
    public float ArrowVerticalOffset { get; set; }

    private Node _preview = null!;
    private TextureRect _background = null!;
    private Control _characterSelector = null!;
    private Control _outfitSelector = null!;
    private Label _characterTitle = null!;
    private Label _characterName = null!;
    private Label _outfitTitle = null!;
    private Label _outfitName = null!;
    private NCharacterSelectScreen? _characterSelectScreen;
    private NGoldArrowButton? _previousCharacter;
    private NGoldArrowButton? _nextCharacter;
    private NGoldArrowButton? _previousOutfit;
    private NGoldArrowButton? _nextOutfit;

    public override void _Ready()
    {
        _preview = GetNode("AppearancePreview");
        _background = GetNode<TextureRect>("Background/Icon");
        _characterSelector = GetNode<Control>("CharacterSelector");
        _outfitSelector = GetNode<Control>("OutfitSelector");
        _characterTitle = GetNode<Label>("CharacterSelector/Title");
        _characterName = GetNode<Label>("CharacterSelector/Name");
        _outfitTitle = GetNode<Label>("OutfitSelector/Title");
        _outfitName = GetNode<Label>("OutfitSelector/Name");
        _characterSelectScreen = FindCharacterSelectScreen();

        CreateArrowButtons();
        ConnectCharacterSelectButtons();
        _characterName.Resized += PositionArrowButtons;
        _outfitName.Resized += PositionArrowButtons;
        CallDeferred(nameof(PositionArrowButtons));
        RefreshAppearance();
    }

    private void ConnectCharacterSelectButtons()
    {
        if (_characterSelectScreen is null)
            return;

        NButton confirmButton =
            _characterSelectScreen.GetNode<NButton>("ConfirmButton");
        confirmButton.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => SetSelectorVisible(false)));

        NButton? unreadyButton =
            _characterSelectScreen.GetNodeOrNull<NButton>("UnreadyButton");
        unreadyButton?.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NButton>(_ => SetSelectorVisible(true)));
    }

    private void CreateArrowButtons()
    {
        PackedScene? scene = ResourceLoader.Load<PackedScene>(
            InspectRelicScenePath,
            null,
            ResourceLoader.CacheMode.Reuse);
        Control? template = scene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
        if (template is null)
        {
            Entry.Logger.Warn("Unable to load Exusiai appearance arrow buttons.");
            return;
        }

        try
        {
            _previousCharacter = DuplicateArrow(
                template,
                _characterName,
                "PreviousCharacter");
            _nextCharacter = DuplicateArrow(
                template,
                _characterName,
                "NextCharacter",
                useRightArrow: true);
            _previousOutfit = DuplicateArrow(
                template,
                _outfitName,
                "PreviousOutfit");
            _nextOutfit = DuplicateArrow(
                template,
                _outfitName,
                "NextOutfit",
                useRightArrow: true);
            if (_previousCharacter is null || _nextCharacter is null
                || _previousOutfit is null || _nextOutfit is null)
            {
                Entry.Logger.Warn("Unable to duplicate Exusiai appearance arrows.");
                return;
            }

            _previousCharacter.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeCharacter(-1)));
            _nextCharacter.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeCharacter(1)));
            _previousOutfit.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeOutfit(-1)));
            _nextOutfit.Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => ChangeOutfit(1)));
            PositionArrowButtons();
        }
        finally
        {
            template.QueueFreeSafely();
        }
    }

    private NGoldArrowButton? DuplicateArrow(
        Control template,
        Control parent,
        string name,
        bool useRightArrow = false)
    {
        string path = useRightArrow ? "RightArrow" : "LeftArrow";
        NGoldArrowButton? button =
            template.GetNode<NGoldArrowButton>(path).Duplicate() as NGoldArrowButton;
        if (button is null)
            return null;

        button.Name = name;
        TextureRect icon = button.GetNode<TextureRect>("TextureRect");
        if (icon.Material is Material material)
            icon.Material = material.Duplicate(true) as Material;
        button.Scale = Vector2.One * ArrowScale;
        parent.AddChildSafely(button);
        // The game template is centered in its original full-screen parent.
        // Clear those anchors after reparenting so Position is local to the
        // selector's Name label instead of inheriting a label-size offset.
        button.AnchorLeft = 0f;
        button.AnchorTop = 0f;
        button.AnchorRight = 0f;
        button.AnchorBottom = 0f;
        return button;
    }

    private void PositionArrowButtons()
    {
        if (_previousCharacter is null || _nextCharacter is null
            || _previousOutfit is null || _nextOutfit is null)
        {
            return;
        }

        PositionArrowPair(
            _characterName,
            _previousCharacter,
            _nextCharacter);
        PositionArrowPair(
            _outfitName,
            _previousOutfit,
            _nextOutfit);

        _previousCharacter.FocusNeighborRight = _nextCharacter.GetPath();
        _nextCharacter.FocusNeighborLeft = _previousCharacter.GetPath();
        _previousCharacter.FocusNeighborBottom = _previousOutfit.GetPath();
        _nextCharacter.FocusNeighborBottom = _nextOutfit.GetPath();
        _previousOutfit.FocusNeighborTop = _previousCharacter.GetPath();
        _nextOutfit.FocusNeighborTop = _nextCharacter.GetPath();
        _previousOutfit.FocusNeighborRight = _nextOutfit.GetPath();
        _nextOutfit.FocusNeighborLeft = _previousOutfit.GetPath();
    }

    private void PositionArrowPair(
        Label label,
        NGoldArrowButton previous,
        NGoldArrowButton next)
    {
        float centerY = GetVisibleTextCenterY(label) + ArrowVerticalOffset;
        CenterArrow(previous, new Vector2(ArrowInset, centerY));
        CenterArrow(next, new Vector2(label.Size.X - ArrowInset, centerY));
    }

    private static float GetVisibleTextCenterY(Label label)
    {
        int lineCount = Math.Max(1, label.GetVisibleLineCount());
        float textHeight = 0f;
        for (int line = 0; line < lineCount; line++)
            textHeight += label.GetLineHeight(line);

        textHeight = Math.Min(textHeight, label.Size.Y);
        return label.VerticalAlignment switch
        {
            VerticalAlignment.Bottom => label.Size.Y - textHeight * 0.5f,
            VerticalAlignment.Center or VerticalAlignment.Fill => label.Size.Y * 0.5f,
            _ => textHeight * 0.5f,
        };
    }

    private static void CenterArrow(NGoldArrowButton button, Vector2 targetCenter)
    {
        TextureRect icon = button.GetNode<TextureRect>("TextureRect");
        button.PivotOffset = Vector2.Zero;
        Vector2 iconCenter = icon.Position + icon.Size * 0.5f;
        button.Position = targetCenter - iconCenter * button.Scale;
    }

    private void ChangeCharacter(int delta)
    {
        ExusiaiAppearanceManager.SelectCharacter(
            ExusiaiAppearanceManager.SelectedCharacterIndex + delta);
        RefreshAppearance();
    }

    private void ChangeOutfit(int delta)
    {
        ExusiaiAppearanceManager.SelectSkin(
            ExusiaiAppearanceManager.SelectedSkinIndex + delta);
        RefreshAppearance();
    }

    private void RefreshAppearance()
    {
        _characterTitle.Text = new LocString(
            "characters",
            "AK_EXUSIAI_APPEARANCE.characterTitle").GetFormattedText();
        _outfitTitle.Text = new LocString(
            "characters",
            "AK_EXUSIAI_APPEARANCE.outfitTitle").GetFormattedText();
        _characterName.Text = new LocString(
            "characters",
            ExusiaiAppearanceManager.SelectedCharacterNameKey).GetFormattedText();
        _outfitName.Text = new LocString(
            "characters",
            ExusiaiAppearanceManager.SelectedSkinNameKey).GetFormattedText();
        CallDeferred(nameof(PositionArrowButtons));

        Texture2D? background = ResourceLoader.Load<Texture2D>(
            ExusiaiAppearanceManager.SelectedBackgroundPath,
            null,
            ResourceLoader.CacheMode.Reuse);
        if (background is not null)
            _background.Texture = background;

        if (!ExusiaiAppearanceManager.ApplyCombatSkinToSprite(_preview))
            return;

        MegaSprite sprite = new(_preview);
        this.RunWhenSpineReady(
            sprite,
            animationState => animationState.SetAnimation("Idle", loop: true));
    }

    private void SetSelectorVisible(bool visible)
    {
        _characterSelector.Visible = visible;
        _outfitSelector.Visible = visible;
        _preview.Set("visible", visible);
        if (_previousCharacter is not null)
            _previousCharacter.Visible = visible;
        if (_nextCharacter is not null)
            _nextCharacter.Visible = visible;
        if (_previousOutfit is not null)
            _previousOutfit.Visible = visible;
        if (_nextOutfit is not null)
            _nextOutfit.Visible = visible;
    }

    private NCharacterSelectScreen? FindCharacterSelectScreen()
    {
        Node? node = GetParent();
        while (node is not null)
        {
            if (node is NCharacterSelectScreen screen)
                return screen;
            node = node.GetParent();
        }

        return null;
    }
}
