using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class LogisticsOutsourcePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Retain;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        for (int i = 0; i < Amount; i++)
        {
            await CardPileCmd.Draw(choiceContext, 1, player);
            CardModel? selected = (await CardSelectCmd.FromHand(
                choiceContext,
                player,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, 1),
                null,
                this)).FirstOrDefault();
            if (selected != null)
                DeliveryCmd.Add(selected, 3);
        }
    }
}
