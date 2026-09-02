using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool))]
public sealed class FirepowerFm : ExusiaiRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Multiplier", 2m),
        new DynamicVar("Ammo", 1m),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id),
        ExusiaiKeywords.OverloadHoverTip,
    ];

    public int DamageMultiplier => DynamicVars["Multiplier"].IntValue;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) ||
            SecondaryResourceCmd.Get(Owner, AmmoResource.Id) <= 0)
        {
            return;
        }

        if (await SecondaryResourceCmd.Spend(
                Owner,
                AmmoResource.Id,
                DynamicVars["Ammo"].IntValue,
                null,
                this))
        {
            Flash();
        }
    }
}
