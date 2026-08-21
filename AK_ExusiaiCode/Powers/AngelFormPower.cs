using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class AngelFormPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Echo;

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

    public override int ModifyCardPlayCount(CardModel card, MegaCrit.Sts2.Core.Entities.Creatures.Creature? target, int playCount)
    {
        if (card.Owner.Creature == Owner &&
            card.Type != CardType.Power &&
            card.HasModKeyword(ExusiaiKeywords.AngelKeyword) &&
            card.Keywords.Contains(CardKeyword.Exhaust))
        {
            return playCount + 1;
        }

        return playCount;
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
