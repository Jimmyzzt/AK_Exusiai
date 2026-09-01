using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Brawl : ExusiaiCardTemplate
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Threshold", 3m)];

    public Brawl() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int interference = cardPlay.Target.GetPower<InterferencePower>()?.Amount ?? 0;
        int amount = interference / DynamicVars["Threshold"].IntValue;
        if (amount > 0)
        {
            await InterferenceCmd.Apply(
                choiceContext,
                cardPlay.Target,
                amount,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Threshold"].UpgradeValueBy(-1m);
}
