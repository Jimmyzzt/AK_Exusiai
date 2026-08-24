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
    protected override bool ShowAmmoHoverTip => true;
    protected override bool ShowDeliveryHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> CardHoverTips =>
        [MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<FirepowerPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AmmoKey, 2m),
        new PowerVar<FirepowerPower>(2m),
    ];

    public PaganiniCustom() : base(4, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        DeliveryCmd.Set(this, 4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FirepowerPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(FirepowerPower)].BaseValue,
            Owner.Creature,
            this);
        await PowerCmd.Apply<PaganiniCustomPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[AmmoKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars[AmmoKey].UpgradeValueBy(1m);
}
