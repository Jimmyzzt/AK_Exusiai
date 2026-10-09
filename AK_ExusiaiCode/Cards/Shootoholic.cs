using AK_Exusiai.Characters;
using AK_Exusiai.Content;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
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
        decimal damagePerAmmo = AK_Exusiai.Characters.Exusiai.GetAmmoDamagePerAmmo(Owner);
        // Overload waives the resource payment, but this card still snapshots and
        // grants the full five-Ammo prepaid bonus when five Ammo are available.
        if (ammo > 0)
        {
            if (Owner.Creature.HasPower<OverloadPower>())
                await AK_Exusiai.Characters.Exusiai.NotifyOverloadAmmoSpent(Owner, ammo);
            else
                await SecondaryResourceCmd.Spend(Owner, AmmoResource.Id, ammo, this, this);
        }

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

        using IDisposable scope = AK_Exusiai.Characters.Exusiai.BeginPrepaidAmmo(Owner, ammo, damagePerAmmo);
        foreach (CardModel attack in selected)
            await CardCmd.AutoPlay(choiceContext, attack, null, AutoPlayType.Default);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
