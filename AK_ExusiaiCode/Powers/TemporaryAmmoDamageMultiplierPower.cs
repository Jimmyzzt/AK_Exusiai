using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class TemporaryAmmoDamageMultiplierPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse the Double-Shot Kit artwork under its stable resource name.
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom("TemporaryExtraAmmoTriggerPower");

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
