using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Flashbang : ExusiaiCardTemplate
{
    protected override IEnumerable<IHoverTip> CardHoverTips => [HoverTipFactory.FromPower<WeakPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(1m),
        new PowerVar<FlashbangPower>(2m),
    ];
    public Flashbang() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState!.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<FlashbangPower>(choiceContext, enemy,
                DynamicVars[nameof(FlashbangPower)].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars[nameof(FlashbangPower)].UpgradeValueBy(1m);
}
