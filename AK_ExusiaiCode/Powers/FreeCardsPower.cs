using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class FreeCardsPower : ModPowerTemplate
{
    private CardModel? _ignoredSourceCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile =>
        ExusiaiPowerAssets.Custom(nameof(FreeCardsPower), ".png");

    internal void IgnoreSourceCard(CardModel card)
    {
        _ignoredSourceCard = card;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (_ignoredSourceCard != null && cardPlay.Card != _ignoredSourceCard)
            _ignoredSourceCard = null;
        return Task.CompletedTask;
    }

    public override bool TryModifyEnergyCostInCombatLate(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = card.Owner.Creature == Owner &&
                       card.Pile?.Type is PileType.Hand or PileType.Play &&
                       !card.EnergyCost.CostsX
            ? 0m
            : originalCost;
        return modifiedCost != originalCost;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player.Creature != Owner)
            return;

        // Depending on the HookBus snapshot, a newly applied power may already
        // receive the source card's AfterCardPlayed callback. Never consume the
        // first granted use on the card that created the power.
        if (cardPlay.Card == _ignoredSourceCard)
        {
            _ignoredSourceCard = null;
            return;
        }

        if (cardPlay.Card.EnergyCost.CostsX == false)
            await PowerCmd.Decrement(this);
    }
}
