using Godot;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace AK_Exusiai.Content;

public sealed class ExusiaiCardPool : TypeListCardPoolModel
{
    private static readonly Material? PoolFrameTintMaterial =
        MaterialUtils.CreateHsvShaderMaterial(0.998f, 0.961f, 0.819f);

    public override string Title => "exusiai";
    public override string EnergyColorName => "ironclad";
    public override string? TextEnergyIconPath =>
        $"{Entry.ResPath}/images/ui/exusiai_energy_text.svg";
    public override string? BigEnergyIconPath =>
        $"{Entry.ResPath}/images/ui/exusiai_energy_big.svg";
    public override string CardFrameMaterialPath => "card_frame_red";
    public override Material? PoolFrameMaterial => PoolFrameTintMaterial;
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
