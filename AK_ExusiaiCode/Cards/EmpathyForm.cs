using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class EmpathyForm : ExusiaiCardTemplate
{
    protected override bool ShowAngelHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<EmpathyFormPower>(1m)];

    public EmpathyForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EmpathyFormPower>(
            choiceContext, Owner.Creature,
            DynamicVars[nameof(EmpathyFormPower)].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade() =>
        DynamicVars[nameof(EmpathyFormPower)].UpgradeValueBy(1m);
}
