using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class SteadfastHeart : ExusiaiCardTemplate
{
    protected override IEnumerable<IHoverTip> CardHoverTips =>
        [HoverTipFactory.FromCard<Injury>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(15m, ValueProp.Move),
        new CardsVar(2),
    ];
    public override bool GainsBlock => true;

    public SteadfastHeart() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            Injury generated = CombatState!.CreateCard<Injury>(Owner);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                generated,
                PileType.Hand,
                Owner));
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);
}
