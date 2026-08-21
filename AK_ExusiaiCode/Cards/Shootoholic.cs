using AK_Exusiai.Characters;
using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.SecondaryResources;
using STS2RitsuLib.Interop.AutoRegistration;

namespace AK_Exusiai.Cards;

[RegisterCard(typeof(ExusiaiCardPool))]
public sealed class Shootoholic : ExusiaiCardTemplate
{
    protected override bool ShowAmmoHoverTip => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Shootoholic() : base(5, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int ammo = Math.Min(5, SecondaryResourceCmd.Get(Owner, AmmoResource.Id));
        if (ammo > 0)
            await SecondaryResourceCmd.Spend(Owner, AmmoResource.Id, ammo, this, this);

        List<CardModel> attacks = CardPile.GetCards(Owner, PileType.Draw, PileType.Discard)
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        List<CardModel> selected = [];
        while (attacks.Count > 0 && selected.Count < 5)
        {
            CardModel? card = Owner.RunState.Rng.CombatCardSelection.NextItem(attacks);
            if (card == null)
                break;
            attacks.Remove(card);
            selected.Add(card);
        }

        using IDisposable scope = Exusiai.BeginPrepaidAmmo(Owner, ammo);
        foreach (CardModel attack in selected)
            await CardCmd.AutoPlay(choiceContext, attack, null, AutoPlayType.Default);
    }
}
