using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class TheLordsMercyPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(TheLordsMercyPower));

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
        if (IsRelevant(card))
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player!);
        }
    }

    private bool IsRelevant(CardModel card) =>
        card.Owner.Creature == Owner && (card.Type == CardType.Curse || AngelCmd.IsAngel(card));
}
