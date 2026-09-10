using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class HolyCityEmbrace : ExusiaiCardTemplate
{
    private const string CalculatedBlocksKey = "CalculatedBlocks";

    protected override bool ShowAngelHoverTip => true;
    public override bool GainsBlock => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(CalculatedBlocksKey).WithMultiplier((card, _) =>
            card.Owner.PlayerCombatState?.AllCards.Count(AngelCmd.IsAngel) ?? 0),
    ];

    public HolyCityEmbrace() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = (int)((CalculatedVar)DynamicVars[CalculatedBlocksKey]).Calculate(Owner.Creature);
        for (int i = 0; i < count; i++)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay, fast: true);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
