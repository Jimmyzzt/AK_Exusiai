using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Exusiai : ExusiaiCardTemplate
{
    protected override bool ShowAmmoHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Ammo", 3m),
        new DynamicVar("Heal", 5m),
    ];
    public Exusiai() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, DynamicVars["Ammo"].IntValue, this);
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["Ammo"].UpgradeValueBy(3m);
}
