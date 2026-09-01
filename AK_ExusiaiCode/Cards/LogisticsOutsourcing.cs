using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class LogisticsOutsourcing : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Selections", 1m)];

    public LogisticsOutsourcing() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<LogisticsOutsourcingPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        if (DynamicVars["Selections"].IntValue > 1)
        {
            await PowerCmd.Apply<LogisticsOutsourcingSelectionPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["Selections"].BaseValue - 1m,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Selections"].UpgradeValueBy(1m);
}
