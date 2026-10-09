using AK_Exusiai.Characters;
using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
[RegisterCharacterStarterRelic(typeof(Exusiai), Order = 0)]
[RegisterTouchOfOrobasRefinement(typeof(ExusiaiSurprise))]
public sealed class ExusiaiBadge : ExusiaiRelicTemplate
{
    private const string AmmoKey = "Ammo";

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AmmoKey, 5m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id),
        ExusiaiKeywords.OverloadHoverTip,
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, DynamicVars[AmmoKey].IntValue, this);
    }
}
