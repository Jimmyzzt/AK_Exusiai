using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Talent : ExusiaiCardTemplate
{
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<AmmoDamagePower>(1m)];
    public Talent() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        await PowerCmd.Apply<AmmoDamagePower>(choiceContext, Owner.Creature,
            DynamicVars[nameof(AmmoDamagePower)].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars[nameof(AmmoDamagePower)].UpgradeValueBy(1m);
}
