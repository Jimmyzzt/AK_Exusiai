using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Content;

// Keep these event-only models discoverable through ModelDb.AllRelicPools.
[RegisterSharedRelicPool]
public sealed class AncientRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "exusiai";
}

// Only the Vintages relic creates these potions, but their descriptions still need a pool.
[RegisterSharedPotionPool]
public sealed class AncientWinePotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "exusiai";
}
