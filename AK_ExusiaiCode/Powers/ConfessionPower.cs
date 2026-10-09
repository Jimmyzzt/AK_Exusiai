using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class ConfessionPower : ModPowerTemplate
{
    private sealed class Data
    {
        public int EtherealExhaustCount;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(ConfessionPower));

    protected override object InitInternalData() => new Data();

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (IsRelevant(card))
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player!);
        }
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (!IsRelevant(card))
            return;

        Flash();
        if (causedByEthereal)
        {
            GetInternalData<Data>().EtherealExhaustCount++;
            return;
        }

        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player!);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;

        Data data = GetInternalData<Data>();
        if (data.EtherealExhaustCount > 0)
            await CardPileCmd.Draw(choiceContext, Amount * data.EtherealExhaustCount, Owner.Player!);

        data.EtherealExhaustCount = 0;
    }

    private bool IsRelevant(CardModel card) =>
        card.Owner.Creature == Owner && (card.Type == CardType.Curse || AngelCmd.IsAngel(card));
}
