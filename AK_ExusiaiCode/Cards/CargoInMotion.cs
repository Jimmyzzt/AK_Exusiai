using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class CargoInMotion : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override bool ShowTransitHoverTip => true;

    public CargoInMotion() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CargoInMotionPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        if (IsUpgraded)
        {
            await PowerCmd.Apply<CargoInMotionTransitPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() { }
}
