using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class LogisticsSupportPower : ModPowerTemplate, ISecondaryResourceHookListener
{
    private int _spent;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(LogisticsSupportPower));
    public override int DisplayAmount => Math.Max(0, Amount - _spent);

    public async Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context)
    {
        if (context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id)
            return;

        _spent += context.Amount;
        int draws = _spent / Amount;
        _spent %= Amount;
        InvokeDisplayAmountChanged();
        if (draws <= 0 || Owner.Player is not { } player)
            return;

        Flash();
        await MegaCrit.Sts2.Core.Commands.CardPileCmd.Draw(
            new MegaCrit.Sts2.Core.GameActions.Multiplayer.BlockingPlayerChoiceContext(),
            draws,
            player);
    }
}
