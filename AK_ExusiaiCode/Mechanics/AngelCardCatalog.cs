using AK_Exusiai.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AK_Exusiai.Mechanics;

public static class AngelCardCatalog
{
    public static IReadOnlyList<CardModel> CanonicalCards =>
    [
        ModelDb.Card<HolyCityGuidance>(),
        ModelDb.Card<HolyCityPurge>(),
        ModelDb.Card<HolyCityProtection>(),
        ModelDb.Card<HolyCityEternal>(),
        ModelDb.Card<HolyCityIceCream>(),
    ];

    public static CardModel Create(Player owner, CardModel canonical)
    {
        CardModel card = (owner.Creature.CombatState ?? throw new InvalidOperationException("Cannot create an Angel card outside combat."))
            .CreateCard(canonical, owner);
        AscensionCmd.UpgradeIfNeeded(card);
        return card;
    }

    public static CardModel CreateRandom(Player owner)
    {
        CardModel canonical = owner.RunState.Rng.CombatCardGeneration.NextItem(CanonicalCards)
            ?? throw new InvalidOperationException("The Angel card catalog is empty.");
        return Create(owner, canonical);
    }
}
