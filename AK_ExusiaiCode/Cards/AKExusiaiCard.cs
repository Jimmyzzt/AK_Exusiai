using AK_Exusiai.AK_ExusiaiCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.AK_ExusiaiCode.Cards;

public abstract class AKExusiaiCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ModCardTemplate(cost, type, rarity, target, false)
{
    public override string CustomPortraitPath => ResourcePaths.CardPortrait("placeholder.png");
}
