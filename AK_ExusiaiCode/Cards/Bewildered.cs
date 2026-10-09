using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(CurseCardPool))]
public sealed class Bewildered : ExusiaiCardTemplate
{
    private int _cardsInHand;

    public override int MaxUpgradeLevel => 0;
    public override bool HasTurnEndInHandEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> CardHoverTips => [HoverTipFactory.FromCard<Regret>()];

    public Bewildered() : base(1, CardType.Curse, CardRarity.Curse, TargetType.Self) { }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature) && Pile?.Type == PileType.Hand)
            _cardsInHand = Pile.Cards.Count;
        return Task.CompletedTask;
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            _cardsInHand,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this,
            null);
        _cardsInHand = 0;
    }

    protected override void OnUpgrade() { }
}
