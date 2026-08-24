using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class ExusiaiSurprise : ExusiaiRelicTemplate
{
    private const string AmmoKey = "Ammo";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AmmoKey, 8m),
        new PowerVar<FirepowerPower>(1m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id),
        HoverTipFactory.FromPower<FirepowerPower>(),
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<FirepowerPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            ((PowerVar<FirepowerPower>)DynamicVars[nameof(FirepowerPower)]).BaseValue,
            Owner.Creature,
            null);
        await SecondaryResourceCmd.Gain(
            Owner,
            AmmoResource.Id,
            DynamicVars[AmmoKey].IntValue,
            this);
    }
}
