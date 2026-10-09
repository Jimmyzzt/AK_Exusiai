using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Unboxing : ExusiaiCardTemplate
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override bool ShowTransitHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Transit", 1m)];

    public Unboxing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var player in CombatState?.Players ?? [])
            await RelicLogisticsCmd.AddRandomTransit(player, DynamicVars["Transit"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Transit"].UpgradeValueBy(1m);
}
