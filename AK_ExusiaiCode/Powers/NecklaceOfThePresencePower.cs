using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using AK_Exusiai.Mechanics;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class NecklaceOfThePresencePower : ModPowerTemplate, ISecondaryResourceHookListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Ammo;

    public async Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id)
            return;

        Flash();
        await CreatureCmd.GainBlock(
            Owner,
            new BlockVar(Amount * context.Amount, ValueProp.Move),
            null);
    }
}
