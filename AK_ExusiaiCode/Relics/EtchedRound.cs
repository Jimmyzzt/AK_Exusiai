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
        [new PowerVar<AmmoDamagePower>(1m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<AmmoDamagePower>()];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<AmmoDamagePower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ((PowerVar<AmmoDamagePower>)DynamicVars[nameof(AmmoDamagePower)]).BaseValue,
            Owner.Creature,
            null);
    }
}
