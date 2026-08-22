using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class EmpathyFormPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(EmpathyFormPower), ".png");

    public override Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        AttachToAllCards();
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        AttachIfOwned(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, MegaCrit.Sts2.Core.Entities.Players.Player? creator)
    {
        AttachIfOwned(card);
        return Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (card.Owner.Creature == Owner &&
            card.Type != CardType.Power &&
            AngelCmd.IsAngel(card))
        {
            Flash();
            CardModel replay = card.CreateDupe(card.Owner);
            await MegaCrit.Sts2.Core.Commands.CardCmd.AutoPlay(
                choiceContext,
                replay,
                null,
                AutoPlayType.Default);
        }
    }

    private void AttachToAllCards()
    {
        if (Owner.Player?.PlayerCombatState is not { } playerCombatState)
            return;

        foreach (CardModel card in playerCombatState.AllCards.ToList())
            AttachIfOwned(card);
    }

    private void AttachIfOwned(CardModel card)
    {
        if (card.Owner.Creature == Owner)
            card.Capabilities().GetOrCreate<AngelCapability>();
    }
}
