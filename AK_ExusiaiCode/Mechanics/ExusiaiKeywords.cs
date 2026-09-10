using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib;
using STS2RitsuLib.Keywords;
using MegaCrit.Sts2.Core.Localization;

namespace AK_Exusiai.Mechanics;

public static class ExusiaiKeywords
{
    public static string DeliveryId { get; private set; } = string.Empty;
    public static string TransitId { get; private set; } = string.Empty;
    public static string AngelId { get; private set; } = string.Empty;
    public static string OverloadId { get; private set; } = string.Empty;
    public static CardKeyword DeliveryKeyword { get; private set; }
    public static CardKeyword TransitKeyword { get; private set; }
    public static CardKeyword AngelKeyword { get; private set; }
    public static CardKeyword OverloadKeyword { get; private set; }

    public static void Register()
    {
        var registry = RitsuLibFramework.GetKeywordRegistry(Entry.ModId);
        var delivery = registry.RegisterCardKeywordOwnedByLocNamespace("delivery");
        var transit = registry.RegisterCardKeywordOwnedByLocNamespace("transit");
        var angel = registry.RegisterCardKeywordOwnedByLocNamespace("angel");
        var overload = registry.RegisterCardKeywordOwnedByLocNamespace("overload");
        DeliveryId = delivery.Id;
        TransitId = transit.Id;
        AngelId = angel.Id;
        OverloadId = overload.Id;
        DeliveryKeyword = delivery.CardKeywordValue;
        TransitKeyword = transit.CardKeywordValue;
        AngelKeyword = angel.CardKeywordValue;
        OverloadKeyword = overload.CardKeywordValue;
    }

    public static IHoverTip DeliveryHoverTip => ModKeywordRegistry.CreateHoverTip(DeliveryId);
    public static IHoverTip TransitHoverTip => ModKeywordRegistry.CreateHoverTip(TransitId);
    public static IHoverTip AngelHoverTip => ModKeywordRegistry.CreateHoverTip(AngelId);
    public static IHoverTip OverloadHoverTip => ModKeywordRegistry.CreateHoverTip(OverloadId);
    public static IHoverTip DeliveryTransitInteractionHoverTip => new HoverTip(
        new LocString("static_hover_tips", "AK_EXUSIAI_DELIVERY_TRANSIT_INTERACTION.title"),
        new LocString("static_hover_tips", "AK_EXUSIAI_DELIVERY_TRANSIT_INTERACTION.description"));
}
