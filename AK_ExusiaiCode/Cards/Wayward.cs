using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Wayward : ExusiaiCardTemplate
{
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Unplayable];
    protected override IEnumerable<IHoverTip> CardHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<DexterityPower>()];

    public Wayward() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None) { }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (ReferenceEquals(card, this))
            await ApplyPenalty(choiceContext);
    }

    public override void AfterTransformedFrom()
    {
        if (Owner.Creature.CombatState != null)
            TaskHelper.RunSafely(ApplyPenalty(new BlockingPlayerChoiceContext()));
    }

    private async Task ApplyPenalty(PlayerChoiceContext choiceContext)
    {
        await PowerCmd.Apply<StrengthPower>(
            choiceContext, Owner.Creature, -1m, Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(
            choiceContext, Owner.Creature, -1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() { }
}
