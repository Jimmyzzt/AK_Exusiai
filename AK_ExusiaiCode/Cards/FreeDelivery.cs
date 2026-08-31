using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using AK_Exusiai.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class FreeDelivery : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("FreeCards", 1m),
    ];

    public FreeDelivery() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!await RelicLogisticsCmd.AddRandomDelivery(choiceContext, Owner, 1))
            return;

        bool alreadyHadFreeCards = Owner.Creature.HasPower<FreeCardsPower>();
        await PowerCmd.Apply<FreeCardsPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["FreeCards"].BaseValue,
            Owner.Creature,
            this);
        if (!alreadyHadFreeCards)
            Owner.Creature.GetPower<FreeCardsPower>()?.IgnoreSourceCard(this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FreeCards"].UpgradeValueBy(1m);
    }
}
