using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class DoubleShotKit : ExusiaiCardTemplate
{
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar("HitCount", 2m),
    ];
    public DoubleShotKit() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<TemporaryAmmoDamageMultiplierPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars["HitCount"].IntValue)
            .FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
