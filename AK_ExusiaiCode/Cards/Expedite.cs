using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Expedite : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override bool ShowTransitHoverTip => IsUpgraded;
    protected override bool ShowDeliveryTransitInteractionHoverTip => IsUpgraded;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Reduction", 1m)];
    public Expedite() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RelicLogisticsCmd.ReduceAllDelivery(Owner, DynamicVars["Reduction"].IntValue);
        if (IsUpgraded)
            RelicLogisticsCmd.ExtendAllTransit(Owner, 1);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() { }
}
