using AK_Exusiai.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AK_Exusiai.Mechanics;

public static class LogisticsCardCatalog
{
    public static IReadOnlyList<CardModel> CanonicalCards =>
    [
        ModelDb.Card<Emperor>(),
        ModelDb.Card<Texas>(),
        ModelDb.Card<Exusiai>(),
        ModelDb.Card<Croissant>(),
        ModelDb.Card<Mostima>(),
        ModelDb.Card<Sora>(),
        ModelDb.Card<Ylth>(),
        ModelDb.Card<Bison>(),
    ];

    private static IReadOnlyList<CardModel> WeightedCards =>
    [
        ModelDb.Card<Emperor>(),
        ModelDb.Card<Texas>(), ModelDb.Card<Texas>(),
        ModelDb.Card<Exusiai>(), ModelDb.Card<Exusiai>(),
        ModelDb.Card<Croissant>(), ModelDb.Card<Croissant>(),
        ModelDb.Card<Mostima>(),
        ModelDb.Card<Sora>(), ModelDb.Card<Sora>(),
        ModelDb.Card<Ylth>(), ModelDb.Card<Ylth>(),
        ModelDb.Card<Bison>(), ModelDb.Card<Bison>(),
    ];

    public static CardModel Create(Player owner, CardModel canonical) =>
        (owner.Creature.CombatState ?? throw new InvalidOperationException("Cannot create a Logistics card outside combat."))
            .CreateCard(canonical, owner);

    public static CardModel CreateRandom(Player owner)
    {
        CardModel canonical = owner.RunState.Rng.CombatCardGeneration.NextItem(WeightedCards)
            ?? throw new InvalidOperationException("The Logistics card catalog is empty.");
        return Create(owner, canonical);
    }

    public static IReadOnlyList<CardModel> CreateEmperorOptions(Player owner, bool allCards)
    {
        List<CardModel> canonicals = CanonicalCards.Where(card => card is not Emperor).ToList();
        if (!allCards)
        {
            List<CardModel> weighted = WeightedCards.Where(card => card is not Emperor).ToList();
            canonicals.Clear();
            while (weighted.Count > 0 && canonicals.Count < 3)
            {
                CardModel selected = owner.RunState.Rng.CombatCardGeneration.NextItem(weighted)!;
                canonicals.Add(selected);
                weighted.RemoveAll(card => card.Id == selected.Id);
            }
        }

        return canonicals.Select(card => Create(owner, card)).ToList();
    }
}
