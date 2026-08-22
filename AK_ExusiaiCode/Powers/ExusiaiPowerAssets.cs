using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

internal static class ExusiaiPowerAssets
{
    public static PowerAssetProfile Bomb => From<TheBombPower>();
    public static PowerAssetProfile Ammo => new(
        IconPath: $"{Entry.ResPath}/images/powers/AmmoNextTurnPower.svg",
        BigIconPath: $"{Entry.ResPath}/images/powers/AmmoNextTurnPower.svg");
    public static PowerAssetProfile Modification => From<StrengthPower>();
    public static PowerAssetProfile Confusion => From<ConfusedPower>();
    public static PowerAssetProfile Weak => From<WeakPower>();
    public static PowerAssetProfile Energy => From<EnergyNextTurnPower>();
    public static PowerAssetProfile Echo => From<EchoFormPower>();
    public static PowerAssetProfile Retain => From<BarricadePower>();
    public static PowerAssetProfile Angel => Custom(nameof(AngelFreePower));
    public static PowerAssetProfile Delivery => Custom(nameof(LogisticsOutsourcingPower), ".png");
    public static PowerAssetProfile ExtraTurn => From<AmbergrisPower>();
    public static PowerAssetProfile Mercy => From<DarkEmbracePower>();
    public static PowerAssetProfile Slow => From<SlowPower>();
    public static PowerAssetProfile Debilitate => From<DebilitatePower>();
    public static PowerAssetProfile PiercingWail => From<PiercingWailPower>();

    public static PowerAssetProfile Custom(string baseName, string extension = ".svg") => new(
        IconPath: $"{Entry.ResPath}/images/powers/{baseName}{extension}",
        BigIconPath: $"{Entry.ResPath}/images/powers/{baseName}{extension}");

    private static PowerAssetProfile From<TPower>() where TPower : PowerModel
    {
        TPower power = ModelDb.Power<TPower>();
        return new PowerAssetProfile(
            IconPath: power.IconPath,
            BigIconPath: power.ResolvedBigIconPath);
    }
}
