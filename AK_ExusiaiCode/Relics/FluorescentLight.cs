using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class FluorescentLight : ExusiaiRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<TemporaryFirepowerPower>(1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ExusiaiKeywords.AngelHoverTip,
        HoverTipFactory.FromPower<TemporaryFirepowerPower>(),
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner || !AngelCmd.IsAngel(cardPlay.Card))
            return;

        Flash();
        await PowerCmd.Apply<TemporaryFirepowerPower>(
            choiceContext,
            Owner.Creature,
            ((PowerVar<TemporaryFirepowerPower>)DynamicVars[nameof(TemporaryFirepowerPower)]).BaseValue,
            Owner.Creature,
            null);
    }
}
