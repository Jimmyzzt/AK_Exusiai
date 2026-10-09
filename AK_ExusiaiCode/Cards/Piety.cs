using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Piety : ExusiaiCardTemplate
{
    protected override bool ShowAngelHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public Piety() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Count > 0)
        {
            CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                drawPile,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
            if (selected != null)
                await AngelCmd.Add(choiceContext, selected);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
