using AK_Exusiai.Characters;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class OverloadPower : ModPowerTemplate, ISecondaryResourceHookListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(OverloadPower));

    public bool ShouldGainSecondaryResource(SecondaryResourceContext context, decimal amount)
    {
        return context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id;
    }

    public bool ShouldSpendSecondaryResource(SecondaryResourceSpendContext context)
    {
        return context.Player.Creature != Owner || context.Definition.Id != AmmoResource.Id;
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player ||
            !participants.Contains(Owner) ||
            Owner.Player is not { } player)
        {
            return;
        }

        bool retainHalfAmmo = Owner.HasPower<OverloadAmmoRetentionPower>();
        int retainedAmmo = retainHalfAmmo
            ? SecondaryResourceCmd.Get(player, AmmoResource.Id) / 2
            : 0;
        if (Owner.GetPower<OverloadAmmoRetentionPower>() is { } retention)
            await PowerCmd.Remove(retention);

        // Remove Overload before resetting Ammo: while this power is active its
        // resource hook intentionally rejects all Ammo changes.
        await PowerCmd.Remove(this);
        await SecondaryResourceCmd.Set(player, AmmoResource.Id, retainedAmmo, this);

        Exusiai.ClearAmmoTransientState(player);
    }
}
