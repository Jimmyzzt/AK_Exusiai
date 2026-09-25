using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Enchantments;

[RegisterEnchantment]
public sealed class Ascension : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new($"{Entry.ResPath}/images/enchantments/Ascension.svg");

    protected override void OnEnchant()
    {
        Card.Capabilities().GetOrCreate<AngelCapability>().Refresh();
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == Card)
            card.Capabilities().GetOrCreate<AngelCapability>().Refresh();
        return Task.CompletedTask;
    }
}
