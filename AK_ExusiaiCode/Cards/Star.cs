using AK_Exusiai.Characters;
using AK_Exusiai.Content;
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
public sealed class Star : ExusiaiCardTemplate
{
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    public Star() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (!AK_Exusiai.Characters.Exusiai.DidSpendAmmo(cardPlay))
            return;

        List<CardModel> upgradable = Owner.PlayerCombatState!.Hand.Cards
            .Where(card => card.IsUpgradable)
            .ToList();
        if (IsUpgraded)
        {
            foreach (CardModel card in upgradable)
                CardCmd.Upgrade(card);
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            upgradable.Contains,
            this)).FirstOrDefault();
        if (selected != null)
            CardCmd.Upgrade(selected);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
