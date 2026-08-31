using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class BossMedal : ExusiaiRelicTemplate
{
    [SavedProperty]
    private int LastTriggeredTurn { get; set; } = -1;

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(10m, ValueProp.Unpowered)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
        ExusiaiKeywords.DeliveryHoverTip,
    ];

    internal async Task AfterDeliveryAdded(PlayerChoiceContext choiceContext)
    {
        int turn = Owner.PlayerCombatState?.TurnNumber ?? -1;
        if (LastTriggeredTurn == turn)
            return;

        LastTriggeredTurn = turn;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null, fast: true);
    }

    internal void ResetAfterCombat()
    {
        LastTriggeredTurn = -1;
    }
}
