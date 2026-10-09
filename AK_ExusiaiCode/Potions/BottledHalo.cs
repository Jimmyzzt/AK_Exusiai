using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Potions;

[RegisterPotion(typeof(ExusiaiPotionPool))]
public sealed class BottledHalo : ExusiaiPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [ExusiaiKeywords.AngelHoverTip];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        Player player = target.Player ?? throw new InvalidOperationException("Angel potion target must be a player.");
        List<CardModel> cards = Enumerable
            .Range(0, DynamicVars.Cards.IntValue)
            .Select(_ => AngelCardCatalog.CreateRandom(player))
            .ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
    }
}
