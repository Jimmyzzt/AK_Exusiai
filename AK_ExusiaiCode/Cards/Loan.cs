using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Loan : ExusiaiCardTemplate
{
    protected override IEnumerable<IHoverTip> CardHoverTips => [HoverTipFactory.FromCard<Debt>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(45)];

    public Loan() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
        Debt curse = CombatState!.CreateCard<Debt>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
            curse, PileType.Discard, Owner));
    }

    protected override void OnUpgrade() => DynamicVars.Gold.UpgradeValueBy(15m);
}
