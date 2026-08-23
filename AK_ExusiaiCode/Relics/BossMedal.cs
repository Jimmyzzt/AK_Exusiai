using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class BossMedal : ExusiaiRelicTemplate
{
    private bool _usedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6m, ValueProp.Unpowered)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
        ExusiaiKeywords.DeliveryHoverTip,
    ];

    internal async Task AfterDeliveryAdded(PlayerChoiceContext choiceContext)
    {
        if (_usedThisTurn)
            return;

        _usedThisTurn = true;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null, fast: true);
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
