using AK_Exusiai.Characters;
using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
[RegisterDustyTomeCard(typeof(Exusiai))]
public sealed class TheSaintsTravels : ExusiaiCardTemplate
{
    private int _currentFirepower = 1;
    private int _increasedFirepower;

    [SavedProperty]
    public int CurrentFirepower
    {
        get => _currentFirepower;
        set
        {
            AssertMutable();
            _currentFirepower = value;
            DynamicVars[nameof(FirepowerPower)].BaseValue = value;
        }
    }

    [SavedProperty]
    public int IncreasedFirepower
    {
        get => _increasedFirepower;
        set
        {
            AssertMutable();
            _increasedFirepower = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FirepowerPower>(CurrentFirepower)];

    protected override IEnumerable<IHoverTip> CardHoverTips =>
        [HoverTipFactory.FromPower<FirepowerPower>()];

    public TheSaintsTravels()
        : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FirepowerPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[nameof(FirepowerPower)].BaseValue,
            Owner.Creature,
            this);

        BuffFromPlay(1);
        (DeckVersion as TheSaintsTravels)?.BuffFromPlay(1);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override void AfterDowngraded() => UpdateFirepower();

    private void BuffFromPlay(int amount)
    {
        IncreasedFirepower += amount;
        UpdateFirepower();
    }

    private void UpdateFirepower() => CurrentFirepower = 1 + IncreasedFirepower;
}
