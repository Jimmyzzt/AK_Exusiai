using AK_Exusiai.AK_ExusiaiCode.Relics;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.AK_ExusiaiCode.Character;

public sealed class AKExusiaiRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "red";
    public override Color LabOutlineColor => new("ffffff");

    [Obsolete("RitsuLib 0.5.14 marks TypeList pool overrides obsolete, but they remain usable for the initial scaffold.")]
    protected override IEnumerable<Type> RelicTypes =>
    [
        typeof(ExusiaiBadge)
    ];
}
