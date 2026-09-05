using AK_Exusiai.Mechanics;
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
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(EmpathyFormPower), ".png");

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player?.PlayerCombatState is not { } state)
            return;

        var context = new ThrowingPlayerChoiceContext();
        foreach (CardModel card in state.AllCards.Where(IsAngelEligiblePile).ToList())
            await AngelCmd.Add(context, card);
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner.Creature == Owner &&
            IsAngelEligiblePile(card))
        {
            await AngelCmd.Add(new ThrowingPlayerChoiceContext(), card);
        }
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card.Owner.Creature == Owner &&
            IsAngelEligiblePile(card))
        {
            await AngelCmd.Add(new ThrowingPlayerChoiceContext(), card);
        }
    }

    private static bool IsAngelEligiblePile(CardModel card) =>
        card.Pile?.Type is PileType.Draw or
            PileType.Hand or
            PileType.Discard or
            PileType.Exhaust;
}
