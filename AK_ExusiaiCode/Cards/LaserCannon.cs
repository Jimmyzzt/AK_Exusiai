using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class LaserCannon : ExusiaiCardTemplate, IAmmoDamageMultiplier, IAmmoSpendAllAttack
{
    private const string AmmoMultiplierKey = "AmmoMultiplier";
    public decimal AmmoDamageMultiplier => DynamicVars[AmmoMultiplierKey].IntValue;
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Move),
        new DynamicVar(AmmoMultiplierKey, 2m),
    ];

    public LaserCannon() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars[AmmoMultiplierKey].UpgradeValueBy(1m);
    }
}
