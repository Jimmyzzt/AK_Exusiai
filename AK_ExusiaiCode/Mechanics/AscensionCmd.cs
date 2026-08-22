using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace AK_Exusiai.Mechanics;

public static class AscensionCmd
{
    public static void UpgradeIfNeeded(CardModel card)
    {
        if (card.IsMutable &&
            card.IsUpgradable &&
            AngelCmd.IsAngel(card) &&
            card.Owner.Creature.HasPower<AscensionPower>())
        {
            CardCmd.Upgrade(card);
        }
    }
}
