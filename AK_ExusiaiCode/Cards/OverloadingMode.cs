using AK_Exusiai.Content;
using AK_Exusiai.Characters;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class OverloadingMode : ExusiaiCardTemplate
{
    protected override bool PlaysOwnNonAttackAnimation => true;
    protected override bool ShowAmmoHoverTip => true;
    protected override bool ShowOverloadHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    public OverloadingMode() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await AK_Exusiai.Characters.Exusiai.EnterOverload(
            choiceContext,
            Owner,
            this,
            retainAmmoAtTurnEnd: true);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}
