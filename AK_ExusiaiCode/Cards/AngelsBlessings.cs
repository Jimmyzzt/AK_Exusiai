using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class AngelsBlessings : ExusiaiCardTemplate
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<IHoverTip> CardHoverTips =>
    [
        HoverTipFactory.FromCard<HolyCityGuidance>(),
        HoverTipFactory.FromCard<HolyCityPurge>(),
        HoverTipFactory.FromCard<HolyCityIceCream>(),
    ];
    protected override bool ShowAngelHoverTip => true;
    public AngelsBlessings() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var player in CombatState?.Players ?? [])
        {
            for (int i = 0; i < 2; i++)
            {
                CardModel card = Mechanics.AngelCardCatalog.CreateRandom(player);
                CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                    card, PileType.Hand, Owner));
            }
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
