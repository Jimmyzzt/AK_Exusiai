using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib;
using STS2RitsuLib.Keywords;

namespace AK_Exusiai.Mechanics;

public static class ExusiaiKeywords
{
    public static string DeliveryId { get; private set; } = string.Empty;
    public static string AngelId { get; private set; } = string.Empty;
    public static CardKeyword DeliveryKeyword { get; private set; }
    public static CardKeyword AngelKeyword { get; private set; }

    public static void Register()
    {
        var registry = RitsuLibFramework.GetKeywordRegistry(Entry.ModId);
        var delivery = registry.RegisterCardKeywordOwnedByLocNamespace("delivery");
        var angel = registry.RegisterCardKeywordOwnedByLocNamespace("angel");
        DeliveryId = delivery.Id;
        AngelId = angel.Id;
        DeliveryKeyword = delivery.CardKeywordValue;
        AngelKeyword = angel.CardKeywordValue;
    }

    public static IHoverTip DeliveryHoverTip => ModKeywordRegistry.CreateHoverTip(DeliveryId);
    public static IHoverTip AngelHoverTip => ModKeywordRegistry.CreateHoverTip(AngelId);
}
