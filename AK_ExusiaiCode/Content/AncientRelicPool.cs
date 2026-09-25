using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Content;

// A registration-only pool. It is not attached to a character's normal relic rewards.
public sealed class AncientRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "exusiai";
}

// Only the Vintages relic may create these potions.
public sealed class AncientWinePotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "exusiai";
}
