using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class ParadiseLost : ExusiaiCardTemplate
{
    protected override IEnumerable<IHoverTip> CardHoverTips =>
        [EnergyHoverTip, HoverTipFactory.FromCard<Decay>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2),
        new CardsVar(2),
    ];

    public ParadiseLost() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        foreach (CardModel card in Owner.PlayerCombatState!.Hand.Cards)
        {
            if (!card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) >= 0)
            {
                card.EnergyCost.SetThisTurnOrUntilPlayed(
                    Owner.RunState.Rng.CombatEnergyCosts.NextInt(4));
            }
        }
        await PowerCmd.Apply<TemporaryConfusionPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        Decay curse = CombatState!.CreateCard<Decay>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(
            curse, PileType.Discard, Owner));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Energy"].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
