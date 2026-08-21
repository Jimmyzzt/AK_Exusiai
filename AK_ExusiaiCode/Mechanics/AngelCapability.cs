using System.Text.Json.Nodes;
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
    private const int NoCombatMinimum = int.MaxValue;

    private bool _addedKeyword;
    private int _lowestCombatCost = NoCombatMinimum;

    public int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
    {
        if (!ReferenceEquals(card, Owner))
            return currentCost;

        if (!IsInCombatPile(card))
            return Math.Min(currentCost, card.EnergyCost.Canonical);

        return RecordAndClamp(currentCost);
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

        int currentCost = Math.Max(0, (int)originalCost);
        modifiedCost = IsInCombatPile(card)
            ? RecordAndClamp(currentCost)
            : Math.Min(originalCost, card.EnergyCost.Canonical);
        return modifiedCost != originalCost;
    }

    protected override JsonNode SaveAdditionalState()
    {
        return new JsonObject
        {
            ["lowestCombatCost"] = _lowestCombatCost == NoCombatMinimum
                ? -1
                : _lowestCombatCost,
        };
    }

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        int savedCost = (state as JsonObject)?["lowestCombatCost"]?.GetValue<int>() ?? -1;
        _lowestCombatCost = savedCost >= 0 ? savedCost : NoCombatMinimum;
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

    private int RecordAndClamp(int currentCost)
    {
        int previousMinimum = _lowestCombatCost;
        _lowestCombatCost = Math.Min(_lowestCombatCost, Math.Max(0, currentCost));
        if (_lowestCombatCost != previousMinimum)
            MarkDirty();

        return _lowestCombatCost;
    }

    private static bool IsInCombatPile(CardModel card)
    {
        return card.Pile?.IsCombatPile == true;
    }
}
