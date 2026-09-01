using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class LoadEmUp : ExusiaiCardTemplate
{
    protected override bool HasEnergyCostX => true;
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Ammo", 4m)];
    public LoadEmUp() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int ammo = DynamicVars["Ammo"].IntValue * (ResolveEnergyXValue() + (IsUpgraded ? 1 : 0));
        if (ammo > 0)
            await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, ammo, this);
    }

    protected override void OnUpgrade() { }
}
