using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class CompassionPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(CompassionPower));

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player.PlayerCombatState is not { } state || state.Hand.Cards.Count == 0)
            return;

        int count = Math.Min(Amount, state.Hand.Cards.Count);
        List<MegaCrit.Sts2.Core.Models.CardModel> candidates = state.Hand.Cards.ToList();
        Flash();
        for (int i = 0; i < count; i++)
        {
            var card = player.RunState.Rng.CombatCardGeneration.NextItem(candidates);
            if (card == null)
                break;
            candidates.Remove(card);
            await AngelCmd.Add(choiceContext, card);
        }
    }
}
