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
public sealed class BarrageSkyShattering : ExusiaiCardTemplate, IAmmoDamageMultiplier
{
    private const string HitCountKey = "HitCount";
    private const string AmmoMultiplierKey = "AmmoMultiplier";

    public int AmmoDamageMultiplier => DynamicVars[AmmoMultiplierKey].IntValue;
    protected override bool ShowAmmoHoverTip => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new DynamicVar(HitCountKey, 3m),
        new DynamicVar(AmmoMultiplierKey, 3m),
    ];

    public BarrageSkyShattering()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars[HitCountKey].IntValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "dagger_throw.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() =>
        DynamicVars[AmmoMultiplierKey].UpgradeValueBy(2m);
}
