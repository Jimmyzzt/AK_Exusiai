using AK_Exusiai.Cards;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards;

namespace AK_Exusiai.Mechanics;

public static class CompassionTransferCmd
{
    public static bool IsTransfer(CardModel card, Creature? target) =>
        target?.Player is { } recipient &&
        recipient != card.Owner &&
        card.Type == CardType.Power &&
        AngelCmd.IsAngel(card) &&
        card.Owner.Creature.HasPower<Powers.CompassionPower>();

    public static async Task PlayCardEffect(
        CardModel card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (!IsTransfer(card, cardPlay.Target) || cardPlay.Target?.Player is not { } recipient)
        {
            await CardOnPlayHook.RunCardOnPlayHooks(card, choiceContext, cardPlay);
            return;
        }

        // Compassion normally affects every ally. A transferred copy must affect only
        // the teammate selected by the original card play.
        if (card is Compassion)
        {
            await PowerCmd.Apply<CompassionPower>(choiceContext, recipient.Creature,
                card.DynamicVars["Cards"].BaseValue, card.Owner.Creature, card);
            return;
        }

        // Run only the effect with the recipient as the card owner. AutoPlay would
        // start another complete card play while the original one is still active.
        CardModel transferred = card.CreateCloneForPlayer(recipient);
        CardPlay transferredPlay = new()
        {
            Card = transferred,
            Player = recipient,
            Target = recipient.Creature,
            ResultPile = cardPlay.ResultPile,
            Resources = cardPlay.Resources,
            IsAutoPlay = cardPlay.IsAutoPlay,
            PlayIndex = cardPlay.PlayIndex,
            PlayCount = cardPlay.PlayCount,
        };
        await CardPileCmd.Add(transferred, PileType.Play, skipVisuals: true);
        try
        {
            await CardOnPlayHook.RunCardOnPlayHooks(transferred, choiceContext, transferredPlay);
        }
        finally
        {
            transferred.RemoveFromState();
        }
    }

    public static Task PlayEnchantmentEffect(
        EnchantmentModel enchantment,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) =>
        IsTransfer(cardPlay.Card, cardPlay.Target)
            ? Task.CompletedTask
            : enchantment.OnPlay(choiceContext, cardPlay);

    public static Task PlayAfflictionEffect(
        AfflictionModel affliction,
        PlayerChoiceContext choiceContext,
        Creature? target) =>
        IsTransfer(affliction.Card, target)
            ? Task.CompletedTask
            : affliction.OnPlay(choiceContext, target);
}
