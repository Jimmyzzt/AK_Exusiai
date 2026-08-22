using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class HolyCityEternal : ExusiaiCardTemplate
{
    private const string TurnsKey = "Turns";
    protected override bool ShowAngelHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(TurnsKey, 1m),
        new EnergyVar(1),
    ];
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust];

    public HolyCityEternal() : base(1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal turns = DynamicVars[TurnsKey].BaseValue;
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.RetainHandPower>(
            choiceContext, Owner.Creature, turns, Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[TurnsKey].UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
