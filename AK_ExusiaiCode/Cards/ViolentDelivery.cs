using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class ViolentDelivery : ExusiaiCardTemplate
{
    protected override bool ShowDeliveryHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(25m, ValueProp.Move),
    ];

    public ViolentDelivery() : base(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (IsUpgraded)
        {
            foreach (CardModel card in Owner.PlayerCombatState!.Hand.Cards
                         .Where(DeliveryCmd.HasDelivery)
                         .ToList())
            {
                await DeliveryCmd.Reduce(choiceContext, card, 1);
            }
        }
        else
        {
            CardModel? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, 1),
                DeliveryCmd.HasDelivery,
                this)).FirstOrDefault();
            if (selected != null)
                await DeliveryCmd.Reduce(choiceContext, selected, 1);
        }
    }
}
