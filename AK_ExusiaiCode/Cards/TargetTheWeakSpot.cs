using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class TargetTheWeakSpot : ExusiaiCardTemplate
{
    protected override bool ShowInterferenceHoverTip => true;
    protected override bool ShowFirepowerHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FirepowerPower>(2m)];

    public TargetTheWeakSpot() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (!cardPlay.Target.Powers.Any(power => power.Type == PowerType.Debuff))
            return;

        await PowerCmd.Apply<FirepowerPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(FirepowerPower)].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(FirepowerPower)].UpgradeValueBy(1m);
}
