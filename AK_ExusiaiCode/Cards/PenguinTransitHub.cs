using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PenguinTransitHub : ExusiaiCardTemplate
{
    protected override bool ShowTransitHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(8)];

    public PenguinTransitHub() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int cost = DynamicVars.Gold.IntValue;
        if (Owner.Gold < cost)
            return;

        await PlayerCmd.LoseGold(cost, Owner, GoldLossType.Spent);
        await RelicLogisticsCmd.AddRandomTransit(Owner, 1);
    }

    protected override void OnUpgrade() => DynamicVars.Gold.UpgradeValueBy(-4m);
}
