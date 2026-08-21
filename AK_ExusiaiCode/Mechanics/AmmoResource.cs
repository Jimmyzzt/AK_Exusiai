using AK_Exusiai.Characters;
using Godot;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace AK_Exusiai.Mechanics;

public static class AmmoResource
{
    public const string LocalId = "ammo";
    public const int DamageBonus = 2;

    public static SecondaryResourceDefinition Definition { get; private set; } = null!;
    public static string Id => Definition.Id;

    public static void Register()
    {
        var resources = RitsuLibFramework.GetSecondaryResourceRegistry(Entry.ModId);
        Definition = resources.Register(LocalId, new SecondaryResourceDefinition(
            defaultAmount: 0,
            baseMaxAmount: null,
            turnStartPolicy: SecondaryResourceTurnStartPolicy.None,
            persistencePolicy: SecondaryResourcePersistencePolicy.None,
            smallIconPath: $"{Entry.ResPath}/images/ui/ammo.svg",
            largeIconPath: $"{Entry.ResPath}/images/ui/ammo.svg"));

        resources.AlwaysShowInCombatUiForCharacter<Exusiai>(LocalId);
        resources.RegisterCombatUi(
            "ammo_counter",
            _ => CreateCounter(),
            ctx => ctx.Node.Bind(ctx.Player),
            new NodeAttachmentOptions
            {
                Name = "AKExusiaiAmmoCounter",
                DuplicatePolicy = NodeAttachmentDuplicatePolicy.SkipIfExistingByName,
            });
    }

    private static NSecondaryResourceCounter CreateCounter()
    {
        var counter = NSecondaryResourceCounter.Create(Definition, new SecondaryResourceCounterStyle
        {
            CounterSize = new Vector2(56f, 56f),
            IconSize = new Vector2(52f, 52f),
            FontSize = 30,
            OutlineSize = 8,
            AmountLabelOffset = new Vector2(18f, 18f),
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
        return counter;
    }
}
