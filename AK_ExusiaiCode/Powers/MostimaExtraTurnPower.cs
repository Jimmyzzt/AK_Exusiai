using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class MostimaExtraTurnPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile =>
        ExusiaiPowerAssets.Custom(nameof(MostimaExtraTurnPower), ".png");

    public override bool ShouldTakeExtraTurn(Player player) =>
        Amount > 0 && player == Owner.Player;

    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player)
            await PowerCmd.Decrement(this);
    }
}
