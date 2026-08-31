using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class MayTheLordBeWithUs : ExusiaiCardTemplate
{
    protected override bool ShowTransitHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Transit", 1m)];
    public MayTheLordBeWithUs() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await RelicLogisticsCmd.AddNamedTransit<Relics.LordServer>(
            Owner,
            DynamicVars["Transit"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Transit"].UpgradeValueBy(1m);
}
