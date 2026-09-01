using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class FlammableAndExplosive : ExusiaiCardTemplate
{
    private const string OverloadBonusKey = "OverloadBonus";
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(OverloadBonusKey, 50m),
    ];
    public FlammableAndExplosive() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FlammableAndExplosivePower>(choiceContext, Owner.Creature,
            DynamicVars[OverloadBonusKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[OverloadBonusKey].UpgradeValueBy(25m);
}
