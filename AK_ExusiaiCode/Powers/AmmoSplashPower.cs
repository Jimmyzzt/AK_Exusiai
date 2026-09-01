using AK_Exusiai.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Custom(nameof(AmmoSplashPower));

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner ||
            command.CardPlay == null ||
            !Exusiai.HasAmmoBackedBonus(command.CardPlay))
            return;

        IReadOnlyList<decimal> bonuses = Exusiai.GetAmmoHitBonuses(command.CardPlay);
        if (bonuses.Count <= 0 || Owner.CombatState is not { } combatState)
            return;

        Flash();
        foreach (decimal bonus in bonuses)
        {
            await CreatureCmd.Damage(
                choiceContext,
                combatState.GetOpponentsOf(Owner).Where(creature => creature.IsAlive),
                bonus,
                ValueProp.Move | ValueProp.Unpowered,
                Owner);
        }
    }
}
