using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class CovenantOfBullets : ExusiaiCardTemplate
{
    protected override bool ShowAmmoHoverTip => true;
    protected override bool ShowAngelHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("AmmoBonus", 0m)];

    public CovenantOfBullets() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile discardPile = PileType.Discard.GetPile(Owner);
        if (discardPile.Cards.Count > 0)
        {
            IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                discardPile,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1))).ToList();
            foreach (CardModel card in selected)
            {
                await CardPileCmd.Add(card, PileType.Hand);
                await AngelCmd.Add(choiceContext, card);
            }
        }

        await PowerCmd.Apply<CovenantOfBulletsPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        if (DynamicVars["AmmoBonus"].IntValue > 0)
        {
            await PowerCmd.Apply<CovenantOfBulletsBonusPower>(
                choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        }

        if (cardPlay.IsLastInSeries)
            PlayerCmd.EndTurn(Owner, false);
    }

    protected override void OnUpgrade() => DynamicVars["AmmoBonus"].UpgradeValueBy(2m);
}
