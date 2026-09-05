using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using AK_Exusiai.Mechanics;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class PatronFirearmPower : ModPowerTemplate, ISecondaryResourceHookListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(PatronFirearmPower));

    public async Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id)
            return;

        Flash();
        await CreatureCmd.GainBlock(
            Owner,
            new MegaCrit.Sts2.Core.Localization.DynamicVars.BlockVar(
                Amount * context.Amount,
                MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered),
            null);
    }
}
