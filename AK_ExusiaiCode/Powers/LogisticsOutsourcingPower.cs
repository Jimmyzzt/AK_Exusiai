using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class LogisticsOutsourcingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(LogisticsOutsourcingPower), ".png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        await CardPileCmd.Draw(choiceContext, Amount, player);
        int bonus = Owner.GetPower<LogisticsOutsourcingSelectionPower>()?.Amount ?? 0;
        IReadOnlyList<MegaCrit.Sts2.Core.Models.RelicModel> selected =
            await RelicLogisticsCmd.ChooseDeliveredRelics(player, Amount + bonus);
        foreach (MegaCrit.Sts2.Core.Models.RelicModel relic in selected)
            relic.Capability<RelicLogisticsCapability>()?.ReduceDelivery(1);
    }
}

[RegisterPower]
public sealed class LogisticsOutsourcingSelectionPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile =>
        ExusiaiPowerAssets.Custom(nameof(LogisticsOutsourcingPower), ".png");
}
