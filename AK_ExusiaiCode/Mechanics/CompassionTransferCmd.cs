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

        CardModel transferred = card.CreateCloneForPlayer(recipient);
        await CardCmd.AutoPlay(choiceContext, transferred, recipient.Creature, AutoPlayType.Default);
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
