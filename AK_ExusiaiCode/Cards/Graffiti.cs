using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Graffiti : ExusiaiCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public Graffiti() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1), null, this)).FirstOrDefault();
        if (selected is null)
            return;
        List<CardModel> copies = [];
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            CardModel copy = selected.CreateCloneForPlayer(Owner);
            await AngelCmd.Add(choiceContext, copy);
            copies.Add(copy);
        }
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(copies, PileType.Hand, Owner));
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
