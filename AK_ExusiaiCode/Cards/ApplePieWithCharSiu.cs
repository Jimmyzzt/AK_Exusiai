using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class ApplePieWithCharSiu : ExusiaiCardTemplate
{
    protected override IEnumerable<IHoverTip> CardHoverTips =>
        [EnergyHoverTip, HoverTipFactory.FromPower<NoDrawPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(3)];
    public ApplePieWithCharSiu() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        CardCostRandomizationCmd.RandomizeHand(Owner);
        await PowerCmd.Apply<NoDrawPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(2m);
}
