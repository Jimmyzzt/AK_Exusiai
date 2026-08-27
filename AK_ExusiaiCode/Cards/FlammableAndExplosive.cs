using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class FlammableAndExplosive : ExusiaiCardTemplate
{
    private const string AmmoKey = "Ammo";
    protected override bool ShowDeliveryHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FlammableAndExplosivePower>(5m),
        new DynamicVar(AmmoKey, 1m),
    ];
    public FlammableAndExplosive() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FlammableAndExplosivePower>(choiceContext, Owner.Creature,
            DynamicVars[nameof(FlammableAndExplosivePower)].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<FlammableAndExplosiveStacksPower>(
            choiceContext, Owner.Creature, DynamicVars[AmmoKey].BaseValue, Owner.Creature, this);

        FlammableAndExplosivePower? power = Owner.Creature.GetPower<FlammableAndExplosivePower>();
        FlammableAndExplosiveStacksPower? stacks =
            Owner.Creature.GetPower<FlammableAndExplosiveStacksPower>();
        if (power != null && stacks != null)
            power.SetAmmoPerTrigger(stacks.Amount);
    }

    protected override void OnUpgrade() => DynamicVars[AmmoKey].UpgradeValueBy(1m);
}
