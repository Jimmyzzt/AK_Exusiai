using AK_Exusiai.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class CoveringFirePower : TemporaryStrengthPower, IModPowerAssetOverrides
{
    public override AbstractModel OriginModel => ModelDb.Card<CoveringFire>();
    public PowerAssetProfile AssetProfile =>
        ExusiaiPowerAssets.Custom(nameof(CoveringFirePower), ".png");
    public string? CustomIconPath => AssetProfile.IconPath;
    public string? CustomBigIconPath => AssetProfile.BigIconPath;
    protected override bool IsPositive => false;
}
