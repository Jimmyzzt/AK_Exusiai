using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Relics;

[RegisterRelic(typeof(ExusiaiRelicPool), StableEntryStem = "CONFESS_47")]
public sealed class Confess47 : ExusiaiRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [ExusiaiKeywords.AngelHoverTip];

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        int? turnNumber = Owner.PlayerCombatState?.TurnNumber;
        if (!participants.Contains(Owner.Creature) || turnNumber is null or > 1)
            return;

        Flash();
        List<MegaCrit.Sts2.Core.Models.CardModel> cards = Enumerable
            .Range(0, DynamicVars.Cards.IntValue)
            .Select(_ => AngelCardCatalog.CreateRandom(Owner))
            .ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner);
    }
}
