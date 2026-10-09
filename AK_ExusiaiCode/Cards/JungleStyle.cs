using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class JungleStyle : ExusiaiCardTemplate
{
    private const string AmmoKey = "Ammo";
    protected override bool ShowAmmoHoverTip => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AmmoKey, 4m),
    ];

    public JungleStyle() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int ammo = DynamicVars[AmmoKey].IntValue;
        await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, ammo, this);
        await PowerCmd.Apply<AmmoNextTurnPower>(choiceContext, Owner.Creature,
            ammo, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[AmmoKey].UpgradeValueBy(1m);
    }
}
