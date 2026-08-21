using AK_Exusiai.AK_ExusiaiCode.Cards;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.AK_ExusiaiCode.Character;

public sealed class AKExusiaiCardPool : TypeListCardPoolModel
{
    public override string Title => "AK_Exusiai";
    public override string EnergyColorName => "red";
    public override Color DeckEntryCardColor => new("ffffff");
    public override bool IsColorless => false;

    [Obsolete("RitsuLib 0.5.14 marks TypeList pool overrides obsolete, but they remain usable for the initial scaffold.")]
    protected override IEnumerable<Type> CardTypes =>
    [
        typeof(ChargingMode),
        typeof(LockedAndLoaded)
    ];
}
