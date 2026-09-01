using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class EmbraceNewLife : ExusiaiCardTemplate
{
    protected override IEnumerable<IHoverTip> CardHoverTips =>
    [
        HoverTipFactory.FromCard<HolyCityGuidance>(),
        HoverTipFactory.FromCard<HolyCityPurge>(),
        HoverTipFactory.FromCard<HolyCityIceCream>(),
    ];
    protected override bool HasEnergyCostX => true;
    protected override bool ShowAngelHoverTip => true;

    public EmbraceNewLife() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        for (int i = 0; i < count; i++)
        {
            CardModel generated = AngelCardCatalog.CreateRandom(Owner);
            CardCmd.Enchant<Swift>(generated, 1m);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                generated, PileType.Draw, Owner));
        }
    }
}
