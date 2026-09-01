using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PaganiniCustom : ExusiaiCardTemplate
{
    private const string AmmoKey = "Ammo";
    private const string AmmoMultiplierKey = "AmmoMultiplier";
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AmmoKey, 2m),
        new DynamicVar(AmmoMultiplierKey, 50m),
    ];

    public PaganiniCustom() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AmmoDamageMultiplierPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[AmmoMultiplierKey].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<PaganiniCustomPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[AmmoKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[AmmoKey].UpgradeValueBy(1m);
    }
}
