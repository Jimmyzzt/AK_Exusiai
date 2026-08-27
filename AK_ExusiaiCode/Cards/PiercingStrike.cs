using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PiercingStrike : ExusiaiCardTemplate
{
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
    ];

    public PiercingStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await using AttackContext context = await AttackCommand.CreateContextAsync(
            CombatState!, choiceContext, cardPlay);
        List<DamageResult> primaryResults = (await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Damage.BaseValue,
            ValueProp.Move,
            this,
            cardPlay)).ToList();
        context.AddHit(primaryResults);

        DamageResult? primaryResult = primaryResults.FirstOrDefault();
        if (primaryResult?.Receiver == null ||
            !AK_Exusiai.Characters.Exusiai.DidSpendAmmo(cardPlay))
            return;

        List<Creature> otherEnemies = CombatState!
            .GetTeammatesOf(primaryResult.Receiver)
            .Where(enemy => enemy != cardPlay.Target && enemy.IsHittable)
            .ToList();
        if (otherEnemies.Count == 0)
            return;

        context.AddHit(await CreatureCmd.Damage(
            choiceContext,
            otherEnemies,
            primaryResult.TotalDamage + primaryResult.OverkillDamage,
            ValueProp.Unpowered | ValueProp.Move,
            Owner.Creature,
            this,
            cardPlay));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
