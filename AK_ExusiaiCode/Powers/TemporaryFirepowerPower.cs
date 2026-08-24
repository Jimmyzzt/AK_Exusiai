using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class TemporaryFirepowerPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse the existing temporary ammo-damage artwork under its stable resource name.
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom("TemporaryAmmoDamagePower");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<FirepowerPower>()];

    public override async Task BeforeApplied(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await PowerCmd.Apply<FirepowerPower>(
            new ThrowingPlayerChoiceContext(),
            target,
            amount,
            applier,
            cardSource,
            silent: true);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power == this && amount != Amount)
        {
            await PowerCmd.Apply<FirepowerPower>(
                choiceContext,
                Owner,
                amount,
                applier,
                cardSource,
                silent: true);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;

        Flash();
        int amount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<FirepowerPower>(
            choiceContext,
            Owner,
            -amount,
            Owner,
            null);
    }
}
