using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PenguinStandard : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override bool ShowTransitHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Delivery", 7m),
        new DynamicVar("Transit", 1m),
    ];
    public PenguinStandard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!await RelicLogisticsCmd.ChooseAndAddDelivery(
                choiceContext,
                Owner,
                DynamicVars["Delivery"].IntValue))
            return;

        await RelicLogisticsCmd.AddRandomTransit(Owner, DynamicVars["Transit"].IntValue);
        await RelicLogisticsCmd.AddRandomTransit(Owner, DynamicVars["Transit"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Transit"].UpgradeValueBy(1m);
}
