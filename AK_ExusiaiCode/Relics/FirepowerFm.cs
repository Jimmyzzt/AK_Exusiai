using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class FirepowerFm : ExusiaiRelicTemplate,
    ISecondaryResourceHookListener,
    IOverloadAmmoSpendListener
{
    private int _ammoSpentThisTurn;

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Ammo", 4m),
        new PowerVar<FirepowerPower>(1m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id),
        ExusiaiKeywords.OverloadHoverTip,
        HoverTipFactory.FromPower<FirepowerPower>(
            DynamicVars[nameof(FirepowerPower)].IntValue),
    ];

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
            _ammoSpentThisTurn = 0;
        return Task.CompletedTask;
    }

    public Task AfterSecondaryResourceSpent(SecondaryResourceSpendContext context) =>
        context.Player == Owner && context.Definition.Id == AmmoResource.Id
            ? CountAmmo(context.Amount)
            : Task.CompletedTask;

    public Task AfterOverloadAmmoSpent(int amount) => CountAmmo(amount);

    private async Task CountAmmo(int amount)
    {
        if (amount <= 0)
            return;

        _ammoSpentThisTurn += amount;
        int threshold = DynamicVars["Ammo"].IntValue;
        int triggers = _ammoSpentThisTurn / threshold;
        _ammoSpentThisTurn %= threshold;
        if (triggers <= 0)
            return;

        Flash();
        await PowerCmd.Apply<FirepowerPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars[nameof(FirepowerPower)].BaseValue * triggers,
            Owner.Creature,
            null);
    }
}
