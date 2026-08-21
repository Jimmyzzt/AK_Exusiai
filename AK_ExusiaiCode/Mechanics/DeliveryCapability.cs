using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

[RegisterModelCapability]
public sealed class DeliveryCapability : CardCapability, ICardDescriptionContributor
{
    public const string DeliveryKey = "Delivery";

    private bool _addedRetain;
    private bool _addedExhaust;
    private bool _addedDeliveryKeyword;

    public int Amount => DynamicVars[DeliveryKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(DeliveryKey, 0m),
    ];

    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
    {
        if (Amount <= 0)
            return [];

        return
        [
            new CardDescriptionFragment(new LocString("cards", "AK_EXUSIAI_DELIVERY.fragment")),
        ];
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        CardModel? card = Owner;
        if (card == null ||
            side != CombatSide.Player ||
            card.Pile?.Type != PileType.Hand ||
            !participants.Contains(card.Owner.Creature))
        {
            return;
        }

        await ReduceAndMaybeAutoPlay(choiceContext, 1);
    }

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        SetAmount(Amount + amount);
        EnsureKeywords();
    }

    public void Set(int amount)
    {
        SetAmount(Math.Max(0, amount));
        if (Amount > 0)
            EnsureKeywords();
    }

    public async Task ReduceAndMaybeAutoPlay(PlayerChoiceContext choiceContext, int amount)
    {
        if (amount <= 0 || Amount <= 0 || Owner == null)
            return;

        SetAmount(Math.Max(0, Amount - amount));
        if (Amount > 0)
        {
            EnsureKeywords();
            return;
        }

        CardModel card = Owner;
        try
        {
            // Keep Exhaust active until AutoPlay has routed both playable and unplayable cards.
            await CardCmd.AutoPlay(choiceContext, card, null, AutoPlayType.Default);
        }
        finally
        {
            RemoveFromOwner();
        }
    }

    protected override JsonNode SaveAdditionalState()
    {
        return new JsonObject
        {
            ["addedRetain"] = _addedRetain,
            ["addedExhaust"] = _addedExhaust,
            ["addedDeliveryKeyword"] = _addedDeliveryKeyword,
        };
    }

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        if (state is not JsonObject obj)
            return;

        _addedRetain = obj["addedRetain"]?.GetValue<bool>() ?? false;
        _addedExhaust = obj["addedExhaust"]?.GetValue<bool>() ?? false;
        _addedDeliveryKeyword = obj["addedDeliveryKeyword"]?.GetValue<bool>() ?? false;
    }

    protected override void OnDetach(CardModel owner)
    {
        if (_addedRetain)
            owner.RemoveKeyword(CardKeyword.Retain);
        if (_addedExhaust)
            owner.RemoveKeyword(CardKeyword.Exhaust);
        if (_addedDeliveryKeyword)
            owner.RemoveModKeyword(ExusiaiKeywords.DeliveryKeyword);
    }

    private void EnsureKeywords()
    {
        CardModel? card = Owner;
        if (card == null || Amount <= 0)
            return;

        IReadOnlySet<CardKeyword> local = card.GetKeywordsWithSources(KeywordSources.Local);
        if (!local.Contains(CardKeyword.Retain))
        {
            card.AddKeyword(CardKeyword.Retain);
            _addedRetain = true;
        }

        if (!local.Contains(CardKeyword.Exhaust))
        {
            card.AddKeyword(CardKeyword.Exhaust);
            _addedExhaust = true;
        }

        if (!card.HasModKeyword(ExusiaiKeywords.DeliveryKeyword))
        {
            card.AddModKeyword(ExusiaiKeywords.DeliveryKeyword);
            _addedDeliveryKeyword = true;
        }

        MarkDirty();
    }

    private void SetAmount(int amount)
    {
        DynamicVars[DeliveryKey].BaseValue = amount;
        MarkDirty();
    }
}

public static class DeliveryCmd
{
    public static bool HasDelivery(CardModel card)
    {
        return card.Capabilities().Get<DeliveryCapability>()?.Amount > 0;
    }

    public static void Add(CardModel card, int amount)
    {
        if (amount <= 0)
            return;

        DeliveryCapability capability = card.Capabilities().GetOrCreate<DeliveryCapability>();
        capability.Add(amount);
    }

    public static void Set(CardModel card, int amount)
    {
        DeliveryCapability capability = card.Capabilities().GetOrCreate<DeliveryCapability>();
        capability.Set(amount);
    }

    public static async Task Reduce(PlayerChoiceContext choiceContext, CardModel card, int amount)
    {
        DeliveryCapability? capability = card.Capabilities().Get<DeliveryCapability>();
        if (capability != null)
            await capability.ReduceAndMaybeAutoPlay(choiceContext, amount);
    }
}
