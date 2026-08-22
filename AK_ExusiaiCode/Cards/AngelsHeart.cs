using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class AngelsHeart : ExusiaiCardTemplate
{
    private const string TurnsKey = "Turns";
    protected override bool ShowAngelHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(TurnsKey, 1m)];

    public AngelsHeart() : base(6, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (CardModel canonical in AngelCardCatalog.CanonicalCards)
        {
            CardModel handCopy = AngelCardCatalog.Create(Owner, canonical);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                handCopy, PileType.Hand, Owner));
            CardModel drawCopy = AngelCardCatalog.Create(Owner, canonical);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
                drawCopy, PileType.Draw, Owner));
        }

        await PowerCmd.Apply<AngelFreePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[TurnsKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars[TurnsKey].UpgradeValueBy(1m);
}
