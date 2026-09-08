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
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public CovenantOfBullets() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = Math.Min(
            DynamicVars.Cards.IntValue,
            PileType.Hand.GetPile(Owner).Cards.Count);
        if (count > 0)
        {
            IReadOnlyList<CardModel> selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Hand.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, count))).ToList();
            foreach (CardModel card in selected)
                await AngelCmd.Add(choiceContext, card);
        }

        await PowerCmd.Apply<CovenantOfBulletsPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        if (cardPlay.IsLastInSeries)
            PlayerCmd.EndTurn(Owner, false);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
