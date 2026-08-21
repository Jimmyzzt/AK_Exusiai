using AK_Exusiai.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
public sealed class DelayedBlastPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Bomb;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
    ];

    public void SetDamage(decimal damage)
    {
        AssertMutable();
        DynamicVars.Damage.BaseValue = damage;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner) || AmountOnTurnStart <= 0)
            return;

        if (Owner.Player == null)
        {
            await PowerCmd.Remove(this);
            return;
        }

        Flash();
        CardModel sourceCard = combatState.CreateCard<DelayedBlast>(Owner.Player);
        try
        {
            // A fresh attack-card source survives combat save/load without persisting a raw card reference.
            // The saved amount already contains the attacker's play-time modifiers. Unpowered prevents Ammo,
            // temporary modification, Strength, Weak, and other next-turn modifiers from changing it again.
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .Unpowered()
                .FromCard(sourceCard, null)
                .TargetingAllOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(new ThrowingPlayerChoiceContext());
        }
        finally
        {
            combatState.RemoveCard(sourceCard);
            await PowerCmd.Remove(this);
        }
    }
}
