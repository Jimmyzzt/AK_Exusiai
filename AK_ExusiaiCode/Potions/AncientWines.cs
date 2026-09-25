using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Potions;

[RegisterPotion(typeof(AncientWinePotionPool))]
public sealed class UrsusBeluga : ExusiaiPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        await SecondaryResourceCmd.Gain(target!.Player!, AmmoResource.Id, 30, this);
        await PowerCmd.Apply<BelugaRandomTargetPower>(choiceContext, target, 1m, target, null);
    }
}

[RegisterPotion(typeof(AncientWinePotionPool))]
public sealed class GaulChardonnay : ExusiaiPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        var player = target!.Player!;
        List<MegaCrit.Sts2.Core.Models.CardModel> discarded = (await CardSelectCmd.FromHandForDiscard(
            choiceContext, player, new CardSelectorPrefs(SelectionScreenPrompt, 0, int.MaxValue), null, this)).ToList();
        await CardCmd.Discard(choiceContext, discarded);
        int missing = Math.Max(0, RitsuLibFramework.GetMaxHandSize(player) - player.PlayerCombatState!.Hand.Cards.Count);
        await CardPileCmd.Draw(choiceContext, missing, player);
        await PowerCmd.Apply<NoDrawPower>(choiceContext, target, 1m, target, null);
    }
}

[RegisterPotion(typeof(AncientWinePotionPool))]
public sealed class YanFenjiu : ExusiaiPotionTemplate
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        var player = target!.Player!;
        List<MegaCrit.Sts2.Core.Models.CardModel> selected = (await CardSelectCmd.FromHand(
            choiceContext, player, new CardSelectorPrefs(SelectionScreenPrompt, 0, int.MaxValue), null, this)).ToList();
        foreach (var card in selected)
        {
            var replacement = CardFactory.CreateRandomCardForTransform(card, isInCombat: true, player.RunState.Rng.Niche);
            await CardCmd.Transform(card, replacement);
        }
    }
}
