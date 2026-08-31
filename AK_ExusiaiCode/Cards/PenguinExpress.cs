using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class PenguinExpress : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Delivery", 4m)];
    public PenguinExpress() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!await RelicLogisticsCmd.ChooseAndAddDelivery(
                choiceContext,
                Owner,
                DynamicVars["Delivery"].IntValue))
            return;

        int drawCount = Math.Max(0, CardPile.MaxCardsInHand - Owner.PlayerCombatState!.Hand.Cards.Count);
        if (drawCount > 0)
            await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
