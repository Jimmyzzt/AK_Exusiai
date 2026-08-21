using AK_Exusiai.AK_ExusiaiCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.AK_ExusiaiCode.Cards;

[RegisterCard(typeof(AKExusiaiCardPool))]
public sealed class ChargingMode : AKExusiaiCard
{
    public ChargingMode() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, default(ValueProp))
    ];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        for (var i = 0; i < 3; i++)
        {
            await CreatureCmd.Damage(context, play.Target!, DynamicVars.Damage, play.Player.Creature, this, play);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
    }
}
