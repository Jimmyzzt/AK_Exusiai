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
    public override PowerAssetProfile AssetProfile => ExusiaiPowerAssets.Bomb;

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner ||
            command.CardPlay == null ||
            !Exusiai.HasAmmoBackedBonus(command.CardPlay))
            return;

        int bonus = Exusiai.GetAmmoBonus(command.CardPlay);
        int hitCount = command.Results.Count();
        if (bonus <= 0 || hitCount <= 0 || Owner.CombatState is not { } combatState)
            return;

        Flash();
        for (int i = 0; i < hitCount; i++)
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
