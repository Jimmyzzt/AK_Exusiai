using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PenguinFreight : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override bool ShowDeliveryTransitInteractionHoverTip => true;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Delivery", 99m),
        new BlockVar(60m, ValueProp.Move),
    ];
    public PenguinFreight() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!await RelicLogisticsCmd.ChooseAndAddDelivery(
                choiceContext,
                Owner,
                DynamicVars["Delivery"].IntValue))
            return;

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<BlurPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(15m);
}
