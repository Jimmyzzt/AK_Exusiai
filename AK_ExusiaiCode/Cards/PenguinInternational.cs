using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PenguinInternational : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override bool ShowTransitHoverTip => true;
    protected override bool ShowDeliveryTransitInteractionHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Delivery", 4m),
        new DynamicVar("Transit", 3m),
        new DynamicVar("TransitCount", 1m),
    ];
    public PenguinInternational() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!await RelicLogisticsCmd.ChooseAndAddDelivery(
                choiceContext,
                Owner,
                DynamicVars["Delivery"].IntValue))
        {
            return;
        }

        IReadOnlyList<RelicModel> transitRelics = await RelicLogisticsCmd.ChooseTransitRelics(
            choiceContext, Owner, DynamicVars["TransitCount"].IntValue);
        foreach (RelicModel transit in transitRelics)
            transit.GetOrCreateCapability<RelicLogisticsCapability>()
                .StartOrExtendTransit(DynamicVars["Transit"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["TransitCount"].UpgradeValueBy(1m);
}
