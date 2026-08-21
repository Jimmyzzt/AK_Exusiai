using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

internal static class ExusiaiPowerAssets
{
    public static PowerAssetProfile Bomb => From<TheBombPower>();
    public static PowerAssetProfile Ammo => From<DrawCardsNextTurnPower>();
    public static PowerAssetProfile Modification => From<StrengthPower>();

    private static PowerAssetProfile From<TPower>() where TPower : PowerModel
    {
        TPower power = ModelDb.Power<TPower>();
        return new PowerAssetProfile(
            IconPath: power.IconPath,
            BigIconPath: power.ResolvedBigIconPath);
    }
}
