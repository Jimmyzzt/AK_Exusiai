using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace AK_Exusiai.Mechanics;

public static class CardCostRandomizationCmd
{
    public static void RandomizeHand(Player player)
    {
        foreach (CardModel card in player.PlayerCombatState!.Hand.Cards.Where(card => !card.EnergyCost.CostsX))
        {
            if (card.EnergyCost.GetWithModifiers(CostModifiers.None) < 0)
                continue;

            card.EnergyCost.SetThisTurnOrUntilPlayed(
                player.RunState.Rng.CombatEnergyCosts.NextInt(4));
            NCard.FindOnTable(card)?.PlayRandomizeCostAnim();
        }
    }
}
