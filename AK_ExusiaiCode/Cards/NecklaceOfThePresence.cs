using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class NecklaceOfThePresence : ExusiaiCardTemplate
{
    private const string BlockKey = "Block";
    protected override bool ShowAmmoHoverTip => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(BlockKey, 4m)];

    public NecklaceOfThePresence() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NecklaceOfThePresencePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[BlockKey].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars[BlockKey].UpgradeValueBy(1m);
}
