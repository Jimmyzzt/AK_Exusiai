using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class FreeDelivery : ExusiaiCardTemplate
{
    protected override bool ShowTransitHoverTip => true;
    protected override IEnumerable<IHoverTip> CardHoverTips
    {
        get
        {
            RelicModel? relic = GetRevealedRelic();
            return relic == null
                ? [ExusiaiKeywords.MysteryRelicHoverTip]
                : HoverTipFactory.FromRelic(relic);
        }
    }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar("Transit", 1m),
    ];

    public FreeDelivery() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        RelicModel? mysteryRelic = MysteryRelicCmd.Reveal(Owner);
        if (mysteryRelic != null)
        {
            Owner.PlayerCombatState?.RecalculateCardValues();
            await RelicLogisticsCmd.AddNamedTransit(Owner, mysteryRelic, DynamicVars["Transit"].IntValue);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Transit"].UpgradeValueBy(1m);

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add(
            "MysteryRelic",
            GetRevealedRelic()?.Title.GetFormattedText() ?? ExusiaiKeywords.MysteryRelicName);
    }

    private RelicModel? GetRevealedRelic()
    {
        if (!IsMutable)
            return null;

        // Mutable preview clones can briefly exist before an owner is assigned.
        Player? owner = Owner;
        return owner == null ? null : MysteryRelicCmd.GetRevealed(owner);
    }
}
