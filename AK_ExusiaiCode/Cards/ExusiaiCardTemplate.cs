using AK_Exusiai.Mechanics;
using Godot;
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
    public override CardAssetProfile AssetProfile
    {
        get
        {
            string portraitPath = $"{Entry.ResPath}/images/cards/{GetType().Name}.png";
            return ResourceLoader.Exists(portraitPath)
                ? new CardAssetProfile(PortraitPath: portraitPath)
                : CardAssetProfile.Empty;
        }
    }

    protected virtual bool ShowAmmoHoverTip => false;
    protected virtual bool ShowDeliveryHoverTip => false;
    protected virtual bool ShowTransitHoverTip => false;
    protected virtual bool ShowAngelHoverTip => false;
    protected virtual bool ShowOverloadHoverTip => false;
    protected virtual IEnumerable<IHoverTip> CardHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ShowAmmoHoverTip)
                yield return ModSecondaryResourceRegistry.CreateHoverTip(AmmoResource.Id);
            if (ShowDeliveryHoverTip)
                yield return ExusiaiKeywords.DeliveryHoverTip;
            if (ShowTransitHoverTip)
                yield return ExusiaiKeywords.TransitHoverTip;
            if (ShowAngelHoverTip)
                yield return ExusiaiKeywords.AngelHoverTip;
            if (ShowAmmoHoverTip || ShowOverloadHoverTip)
                yield return ExusiaiKeywords.OverloadHoverTip;

            foreach (IHoverTip hoverTip in CardHoverTips)
                yield return hoverTip;
        }
    }
}
