using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class OverloadPower : ModPowerTemplate, ISecondaryResourceHookListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Ammo;

    public bool ShouldGainSecondaryResource(SecondaryResourceContext context, decimal amount)
    {
        return context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id;
    }

    public bool ShouldSpendSecondaryResource(SecondaryResourceSpendContext context)
    {
        return context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id;
    }
}
