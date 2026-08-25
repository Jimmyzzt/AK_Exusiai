using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class EtchedRound : ExusiaiRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FirepowerPower>(1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<FirepowerPower>(DynamicVars[nameof(FirepowerPower)].IntValue)];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<FirepowerPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ((PowerVar<FirepowerPower>)DynamicVars[nameof(FirepowerPower)]).BaseValue,
            Owner.Creature,
            null);
    }
}
