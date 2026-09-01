using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class AngelsHeart : ExusiaiCardTemplate
{
    protected override IEnumerable<IHoverTip> CardHoverTips =>
    [
        HoverTipFactory.FromCard<HolyCityGuidance>(),
        HoverTipFactory.FromCard<HolyCityPurge>(),
        HoverTipFactory.FromCard<HolyCityIceCream>(),
    ];
    protected override bool ShowAngelHoverTip => true;
    public AngelsHeart() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var canonical in AngelCardCatalog.CanonicalCards)
        {
            var handCopy = AngelCardCatalog.Create(Owner, canonical);
            if (IsUpgraded)
                CardCmd.Upgrade(handCopy);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                handCopy, PileType.Hand, Owner));
        }
    }

    protected override void OnUpgrade() { }
}
