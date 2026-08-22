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
    private int _spent;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(PatronFirearmPower), ".png");

    public async Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id)
            return;

        _spent += context.Amount;
        int draws = _spent / Amount;
        _spent %= Amount;
        if (draws <= 0 || Owner.Player is not { } player)
            return;

        Flash();
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), draws, player);
    }
}
