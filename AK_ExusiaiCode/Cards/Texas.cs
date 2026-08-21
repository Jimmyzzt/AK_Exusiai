using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ColorlessCardPool))]
public sealed class Texas : ExusiaiCardTemplate, IAmmoFreeAttack
{
    private const string HitCountKey = "HitCount";
    protected override IEnumerable<IHoverTip> CardHoverTips => [HoverTipFactory.FromPower<WeakPower>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2m, ValueProp.Move),
        new DynamicVar(HitCountKey, 2m),
        new PowerVar<WeakPower>(1m),
    ];

    public Texas() : base(0, CardType.Attack, CardRarity.Token, TargetType.AllEnemies, false)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars[HitCountKey].IntValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        foreach (Creature enemy in CombatState!.HittableEnemies)
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, DynamicVars.Weak.BaseValue,
                Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}
