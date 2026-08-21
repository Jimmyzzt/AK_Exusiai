using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class QuickMagazine : ExusiaiCardTemplate
{
    private const string AmmoKey = "Ammo";
    protected override bool ShowAmmoHoverTip => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AmmoKey, 2m),
        new CardsVar(2),
    ];

    public QuickMagazine() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, DynamicVars[AmmoKey].IntValue, this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[AmmoKey].UpgradeValueBy(1m);
    }
}
