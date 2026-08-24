using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;
using AK_Exusiai.Cards;

namespace AK_Exusiai.Mechanics;

[RegisterModelCapability]
[RegisterDefaultModelCapability(typeof(HolyCityPurge))]
[RegisterDefaultModelCapability(typeof(HolyCityGuidance))]
[RegisterDefaultModelCapability(typeof(HolyCityProtection))]
[RegisterDefaultModelCapability(typeof(HolyCityEternal))]
[RegisterDefaultModelCapability(typeof(HolyCityIceCream))]
public sealed class AngelCapability : CardCapability, ICardEnergyCostContributor,
    ICardDescriptionContributor, ICardHoverTipContributor
{
    private const int NoCombatMinimum = int.MaxValue;

    private bool _addedKeyword;
    private int _lowestCombatCost = NoCombatMinimum;

    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context) =>
    [
        new CardDescriptionFragment(
            new LocString("cards", "AK_EXUSIAI_ANGEL.fragment"),
            CardDescriptionFragmentPlacement.BeforeBase,
            0),
    ];

    public IEnumerable<IHoverTip> GetHoverTips(CardModel card) => [ExusiaiKeywords.AngelHoverTip];

    public int ModifyEnergyCost(CardModel card, int currentCost, CostModifiers modifiers)
    {
        if (!ReferenceEquals(card, Owner))
            return currentCost;

        currentCost = Math.Min(currentCost, Math.Max(0, card.EnergyCost.Canonical));
        return !IsInCombatPile(card) || _lowestCombatCost == NoCombatMinimum
            ? currentCost
            : Math.Min(currentCost, _lowestCombatCost);
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

    internal void RecordCombatMinimum(int currentCost)
    {
        int previousMinimum = _lowestCombatCost;
        _lowestCombatCost = Math.Min(_lowestCombatCost, Math.Max(0, currentCost));
        if (_lowestCombatCost != previousMinimum)
            MarkDirty();
    }

    private static bool IsInCombatPile(CardModel card)
    {
        return card.Pile?.IsCombatPile == true;
    }
}

public static class AngelCmd
{
    public static bool IsAngel(CardModel card) =>
        card.Capabilities().Get<AngelCapability>() != null;

    public static void Add(CardModel card)
    {
        card.Capabilities().GetOrCreate<AngelCapability>();
        AscensionCmd.UpgradeIfNeeded(card);
    }

    public static void RecordCurrentCombatCost(CardModel card)
    {
        AngelCapability? capability = card.Capabilities().Get<AngelCapability>();
        if (capability == null || card.Pile?.IsCombatPile != true)
            return;

        int localCost = card.EnergyCost.GetWithModifiers(CostModifiers.Local);
        capability.RecordCombatMinimum(Math.Min(localCost, Math.Max(0, card.EnergyCost.Canonical)));
    }
}
