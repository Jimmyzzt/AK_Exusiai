using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class OutstandingGraduate : ExusiaiCardTemplate
{
    protected override bool ShowInterferenceHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public OutstandingGraduate() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int multiplier = IsUpgraded ? 3 : 2;
        foreach (var enemy in CombatState!.HittableEnemies.ToList())
        {
            int current = enemy.GetPower<InterferencePower>()?.Amount ?? 0;
            if (current > 0)
            {
                await InterferenceCmd.Apply(
                    choiceContext,
                    enemy,
                    current * (multiplier - 1),
                    Owner.Creature,
                    this);
            }
        }
    }

    protected override void OnUpgrade() { }
}
