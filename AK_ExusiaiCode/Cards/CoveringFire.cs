using AK_Exusiai.Characters;
using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class CoveringFire : ExusiaiCardTemplate, IMultiAmmoAttack
{
    public int MaxAmmoSpend => 3;
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<IHoverTip> CardHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar("HitCount", 3m),
        new DynamicVar("StrengthLoss", 2m),
    ];
    public CoveringFire() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars["HitCount"].IntValue)
            .FromCard(this, cardPlay).TargetingAllOpponents(CombatState!)
            .WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        int ammoSpent = AK_Exusiai.Characters.Exusiai.GetAmmoMultiplier(cardPlay);
        if (ammoSpent <= 0)
            return;
        foreach (var enemy in CombatState!.HittableEnemies)
        {
            for (int i = 0; i < ammoSpent; i++)
            {
                await PowerCmd.Apply<CoveringFirePower>(choiceContext, enemy,
                    DynamicVars["StrengthLoss"].BaseValue, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars["StrengthLoss"].UpgradeValueBy(1m);
}
