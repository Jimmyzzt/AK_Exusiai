using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PatronFirearm : ExusiaiCardTemplate
{
    private const string ThresholdKey = "Threshold";
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(ThresholdKey, 4m)];

    public PatronFirearm() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PatronFirearmPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[ThresholdKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars[ThresholdKey].UpgradeValueBy(-1m);
}
