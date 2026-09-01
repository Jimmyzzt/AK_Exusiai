using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AK_Exusiai.Mechanics;

public static class InterferenceCmd
{
    public static async Task Apply(
        PlayerChoiceContext choiceContext,
        Creature target,
        int amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0 || target.IsDead)
            return;

        int oldAmount = target.GetPower<InterferencePower>()?.Amount ?? 0;
        await PowerCmd.Apply<InterferencePower>(
            choiceContext,
            target,
            amount,
            applier,
            cardSource);
        int newAmount = target.GetPower<InterferencePower>()?.Amount ?? 0;
        int silence = ThresholdCount(newAmount) - ThresholdCount(oldAmount);
        if (silence > 0)
        {
            await PowerCmd.Apply<SilencePower>(
                choiceContext,
                target,
                silence,
                applier,
                cardSource);
        }
    }

    public static async Task Apply(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        int amount,
        Creature? applier,
        CardModel? cardSource)
    {
        foreach (Creature target in targets.ToList())
            await Apply(choiceContext, target, amount, applier, cardSource);
    }

    private static int ThresholdCount(int amount) => amount <= 0 ? 0 : ((amount - 1) / 5) + 1;
}
