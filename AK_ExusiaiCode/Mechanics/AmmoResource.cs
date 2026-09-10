using AK_Exusiai.Characters;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace AK_Exusiai.Mechanics;

public static partial class AmmoResource
{
    public const string LocalId = "ammo";
    public const int MaxAmount = 30;
    public const int MinimumDamageBonus = 2;

    public static SecondaryResourceDefinition Definition { get; private set; } = null!;
    public static string Id => Definition.Id;

    public static void Register()
    {
        var resources = RitsuLibFramework.GetSecondaryResourceRegistry(Entry.ModId);
        Definition = resources.Register(LocalId, new SecondaryResourceDefinition(
            defaultAmount: 0,
            baseMaxAmount: MaxAmount,
            turnStartPolicy: SecondaryResourceTurnStartPolicy.None,
            persistencePolicy: SecondaryResourcePersistencePolicy.None,
            smallIconPath: $"{Entry.ResPath}/images/ui/ammo.svg",
            largeIconPath: $"{Entry.ResPath}/images/ui/ammo.svg")
        {
            ClampToMaxAmount = true,
        });

        resources.AlwaysShowInCombatUiForCharacter<Exusiai>(LocalId);
        resources.RegisterCombatUi(
            "ammo_counter",
            _ => CreateCounter(),
            ctx => BindCounter(ctx.Node, ctx.Player!),
            new NodeAttachmentOptions
            {
                Name = "AKExusiaiAmmoCounter",
                DuplicatePolicy = NodeAttachmentDuplicatePolicy.SkipIfExistingByName,
            });
    }

    public static int GetBaseDamageBonus(int ammoBeforeSpend)
    {
        return Math.Max(MinimumDamageBonus, ammoBeforeSpend / 5);
    }

    public static AmmoDamageBreakdown GetCurrentDamageBreakdown(Player player, decimal cardMultiplier = 1m)
    {
        int ammo = SecondaryResourceCmd.Get(player, Id);
        int firepower = player.Creature.Powers.OfType<AK_Exusiai.Powers.FirepowerPower>().Sum(power => power.Amount);
        decimal baseDamage = GetBaseDamageBonus(ammo) + firepower;
        decimal commonMultiplier = 1m + player.Creature.Powers
            .OfType<AK_Exusiai.Powers.AmmoDamageMultiplierPower>()
            .Sum(power => power.Amount / 100m);
        return new AmmoDamageBreakdown(
            ammo,
            firepower,
            baseDamage,
            commonMultiplier,
            cardMultiplier,
            baseDamage * commonMultiplier * cardMultiplier);
    }

    private static NSecondaryResourceCounter CreateCounter()
    {
        var counter = NSecondaryResourceCounter.Create(Definition, new SecondaryResourceCounterStyle
        {
            CounterSize = new Vector2(56f, 56f),
            IconSize = new Vector2(52f, 52f),
            FontSize = 30,
            OutlineSize = 8,
            AmountLabelOffset = new Vector2(34f, 18f),
        });

        counter.AnchorLeft = 0f;
        counter.AnchorTop = 1f;
        counter.AnchorRight = 0f;
        counter.AnchorBottom = 1f;
        counter.OffsetLeft = 170f;
        counter.OffsetTop = -150f;
        counter.OffsetRight = 226f;
        counter.OffsetBottom = -94f;
        counter.MouseFilter = Control.MouseFilterEnum.Pass;

        counter.AddChild(new AmmoDamageLabel
        {
            Name = AmmoDamageLabel.NodeName,
            Position = new Vector2(50f, 2f),
            Size = new Vector2(38f, 24f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Pass,
        });
        return counter;
    }

    private static void BindCounter(NSecondaryResourceCounter counter, Player player)
    {
        counter.Bind(player);
        if (counter.GetNodeOrNull<AmmoDamageLabel>(AmmoDamageLabel.NodeName) is { } damageLabel)
            damageLabel.Bind(player);
    }

    public readonly record struct AmmoDamageBreakdown(
        int Ammo,
        int Firepower,
        decimal BaseDamage,
        decimal CommonMultiplier,
        decimal CardMultiplier,
        decimal DamagePerAmmo);

    private sealed partial class AmmoDamageLabel : Label
    {
        public const string NodeName = "AKExusiaiAmmoDamageLabel";

        private Player? _player;
        private string? _lastText;

        public AmmoDamageLabel()
        {
            AddThemeFontSizeOverride("font_size", 18);
            AddThemeColorOverride("font_color", new Color("FFE37A"));
            AddThemeColorOverride("font_outline_color", new Color("4F1414"));
            AddThemeConstantOverride("outline_size", 5);
        }

        public void Bind(Player player)
        {
            _player = player;
            RefreshDamageLabel();
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            RefreshDamageLabel();
        }

        private void RefreshDamageLabel()
        {
            if (_player == null)
                return;

            decimal damage = AmmoResource.GetCurrentDamageBreakdown(_player).DamagePerAmmo;
            string text = $"+{FormatDamage(damage)}";
            if (text == _lastText)
                return;

            _lastText = text;
            Text = text;
        }

        private static string FormatDamage(decimal value)
        {
            return decimal.Truncate(value) == value
                ? ((int)value).ToString()
                : value.ToString("0.#");
        }
    }
}
