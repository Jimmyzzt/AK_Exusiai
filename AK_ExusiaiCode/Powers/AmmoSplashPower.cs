using AK_Exusiai.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class AmmoSplashPower : ModPowerTemplate
{
    private readonly Dictionary<CardModel, decimal> _damageByCard = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(AmmoSplashPower));

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Player.Creature == Owner && cardPlay.Card.Type == CardType.Attack)
            _damageByCard[cardPlay.Card] = 0m;
        return Task.CompletedTask;
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer == Owner &&
            cardSource != null &&
            _damageByCard.TryGetValue(cardSource, out decimal total))
        {
            _damageByCard[cardSource] = total + result.TotalDamage + result.OverkillDamage;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_damageByCard.Remove(cardPlay.Card, out decimal totalDamage) ||
            cardPlay.Player.Creature != Owner ||
            !Exusiai.DidSpendAmmo(cardPlay))
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);

        decimal splashDamage = totalDamage * 0.5m;
        if (splashDamage <= 0m || Owner.CombatState is not { } combatState)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            combatState.GetOpponentsOf(Owner).Where(creature => creature.IsAlive),
            splashDamage,
            ValueProp.Move | ValueProp.Unpowered,
            Owner);
    }
}
