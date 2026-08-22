using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Package : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new BlockVar(4m, ValueProp.Move),
    ];

    public Package() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        DeliveryCmd.Set(this, 2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
}
