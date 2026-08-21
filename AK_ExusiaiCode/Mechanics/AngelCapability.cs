using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;
using AK_Exusiai.Cards;

namespace AK_Exusiai.Mechanics;

[RegisterModelCapability]
[RegisterDefaultModelCapability(typeof(HolyCityPurification))]
public sealed class AngelCapability : CardCapability, ICardEnergyCostContributor
{
    private bool _addedKeyword;

    public int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
    {
        return ReferenceEquals(card, Owner)
            ? Math.Min(currentCost, card.EnergyCost.Canonical)
            : currentCost;
    }

    public override bool TryModifyEnergyCostInCombatLate(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        if (!ReferenceEquals(card, Owner))
        {
            modifiedCost = originalCost;
            return false;
        }

        modifiedCost = Math.Min(originalCost, card.EnergyCost.Canonical);
        return modifiedCost != originalCost;
    }

    protected override void OnAttach(CardModel owner)
    {
        if (owner.HasModKeyword(ExusiaiKeywords.AngelKeyword))
            return;

        owner.AddModKeyword(ExusiaiKeywords.AngelKeyword);
        _addedKeyword = true;
    }

    protected override void OnDetach(CardModel owner)
    {
        if (_addedKeyword)
            owner.RemoveModKeyword(ExusiaiKeywords.AngelKeyword);
    }
}
