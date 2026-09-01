using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class ApplePieLogistics : ExusiaiRelicTemplate
{
    private bool _usedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id)];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_usedThisTurn || cardPlay.Player != Owner || cardPlay.Card.Type != CardType.Attack)
            return;

        _usedThisTurn = true;
        int spent = cardPlay.Resources.EnergySpent;
        Flash();
        if (spent > 0)
            await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, spent, this);
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
            _usedThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _usedThisTurn = false;
        return Task.CompletedTask;
    }
}
