using AK_Exusiai.AK_ExusiaiCode.Character;
using AK_Exusiai.AK_ExusiaiCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.AK_ExusiaiCode.Cards;

[RegisterCard(typeof(AKExusiaiCardPool))]
public sealed class LockedAndLoaded : AKExusiaiCard
{
    private const string AmmoVarName = "A";

    public LockedAndLoaded() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AmmoPower>(AmmoVarName, 3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<AmmoPower>(
            context,
            play.Player.Creature,
            DynamicVars[AmmoVarName].IntValue,
            play.Player.Creature,
            this,
            false);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[AmmoVarName].UpgradeValueBy(2);
    }
}
