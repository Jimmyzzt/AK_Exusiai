using AK_Exusiai.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace AK_Exusiai.Mechanics;

public static class LogisticsCardCatalog
{
    public static IReadOnlyList<CardModel> CanonicalCards =>
    [
        ModelDb.Card<Emperor>(),
        ModelDb.Card<Texas>(),
        ModelDb.Card<Exusiai>(),
        ModelDb.Card<Croissant>(),
        ModelDb.Card<Sora>(),
        ModelDb.Card<Bison>(),
        ModelDb.Card<Ylth>(),
        ModelDb.Card<Mostima>(),
    ];

    public static IEnumerable<IHoverTip> CreateTextHoverTips(bool upgraded)
    {
        yield return new HoverTip(
            new LocString("static_hover_tips", upgraded
                ? "AK_EXUSIAI_UPGRADED_LOGISTICS_CARD.title"
                : "AK_EXUSIAI_LOGISTICS_CARD.title"),
            new LocString("static_hover_tips", upgraded
                ? "AK_EXUSIAI_UPGRADED_LOGISTICS_CARD.description"
                : "AK_EXUSIAI_LOGISTICS_CARD.description"));

        foreach (CardModel canonical in CanonicalCards)
        {
            CardModel card = canonical;
            if (upgraded)
            {
                card = canonical.ToMutable();
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }

            string suffix = upgraded ? "_UPGRADED" : string.Empty;
            string shortId = canonical.Id.Entry.Replace("AK_EXUSIAI_CARD_", string.Empty);
            yield return new HoverTip(
                new LocString("static_hover_tips", $"AK_EXUSIAI_LOGISTICS_{shortId}{suffix}.title"),
                card.GetDescriptionForPile(PileType.None));
        }
    }

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
