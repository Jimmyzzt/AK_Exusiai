using AK_Exusiai.Characters;
using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class ForgetMeNot : ExusiaiCardTemplate, IAmmoSpendAllAttack
{
    protected override bool ShowAmmoHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new DynamicVar("AmmoGainMultiplier", 3m),
    ];
    public ForgetMeNot() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        int ammo = AK_Exusiai.Characters.Exusiai.GetAmmoMultiplier(cardPlay) * DynamicVars["AmmoGainMultiplier"].IntValue;
        if (ammo > 0)
            await SecondaryResourceCmd.Gain(Owner, AmmoResource.Id, ammo, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
