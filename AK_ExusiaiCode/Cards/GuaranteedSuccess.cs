using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class GuaranteedSuccess : ExusiaiCardTemplate
{
    private const string HitCountKey = "HitCount";
    protected override bool ShowDeliveryHoverTip => true;
    protected override bool ShowTransitHoverTip => true;
    protected override bool ShowDeliveryTransitInteractionHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar(HitCountKey, 5m),
    ];

    public GuaranteedSuccess() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars[HitCountKey].IntValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        RelicModel? selected =
            await RelicLogisticsCmd.ChooseDeliveredOrTransitRelic(choiceContext, Owner);
        if (selected == null)
            return;

        RelicLogisticsCapability? state = selected.Capability<RelicLogisticsCapability>();
        if (state?.IsTransit == true)
            state.StartOrExtendTransit(99);
        else
            RelicLogisticsCmd.ReactivateDelivery(selected);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
