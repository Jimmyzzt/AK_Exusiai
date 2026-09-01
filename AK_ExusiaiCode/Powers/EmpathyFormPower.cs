using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class EmpathyFormPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(EmpathyFormPower), ".png");

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player?.PlayerCombatState is not { } state)
            return;

        var context = new ThrowingPlayerChoiceContext();
        foreach (CardModel card in state.AllCards.ToList())
            await AngelCmd.Add(context, card);
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner.Creature == Owner)
            await AngelCmd.Add(new ThrowingPlayerChoiceContext(), card);
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card.Owner.Creature == Owner)
            await AngelCmd.Add(new ThrowingPlayerChoiceContext(), card);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player.Creature != Owner || !AngelCmd.WasFreePlay(cardPlay))
            return;

        Flash();
        CardModel replay = cardPlay.Card.CreateDupe(cardPlay.Card.Owner);
        await CardCmd.AutoPlay(choiceContext, replay, cardPlay.Target, AutoPlayType.Default);
    }
}
