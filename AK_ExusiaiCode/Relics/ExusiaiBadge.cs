using AK_Exusiai.Characters;
using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
[RegisterCharacterStarterRelic(typeof(Exusiai), Order = 0)]
public sealed class ExusiaiBadge : ModRelicTemplate
{
    private const string AmmoKey = "Ammo";

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/relics/ExusiaiBadge.png",
        IconOutlinePath: $"{Entry.ResPath}/images/relics/ExusiaiBadge.png",
        BigIconPath: $"{Entry.ResPath}/images/relics/ExusiaiBadge.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AmmoKey, 4m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id),
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, DynamicVars[AmmoKey].IntValue, this);
    }
}
