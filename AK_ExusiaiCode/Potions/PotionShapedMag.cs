using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Potions;

[RegisterPotion(typeof(ExusiaiPotionPool))]
public sealed class PotionShapedMag : ExusiaiPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Ammo", 6m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id),
        ExusiaiKeywords.OverloadHoverTip,
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        Player player = target.Player ?? throw new InvalidOperationException("Ammo potion target must be a player.");
        await SecondaryResourceCmd.Gain(
            player,
            AmmoResource.Id,
            DynamicVars["Ammo"].IntValue,
            this);
    }
}
