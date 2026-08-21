using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.AK_ExusiaiCode.Powers;

[RegisterPower]
public sealed class AmmoPower : ModPowerTemplate
{
    private const int DamageBonus = 2;

    private readonly HashSet<CardPlay> _chargedCardPlays = [];
    private int _reservedAmmo;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Attack || Amount - _reservedAmmo <= 0)
        {
            return Task.CompletedTask;
        }

        _reservedAmmo++;
        _chargedCardPlays.Add(cardPlay);
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal damage,
        ValueProp damageProps,
        Creature? source,
        CardModel? card,
        CardPlay? cardPlay)
    {
        return cardPlay != null && _chargedCardPlays.Contains(cardPlay) ? DamageBonus : 0;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!_chargedCardPlays.Remove(cardPlay))
        {
            return;
        }

        _reservedAmmo = Math.Max(0, _reservedAmmo - 1);
        await PowerCmd.ModifyAmount(context, this, -1, Owner, cardPlay.Card, false);
    }
}
