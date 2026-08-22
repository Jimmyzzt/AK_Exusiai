using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class FlammableAndExplosivePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(FlammableAndExplosivePower));

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player.Creature != Owner || !DeliveryCmd.HasDelivery(cardPlay.Card))
            return;

        Flash();
        if (Owner.CombatState is { } combatState)
        {
            await CreatureCmd.Damage(
                choiceContext,
                combatState.GetOpponentsOf(Owner).Where(creature => creature.IsAlive),
                Amount,
                ValueProp.Move | ValueProp.Unpowered,
                Owner);
        }

        int ammo = Owner.GetPower<FlammableAndExplosiveStacksPower>()?.Amount ?? 1;
        await SecondaryResourceCmd.Gain(Owner.Player!, AmmoResource.Id, ammo, this);
    }
}

[RegisterPower]
public sealed class FlammableAndExplosiveStacksPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(FlammableAndExplosivePower));
}
