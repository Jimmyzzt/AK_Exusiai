using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class HolyCityMercy : ExusiaiCardTemplate
{
    protected override bool ShowAngelHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(11m, ValueProp.Move)];
    public HolyCityMercy() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        foreach (var card in Owner.PlayerCombatState!.Hand.Cards.ToList())
        {
            if (!AngelCmd.IsAngel(card))
                await AngelCmd.Add(choiceContext, card);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
