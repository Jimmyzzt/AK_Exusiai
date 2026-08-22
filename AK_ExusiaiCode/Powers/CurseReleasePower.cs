using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class CurseReleasePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Energy;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    private static bool IsAffected(CardModel card) =>
        card.Type == CardType.Curse &&
        card.EnergyCost.Canonical < 0;

    public override Task AfterApplied(MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player?.PlayerCombatState is { } playerCombatState)
        {
            foreach (CardModel card in playerCombatState.AllCards.ToList())
                MakePlayable(card);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        MakePlayable(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(
        CardModel card,
        MegaCrit.Sts2.Core.Entities.Players.Player? creator)
    {
        MakePlayable(card);
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombatLate(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = card.Owner.Creature == Owner && IsAffected(card) ? 1m : originalCost;
        return modifiedCost != originalCost;
    }

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card.Owner.Creature != Owner || !IsAffected(card))
            return false;

        bool changed = keywords.Remove(CardKeyword.Unplayable);
        changed |= keywords.Add(CardKeyword.Exhaust);
        return changed;
    }

    private void MakePlayable(CardModel card)
    {
        if (card.Owner.Creature == Owner && IsAffected(card))
            card.EnergyCost.SetCustomBaseCost(1);
    }
}
