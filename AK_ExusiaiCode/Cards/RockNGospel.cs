using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class RockNGospel : ExusiaiCardTemplate
{
    protected override bool ShowAngelHoverTip => true;
    protected override IEnumerable<IHoverTip> CardHoverTips => [HoverTipFactory.FromPower<ConfusedPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<RockNGospelPower>(1m)];
    public RockNGospel() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RockNGospelPower>(choiceContext, Owner.Creature,
            DynamicVars[nameof(RockNGospelPower)].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<ConfusedPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars[nameof(RockNGospelPower)].UpgradeValueBy(1m);
}
