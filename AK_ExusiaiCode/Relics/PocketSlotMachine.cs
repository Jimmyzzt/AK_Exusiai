using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class PocketSlotMachine : ExusiaiRelicTemplate
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Interference", 1m)];
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        if (Owner.Creature.CombatState is not { } combatState)
            return;

        Flash();
        await InterferenceCmd.Apply(
            new ThrowingPlayerChoiceContext(),
            combatState.GetOpponentsOf(Owner.Creature),
            DynamicVars["Interference"].IntValue,
            Owner.Creature,
            null);
    }
}
