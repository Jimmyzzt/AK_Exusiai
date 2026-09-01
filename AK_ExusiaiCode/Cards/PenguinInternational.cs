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
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Delivery", 5m),
        new DynamicVar("Transit", 3m),
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

        RelicModel? transit = await RelicLogisticsCmd.ChooseTransitRelic(Owner);
        transit?.GetOrCreateCapability<RelicLogisticsCapability>()
            .StartOrExtendTransit(DynamicVars["Transit"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Transit"].UpgradeValueBy(1m);
}
