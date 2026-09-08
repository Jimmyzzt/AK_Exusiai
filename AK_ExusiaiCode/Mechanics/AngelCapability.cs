using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

[RegisterModelCapability]
[RegisterDefaultModelCapability(typeof(Cards.HolyCityPurge))]
[RegisterDefaultModelCapability(typeof(Cards.HolyCityGuidance))]
[RegisterDefaultModelCapability(typeof(Cards.HolyCityIceCream))]
[RegisterDefaultModelCapability(typeof(Cards.HolyCityRadiance))]
[RegisterDefaultModelCapability(typeof(Cards.SwearOnThisGun))]
[RegisterDefaultModelCapability(typeof(Cards.ApplePieWithCharSiu))]
[RegisterDefaultModelCapability(typeof(Cards.HolyCityEmbrace))]
public sealed class AngelCapability : CardCapability, ICardDescriptionContributor,
    ICardHoverTipContributor, ICardPropertyContributor
{
    private static readonly ConditionalWeakTable<CardPlay, AngelFreePlayMarker> FreePlays = new();

    private bool _hasFreePlay = true;
    private bool _addedKeyword;
    private bool _addedRetain;

    public bool HasFreePlay => _hasFreePlay;

    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context) =>
    [
        new CardDescriptionFragment(
            new LocString("cards", "AK_EXUSIAI_ANGEL.fragment"),
            CardDescriptionFragmentPlacement.BeforeBase,
            0),
    ];

    public IEnumerable<IHoverTip> GetHoverTips(CardModel card) => [ExusiaiKeywords.AngelHoverTip];

    public TargetType? GetTargetType(CardModel card) =>
        card.CombatState != null &&
        card.Type == CardType.Power &&
        card.Owner.Creature.HasPower<Powers.CompassionPower>()
            ? TargetType.AnyPlayer
            : null;

    public override int ModifyCardPlayCount(CardModel card, MegaCrit.Sts2.Core.Entities.Creatures.Creature? target, int playCount) =>
        CompassionTransferCmd.IsTransfer(card, target) ? 1 : playCount;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (ReferenceEquals(cardPlay.Card, Owner) &&
            _hasFreePlay &&
            !Owner.EnergyCost.CostsX)
        {
            _hasFreePlay = false;
            FreePlays.Add(cardPlay, new AngelFreePlayMarker());
            MarkDirty();
        }

        return Task.CompletedTask;
    }

    public void Refresh()
    {
        _hasFreePlay = true;
        EnsurePresentation();
        MarkDirty();
    }

    public void ConsumeFreePlay()
    {
        if (!_hasFreePlay)
            return;

        _hasFreePlay = false;
        MarkDirty();
    }

    protected override JsonNode SaveAdditionalState() => new JsonObject
    {
        ["hasFreePlay"] = _hasFreePlay,
    };

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        if (state is not JsonObject obj)
            return;

        _hasFreePlay = obj["hasFreePlay"]?.GetValue<bool>() ?? true;
    }

    protected override void OnAttach(CardModel owner) => EnsurePresentation();

    protected override void OnLoadedFromSave(CardModel owner) => EnsurePresentation();

    protected override void OnDetach(CardModel owner)
    {
        if (_addedRetain)
            owner.RemoveKeyword(CardKeyword.Retain);
        if (_addedKeyword)
            owner.RemoveModKeyword(ExusiaiKeywords.AngelKeyword);
    }

    private void EnsurePresentation()
    {
        CardModel? card = Owner;
        if (card == null || card.IsCanonical)
            return;

        IReadOnlySet<CardKeyword> local = card.GetKeywordsWithSources(KeywordSources.Local);
        if (!local.Contains(CardKeyword.Retain))
        {
            card.AddKeyword(CardKeyword.Retain);
            _addedRetain = true;
        }

        if (!card.HasModKeyword(ExusiaiKeywords.AngelKeyword))
        {
            card.AddModKeyword(ExusiaiKeywords.AngelKeyword);
            _addedKeyword = true;
        }
    }

    internal static bool WasConsumedFor(CardPlay cardPlay) => FreePlays.TryGetValue(cardPlay, out _);

    private sealed class AngelFreePlayMarker;
}

public static class AngelCmd
{
    public static bool IsAngel(CardModel card) =>
        card.Capabilities().Get<AngelCapability>() != null;

    public static bool HasFreePlay(CardModel card) =>
        card.Capabilities().Get<AngelCapability>()?.HasFreePlay == true;

    public static bool WasFreePlay(CardPlay cardPlay) =>
        AngelCapability.WasConsumedFor(cardPlay);

    public static void ConsumeFreePlay(CardModel card) =>
        card.Capabilities().Get<AngelCapability>()?.ConsumeFreePlay();

    public static async Task Add(PlayerChoiceContext choiceContext, CardModel card)
    {
        AngelCapability capability = card.Capabilities().GetOrCreate<AngelCapability>();
        capability.Refresh();

        if (card.Type == CardType.Curse && card.Pile?.IsCombatPile == true)
            await CardCmd.Exhaust(choiceContext, card);
    }
}
