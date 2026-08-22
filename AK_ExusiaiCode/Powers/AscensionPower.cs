using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class AscensionPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(AscensionPower));

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpgradeAllAngels();
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        UpgradeIfOwned(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, MegaCrit.Sts2.Core.Entities.Players.Player? creator)
    {
        UpgradeIfOwned(card);
        return Task.CompletedTask;
    }

    private void UpgradeAllAngels()
    {
        if (Owner.Player?.PlayerCombatState is not { } state)
            return;

        foreach (CardModel card in state.AllCards.ToList())
            UpgradeIfOwned(card);
    }

    private void UpgradeIfOwned(CardModel card)
    {
        if (card.Owner.Creature == Owner && AngelCmd.IsAngel(card) && card.IsUpgradable)
            CardCmd.Upgrade(card);
    }
}
