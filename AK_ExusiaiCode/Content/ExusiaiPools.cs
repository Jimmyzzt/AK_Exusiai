using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Content;

public sealed class ExusiaiCardPool : TypeListCardPoolModel
{
    public override string Title => "exusiai";
    public override string EnergyColorName => "ironclad";
    public override string CardFrameMaterialPath => "card_frame_red";
    public override Color DeckEntryCardColor => new("D73545");
    public override Color EnergyOutlineColor => new("7A1521");
    public override bool IsColorless => false;
}

public sealed class ExusiaiRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => new("D73545");
}

public sealed class ExusiaiPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => new("D73545");
}
