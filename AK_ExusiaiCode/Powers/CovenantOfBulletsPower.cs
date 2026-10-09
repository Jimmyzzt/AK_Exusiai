using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class CovenantOfBulletsPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile =>
        ExusiaiPowerAssets.Custom(nameof(CovenantOfBulletsPower), ".png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player.PlayerCombatState is not { } state)
            return;

        int bonus = 0;
        if (Owner.GetPower<CovenantOfBulletsBonusPower>() is { } bonusPower)
        {
            bonus = 2;
            await PowerCmd.Decrement(bonusPower);
        }

        Flash();
        await SecondaryResourceCmd.Gain(
            player, AmmoResource.Id, state.Hand.Cards.Count + bonus, this);
        await PowerCmd.Decrement(this);
    }
}

[RegisterPower]
public sealed class CovenantOfBulletsBonusPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Ammo;
}
