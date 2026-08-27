using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class FlammableAndExplosivePower : ModPowerTemplate
{
    private const string AmmoKey = "Ammo";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(FlammableAndExplosivePower));
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(AmmoKey, 1m)];

    public void SetAmmoPerTrigger(int amount) => DynamicVars[AmmoKey].BaseValue = amount;

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

        int ammo = Owner.GetPower<FlammableAndExplosiveStacksPower>()?.Amount ??
                   DynamicVars[AmmoKey].IntValue;
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
