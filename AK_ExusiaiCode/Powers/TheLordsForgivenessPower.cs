using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class TheLordsForgivenessPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(TheLordsForgivenessPower));

    public override async Task BeforeSideTurnEnd(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || Owner.Player is not { } player)
            return;

        int retainedAngels = player.PlayerCombatState?.Hand.Cards.Count(AngelCmd.IsAngel) ?? 0;
        if (retainedAngels <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, retainedAngels * Amount, ValueProp.Unpowered, null);
    }
}
