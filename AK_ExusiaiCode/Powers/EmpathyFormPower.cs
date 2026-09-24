using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class EmpathyFormPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(EmpathyFormPower), ".png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player.PlayerCombatState is not { })
            return;

        CardPile hand = PileType.Hand.GetPile(player);
        int count = Math.Min(Amount, hand.Cards.Count);
        if (count <= 0)
            return;

        IEnumerable<CardModel> selected = await CardSelectCmd.FromCombatPile(
            choiceContext,
            hand,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, count));
        foreach (CardModel card in selected)
            await AngelCmd.Add(choiceContext, card);
    }
}
