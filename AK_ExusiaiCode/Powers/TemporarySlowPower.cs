using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Powers;

[RegisterPower]
public sealed class TemporarySlowPower : ModPowerTemplate
{
    private const string SlowAmountKey = "SlowAmount";
    private bool _skipNextAmountHook;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Slow;
    public override int DisplayAmount => DynamicVars[SlowAmountKey].IntValue;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(SlowAmountKey, 0m)];

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        target == Owner && props.IsPoweredAttack()
            ? 1m + DynamicVars[SlowAmountKey].BaseValue / 100m
            : 1m;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        DynamicVars[SlowAmountKey].BaseValue = Amount;
        _skipNextAmountHook = true;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this)
            return Task.CompletedTask;

        if (_skipNextAmountHook)
        {
            _skipNextAmountHook = false;
            return Task.CompletedTask;
        }

        DynamicVars[SlowAmountKey].BaseValue += amount;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is not AK_Exusiai.Cards.BoostedRound)
        {
            DynamicVars[SlowAmountKey].BaseValue += 10m;
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player)
            await PowerCmd.Remove(this);
    }
}
