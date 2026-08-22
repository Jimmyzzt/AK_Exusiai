using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PartyTime : ExusiaiCardTemplate
{
    private const string HitCountKey = "HitCount";
    protected override IEnumerable<IHoverTip> CardHoverTips => [HoverTipFactory.FromCard<PoorSleep>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar(HitCountKey, 25m),
    ];

    public PartyTime() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (int i = 0; i < DynamicVars[HitCountKey].IntValue; i++)
        {
            var targets = CombatState!.HittableEnemies;
            if (targets.Count == 0)
                break;
            var target = Owner.RunState.Rng.CombatTargets.NextItem(targets);
            if (target == null)
                break;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        PoorSleep curse = CombatState!.CreateCard<PoorSleep>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
            curse, PileType.Hand, Owner));
    }

    protected override void OnUpgrade() => DynamicVars[HitCountKey].UpgradeValueBy(5m);
}
