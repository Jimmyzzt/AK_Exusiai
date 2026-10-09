using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class FirepowerPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse the existing ammo-damage artwork under its stable resource name.
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom("AmmoDamagePower");

    public static IHoverTip CreateGenericHoverTip()
    {
        PowerModel firepower = ModelDb.Power<FirepowerPower>();
        string description = new LocString(
            "static_hover_tips",
            "AK_EXUSIAI_FIREPOWER_GENERIC.description").GetFormattedText();
        return new HoverTip(firepower, description, isSmart: false);
    }
}
