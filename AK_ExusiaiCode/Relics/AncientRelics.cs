using AK_Exusiai.Cards;
using AK_Exusiai.Content;
using AK_Exusiai.Enchantments;
using AK_Exusiai.Mechanics;
using AK_Exusiai.Powers;
using AK_Exusiai.Potions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Enchantments;
using AK_Exusiai.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Relics;

public abstract class ExusiaiAncientRelic : ExusiaiRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class PhotoWithTheLord : ExusiaiAncientRelic
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ArtifactPower>(2m)];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, null);
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class EntryPermit : ExusiaiAncientRelic
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained()
    {
        HashSet<ModelId> starterIds = Owner.Character.StartingDeck.Select(c => c.Id).ToHashSet();
        Ascension ascension = ModelDb.Enchantment<Ascension>();
        foreach (CardModel card in Owner.Deck.Cards.ToList())
        {
            if (starterIds.Contains(card.Id) && ascension.CanEnchant(card))
                CardCmd.Enchant<Ascension>(card, 1m);
        }
        Flash();
        return Task.CompletedTask;
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class StudyTourCertificate : ExusiaiAncientRelic
{
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
            return false;
        SmithRestSiteOption? smith = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smith is null)
            return false;
        smith.SmithCount++;
        return true;
    }

    public override Task AfterRestSiteSmith(Player player)
    {
        if (player != Owner)
            return Task.CompletedTask;
        CardModel? card = Owner.RunState.Rng.Niche.NextItem(Owner.Deck.Cards.Where(c => c.IsUpgradable));
        if (card is not null)
        {
            Flash();
            CardCmd.Upgrade(card);
        }
        return Task.CompletedTask;
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class LordDrone : ExusiaiAncientRelic
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new PowerVar<ArtifactPower>(1m)];

    public override decimal ModifyMaxEnergy(Player player, decimal amount) =>
        player == Owner ? amount + DynamicVars.Energy.BaseValue : amount;

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom || Owner.Creature.CombatState is not { } state)
            return;
        Flash();
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(),
            state.GetOpponentsOf(Owner.Creature).Where(c => c.IsAlive),
            DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, null);
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class SprayCan : ExusiaiAncientRelic
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState?.TurnNumber != 1)
            return;
        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(combatState.CreateCard(ModelDb.Card<Graffiti>(), Owner), PileType.Hand, Owner);
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class CactusTart : ExusiaiAncientRelic
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState?.TurnNumber != 1)
            return;
        List<CardModel> cards = ModelDb.CardPool<ExusiaiCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .Where(c => !c.IsUpgraded)
            .ToList();
        CardModel? canonical = Owner.RunState.Rng.CombatCardGeneration.NextItem(cards);
        if (canonical is null)
            return;
        CardModel generated = combatState.CreateCard(canonical, Owner);
        await AngelCmd.Add(new ThrowingPlayerChoiceContext(), generated);
        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner);
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class PrismaticWings : ExusiaiAncientRelic
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || Owner.PlayerCombatState?.TurnNumber is not <= 3)
            return;
        Flash();
        await PowerCmd.Apply<TemporarySoarPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class Confess47 : ExusiaiAncientRelic
{
    private int _cooldown;
    public override bool AddsPet => true;
    public override bool ShowCounter => _cooldown > 0;
    public override int DisplayAmount => _cooldown;

    [SavedProperty]
    public int Cooldown
    {
        get => _cooldown;
        set
        {
            AssertMutable();
            _cooldown = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeCombatStart() => PlayerCmd.AddPet<Confess47Pet>(Owner);

    public override async Task AfterObtained()
    {
        if (CombatManager.Instance.IsInProgress)
            await PlayerCmd.AddPet<Confess47Pet>(Owner);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return;
        if (Owner.PlayerCombatState?.GetPet<Confess47Pet>()?.Monster is not Confess47Pet pet)
            return;
        if (Cooldown > 0)
        {
            Cooldown--;
            if (Cooldown == 0)
                await CreatureCmd.TriggerAnim(pet.Creature, "WakeUp", 0.15f);
            return;
        }
        if (Owner.Gold < 1)
            return;
        Creature? target = Owner.RunState.Rng.Niche.NextItem(
            combatState.GetOpponentsOf(Owner.Creature).Where(c => c.IsAlive));
        if (target is null)
            return;
        Flash();
        await PlayerCmd.LoseGold(1, Owner, GoldLossType.Spent);
        await PlayerCmd.GainEnergy(2, Owner);
        await CreatureCmd.TriggerAnim(pet.Creature, "Attack", 0.15f);
        await CreatureCmd.Stun(target);
        Cooldown = 4;
        await CreatureCmd.TriggerAnim(pet.Creature, "Sleep", 0.15f);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        Cooldown = 0;
        return Task.CompletedTask;
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class BeaconOfNations : ExusiaiAncientRelic
{
    [SavedProperty] public int MarkedActIndex { get; set; } = -1;
    [SavedProperty] public int[] PathColumns { get; set; } = [];
    [SavedProperty] public int[] PathRows { get; set; } = [];
    public override bool HasUponPickupEffect => true;

    public IReadOnlyList<MapCoord> GetPath()
    {
        List<MapCoord> path = [];
        for (int i = 0; i < Math.Min(PathColumns.Length, PathRows.Length); i++)
            path.Add(new MapCoord(PathColumns[i], PathRows[i]));
        return path;
    }

    public override Task AfterObtained()
    {
        MarkedActIndex = Owner.RunState.CurrentActIndex;
        ActMap map = Owner.RunState.Map;
        MapPoint? cursor = Owner.RunState.CurrentMapPoint;
        if (cursor is null)
            return Task.CompletedTask;
        List<MapPoint> route = [cursor];
        while (cursor != map.BossMapPoint && route.Count <= map.GetAllMapPoints().Count())
        {
            List<MapPoint> choices = cursor.Children
                .Where(p => p == map.BossMapPoint || p.BFS_FindPath(map.BossMapPoint).Any())
                .OrderBy(p => p.coord.row).ThenBy(p => p.coord.col).ToList();
            MapPoint? next = Owner.RunState.Rng.Niche.NextItem(choices);
            if (next is null || next.coord.row <= cursor.coord.row)
                break;
            route.Add(next);
            cursor = next;
        }
        PathColumns = route.Select(p => p.coord.col).ToArray();
        PathRows = route.Select(p => p.coord.row).ToArray();
        MarkMap(map);
        Flash();
        return Task.CompletedTask;
    }

    public override ActMap ModifyGeneratedMapLate(MegaCrit.Sts2.Core.Runs.IRunState runState, ActMap map, int actIndex)
    {
        if (actIndex == MarkedActIndex)
            MarkMap(map);
        return map;
    }

    private void MarkMap(ActMap map)
    {
        foreach (MapCoord coord in GetPath())
        {
            MapPoint? point = map.GetPoint(coord);
            if (point is not null && point.PointType is MapPointType.Monster or MapPointType.Elite &&
                !point.Quests.Contains(this))
                point.AddQuest(this);
        }
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom || MarkedActIndex != Owner.RunState.CurrentActIndex ||
            Owner.RunState.CurrentMapPoint is not { } point || point.PointType == MapPointType.Boss ||
            !GetPath().Contains(point.coord))
            return false;
        List<CardModel> candidates = [];
        List<CardPoolModel> eligiblePools = [];
        foreach (CharacterModel character in ModelDb.AllCharacters)
        {
            try
            {
                List<CardModel> rares = character.CardPool
                    .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                    .Where(c => c.Rarity == CardRarity.Rare).ToList();
                if (rares.Count == 0)
                    continue;
                candidates.AddRange(rares);
                eligiblePools.Add(character.CardPool);
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"Beacon skipped {character.Id}: {ex.Message}");
            }
        }
        candidates = candidates.GroupBy(c => c.Id).Select(g => g.First())
            .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal).ToList();
        if (candidates.Count < 3)
            return false;
        List<CardModel> choices = [];
        while (choices.Count < 3)
        {
            CardModel? selected = Owner.PlayerRng.Rewards.NextItem(candidates);
            if (selected is null)
                break;
            choices.Add(Owner.RunState.CreateCard(selected, Owner));
            candidates.Remove(selected);
        }
        if (choices.Count != 3)
            return false;
        CardCreationOptions reroll = CardCreationOptions.ForNonCombatWithUniformOdds(
            eligiblePools, c => c.Rarity == CardRarity.Rare);
        rewards.Add(new CardReward(choices, CardCreationSource.Other, Owner, reroll));
        Flash();
        return true;
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class TheLaw : ExusiaiAncientRelic
{
    private int _charges = 3;
    public override bool HasUponPickupEffect => true;
    public override bool ShowCounter => _charges > 0;
    public override int DisplayAmount => _charges;

    [SavedProperty]
    public int Charges
    {
        get => _charges;
        set
        {
            AssertMutable();
            _charges = Math.Max(0, value);
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterObtained()
    {
        List<CardModel> curses = Owner.Deck.Cards.Where(c => c.Type == CardType.Curse).ToList();
        if (curses.Count > 0)
        {
            Flash();
            await CardPileCmd.RemoveFromDeck(curses);
        }
    }

    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card.Owner != Owner || card.Type != CardType.Curse || Charges <= 0)
            return true;
        Charges--;
        Flash();
        return false;
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class PenguinLogisticsId : ExusiaiAncientRelic
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override decimal ModifyMaxEnergy(Player player, decimal amount) =>
        player == Owner ? amount + DynamicVars.Energy.BaseValue : amount;

    public override async Task BeforeCombatStart()
    {
        if (await RelicLogisticsCmd.AddRandomDelivery(new ThrowingPlayerChoiceContext(), Owner, 1))
            Flash();
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class AFewFineVintages : ExusiaiAncientRelic
{
    public override bool HasUponPickupEffect => true;

    public override Task AfterObtained() => PlayerCmd.GainMaxPotionCount(2, Owner);

    public override async Task BeforeCombatStart()
    {
        if (!Owner.HasOpenPotionSlots)
            return;
        PotionModel[] wines = [ModelDb.Potion<UrsusBeluga>(), ModelDb.Potion<GaulChardonnay>(), ModelDb.Potion<YanFenjiu>()];
        PotionModel? choice = Owner.RunState.Rng.Niche.NextItem(wines);
        if (choice is null)
            return;
        Flash();
        await PotionCmd.TryToProcure(choice.ToMutable(), Owner);
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class BlackCard : ExusiaiAncientRelic
{
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice) =>
        player == Owner && LocalContext.IsMe(Owner) ? originalPrice * 0.25m : originalPrice;
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class MasterTape : ExusiaiAncientRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        List<Reward> rewards = [];
        foreach (CharacterModel character in ModelDb.AllCharacters.OrderBy(c => c.Id.ToString(), StringComparer.Ordinal))
        {
            if (character.Id == Owner.Character.Id)
                continue;
            try
            {
                List<CardModel> pool = character.CardPool
                    .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                    .Where(c => c.Rarity == CardRarity.Rare)
                    .GroupBy(c => c.Id).Select(g => g.First())
                    .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal)
                    .ToList();
                if (pool.Count < 3)
                    continue;
                List<CardModel> choices = [];
                while (choices.Count < 3)
                {
                    CardModel? picked = Owner.PlayerRng.Rewards.NextItem(pool);
                    if (picked is null)
                        break;
                    choices.Add(Owner.RunState.CreateCard(picked, Owner));
                    pool.Remove(picked);
                }
                if (choices.Count != 3)
                    continue;
                CardCreationOptions rerollOptions = CardCreationOptions.ForNonCombatWithUniformOdds(
                    [character.CardPool], c => c.Rarity == CardRarity.Rare);
                rewards.Add(new CardReward(choices, CardCreationSource.Other, Owner, rerollOptions));
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"Master Tape skipped {character.Id}: {ex.Message}");
            }
        }
        if (rewards.Count > 0)
        {
            Flash();
            await RewardsCmd.OfferCustom(Owner, rewards);
        }
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class IllGottenGains : ExusiaiAncientRelic
{
    [SavedProperty]
    public bool TookDamageThisCombat { get; set; }
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        for (int i = 0; i < 3; i++)
            await RelicLogisticsCmd.AddRandomTransit(Owner, 5);
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
            TookDamageThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner.Creature && result.UnblockedDamage > 0 && !props.HasFlag(ValueProp.Unblockable))
            TookDamageThisCombat = true;
        return Task.CompletedTask;
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class CompanyVan : ExusiaiAncientRelic
{
    private int _timesUsed;
    public override bool IsUsedUp => _timesUsed >= 2;
    public override bool ShowCounter => !IsUsedUp;
    public override int DisplayAmount => Math.Max(0, 2 - _timesUsed);

    [SavedProperty]
    public int TimesUsed
    {
        get => _timesUsed;
        set
        {
            AssertMutable();
            _timesUsed = Math.Clamp(value, 0, 2);
            if (IsUsedUp)
                Status = RelicStatus.Disabled;
            InvokeDisplayAmountChanged();
        }
    }

    public override bool IsAllowed(MegaCrit.Sts2.Core.Runs.IRunState runState) => runState.Players.Count == 1;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (IsUsedUp || Owner.RunState is not MegaCrit.Sts2.Core.Runs.RunState run || run.VisitedMapCoords.Count < 2)
            return Task.CompletedTask;
        IReadOnlyList<MapCoord> visited = run.VisitedMapCoords;
        MapCoord previous = visited[^2];
        MapCoord current = visited[^1];
        if (previous != current && previous.row == current.row)
        {
            TimesUsed++;
            Flash();
        }
        return Task.CompletedTask;
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class ReturnToSender : ExusiaiAncientRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        List<RelicModel> toRemove = Owner.Relics
            .Where(r => r.Rarity != RelicRarity.Starter)
            .Where(r => r.Capability<RelicLogisticsCapability>()?.IsTransit != true)
            .ToList();
        foreach (RelicModel relic in toRemove)
            await RelicCmd.Remove(relic);

        HashSet<ModelId> neowIds = ModelDb.AncientEvent<Neow>().AllPossibleOptions
            .Select(o => o.Relic?.Id).Where(id => id is not null).Select(id => id!).ToHashSet();
        List<RelicModel> nonNeowAncients = [];
        foreach (AncientEventModel ancient in ModelDb.AllAncients.Where(a => a is not Neow))
        {
            try
            {
                nonNeowAncients.AddRange(ancient.AllPossibleOptions
                    .Select(o => o.Relic).OfType<RelicModel>());
            }
            catch (Exception ex)
            {
                Entry.Logger.Warn($"Return to Sender skipped {ancient.Id}: {ex.Message}");
            }
        }
        List<RelicModel> neowAncients = ModelDb.AncientEvent<Neow>().AllPossibleOptions
            .Select(o => o.Relic).OfType<RelicModel>().ToList();
        List<Reward> rewards = [];
        foreach (RelicModel lost in toRemove)
        {
            RelicModel? replacement = lost.Rarity == RelicRarity.Ancient
                ? PickAncient(neowIds.Contains(lost.Id) ? neowAncients : nonNeowAncients)
                : PickOrdinary();
            if (replacement is not null)
                rewards.Add(new RelicReward(replacement.ToMutable(), Owner));
        }
        if (rewards.Count > 0)
        {
            Flash();
            await RewardsCmd.OfferCustom(Owner, rewards);
        }
    }

    private RelicModel? PickAncient(IEnumerable<RelicModel> source)
    {
        List<RelicModel> candidates = source
            .Where(IsCandidate)
            .GroupBy(r => r.Id).Select(g => g.First())
            .OrderBy(r => r.Id.ToString(), StringComparer.Ordinal).ToList();
        return Owner.RunState.Rng.Niche.NextItem(candidates);
    }

    private RelicModel? PickOrdinary()
    {
        RelicRarity[] rarities = [RelicRarity.Common, RelicRarity.Uncommon, RelicRarity.Rare, RelicRarity.Shop];
        List<(RelicRarity rarity, List<RelicModel> pool, float weight)> pools = [];
        foreach (RelicRarity rarity in rarities)
        {
            List<RelicModel> candidates = ModelDb.AllRelics
                .Where(r => r.Rarity == rarity && IsCandidate(r))
                .GroupBy(r => r.Id).Select(g => g.First())
                .OrderBy(r => r.Id.ToString(), StringComparer.Ordinal).ToList();
            if (candidates.Count > 0)
                pools.Add((rarity, candidates, rarity == RelicRarity.Shop ? 0.1f : 1f));
        }
        float roll = Owner.RunState.Rng.Niche.NextFloat(pools.Sum(p => p.weight));
        foreach (var pool in pools)
        {
            roll -= pool.weight;
            if (roll <= 0)
                return Owner.RunState.Rng.Niche.NextItem(pool.pool);
        }
        return pools.Count > 0 ? Owner.RunState.Rng.Niche.NextItem(pools[^1].pool) : null;
    }

    private bool IsCandidate(RelicModel relic)
    {
        if (!relic.IsStackable && Owner.Relics.Any(r => r.Id == relic.Id))
            return false;
        try { return relic.IsAllowed(Owner.RunState); }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"Return to Sender skipped {relic.Id}: {ex.Message}");
            return false;
        }
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class DjDeck : ExusiaiAncientRelic
{
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return;
        int turn = Owner.PlayerCombatState?.TurnNumber ?? 0;
        float roll = Owner.RunState.Rng.Niche.NextFloat();
        Flash();
        if (turn % 2 == 1)
        {
            int bonus = roll < 0.2f ? 25 :
                turn % 4 == 1 ? (roll < 0.5f ? 50 : 75) :
                (roll < 0.7f ? 50 : 75);
            await PowerCmd.Apply<StrongBeatPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
                bonus, Owner.Creature, null);
        }
        else
        {
            int reduction = roll < 0.2f ? 0 : roll < 0.6f ? 25 : 50;
            await PowerCmd.Apply<WeakBeatPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
                reduction, Owner.Creature, null);
        }
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class PrizedRecord : ExusiaiAncientRelic
{
    public override async Task AfterCreatureAddedToCombat(Creature creature)
    {
        if (creature.Side == Owner.Creature.Side || !creature.IsAlive)
            return;
        Flash();
        await CreatureCmd.SetMaxHp(creature, Math.Max(1, Math.Floor(creature.MaxHp * 0.8m)));
    }
}

[RegisterRelic(typeof(AncientRelicPool))]
public sealed class BossBusinessCard : ExusiaiAncientRelic
{
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<Emperor>(Owner);
        CardCmd.Upgrade(card);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }
}
