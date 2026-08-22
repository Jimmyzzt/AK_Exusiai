using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class SecureDelivery : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SecureDeliveryPower>(3m)];
    public SecureDelivery() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<SecureDeliveryPower>(choiceContext, Owner.Creature,
            DynamicVars[nameof(SecureDeliveryPower)].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars[nameof(SecureDeliveryPower)].UpgradeValueBy(1m);
}
