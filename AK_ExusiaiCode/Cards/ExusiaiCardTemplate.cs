using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Cards;

public abstract class ExusiaiCardTemplate(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary)
{
    protected virtual bool ShowAmmoHoverTip => Type == CardType.Attack;
    protected virtual bool ShowDeliveryHoverTip => false;
    protected virtual bool ShowAngelHoverTip => false;
    protected virtual IEnumerable<IHoverTip> CardHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ShowAmmoHoverTip)
                yield return ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id);
            if (ShowDeliveryHoverTip)
                yield return ExusiaiKeywords.DeliveryHoverTip;
            if (ShowAngelHoverTip)
                yield return ExusiaiKeywords.AngelHoverTip;

            foreach (IHoverTip hoverTip in CardHoverTips)
                yield return hoverTip;
        }
    }
}
