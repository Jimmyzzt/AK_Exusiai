using AK_Exusiai.Content;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

public static class RelicLogisticsCmd
{
    private static readonly HashSet<RelicRarity> TransitRarities =
    [
        RelicRarity.Common,
        RelicRarity.Uncommon,
        RelicRarity.Rare,
    ];

    public static bool IsOperational(RelicModel relic) =>
        relic.Capability<RelicLogisticsCapability>()?.IsOperational ?? true;

    public static bool IsDeliveryTarget(RelicModel relic) =>
        relic.Rarity != RelicRarity.Starter &&
        relic.Status != RelicStatus.Disabled &&
        IsOperational(relic);

    public static IReadOnlyList<RelicModel> GetDeliveryTargets(Player player) =>
        player.Relics.Where(IsDeliveryTarget).ToList();

    public static async Task<RelicModel?> ChooseDeliveryTarget(Player player)
    {
        IReadOnlyList<RelicModel> targets = GetDeliveryTargets(player);
        return targets.Count == 0
            ? null
            : await RelicSelectCmd.FromChooseARelicScreen(player, targets);
    }

    public static async Task<bool> ChooseAndAddDelivery(Player player, int amount)
    {
        RelicModel? relic = await ChooseDeliveryTarget(player);
        if (relic == null)
            return false;

        AddDelivery(relic, amount);
        return true;
    }

    public static bool AddDelivery(RelicModel relic, int amount)
    {
        if (amount <= 0 || !IsDeliveryTarget(relic))
            return false;

        relic.GetOrCreateCapability<RelicLogisticsCapability>().AddDelivery(amount);
        return true;
    }

    public static void ReduceAllDelivery(Player player, int amount)
    {
        if (amount <= 0)
            return;

        foreach (RelicModel relic in player.Relics)
            relic.Capability<RelicLogisticsCapability>()?.ReduceDelivery(amount);
    }

    public static IReadOnlyList<RelicModel> GetDeliveredRelics(Player player) =>
        player.Relics
            .Where(relic => relic.Capability<RelicLogisticsCapability>()?.DeliveryRemaining > 0)
            .ToList();

    public static IReadOnlyList<RelicModel> GetTransitRelics(Player player) =>
        player.Relics
            .Where(relic => relic.Capability<RelicLogisticsCapability>()?.IsTransit == true)
            .ToList();

    public static async Task<RelicModel?> ChooseDeliveredRelic(Player player)
    {
        IReadOnlyList<RelicModel> targets = GetDeliveredRelics(player);
        return targets.Count == 0
            ? null
            : await RelicSelectCmd.FromChooseARelicScreen(player, targets);
    }

    public static async Task<RelicModel?> ChooseTransitRelic(Player player)
    {
        IReadOnlyList<RelicModel> targets = GetTransitRelics(player);
        return targets.Count == 0
            ? null
            : await RelicSelectCmd.FromChooseARelicScreen(player, targets);
    }

    public static async Task<RelicModel?> AddNamedTransit<T>(Player player, int amount)
        where T : RelicModel
    {
        return await AddNamedTransit(player, ModelDb.Relic<T>(), amount);
    }

    public static async Task<RelicModel?> AddNamedTransit(
        Player player,
        RelicModel canonical,
        int amount)
    {
        if (amount <= 0)
            return null;

        RelicModel? existingTransit = player.Relics.FirstOrDefault(relic =>
            relic.Id == canonical.Id &&
            relic.Capability<RelicLogisticsCapability>()?.IsTransit == true);
        if (existingTransit != null)
        {
            existingTransit.GetOrCreateCapability<RelicLogisticsCapability>()
                .StartOrExtendTransit(amount);
            return existingTransit;
        }

        return await AddNewTransit(player, canonical, amount);
    }

    public static async Task<RelicModel?> AddRandomTransit(Player player, int amount)
    {
        if (amount <= 0)
            return null;

        HashSet<ModelId> excludedTransitIds = player.Relics
            .Where(relic => relic.Capability<RelicLogisticsCapability>()?.IsTransit == true)
            .Select(relic => relic.Id)
            .ToHashSet();

        List<RelicModel> candidates = GetTransitPool(player)
            .Where(relic => !excludedTransitIds.Contains(relic.Id))
            .Where(relic => relic.IsAllowed(player.RunState))
            .OrderBy(relic => relic.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        RelicModel? selected = player.RunState.Rng.Niche.NextItem(candidates);
        return selected == null ? null : await AddNewTransit(player, selected, amount);
    }

    public static IReadOnlyList<RelicModel> GetTransitPool(Player player)
    {
        IEnumerable<RelicModel> shared = ModelDb.RelicPool<SharedRelicPool>()
            .GetUnlockedRelics(player.UnlockState);
        IEnumerable<RelicModel> character = ModelDb.RelicPool<ExusiaiRelicPool>()
            .GetUnlockedRelics(player.UnlockState);

        return shared
            .Concat(character)
            .Where(relic => TransitRarities.Contains(relic.Rarity))
            .GroupBy(relic => relic.Id)
            .Select(group => group.First())
            .ToList();
    }

    public static void EndCombat(Player player)
    {
        foreach (RelicModel relic in player.Relics)
            relic.Capability<RelicLogisticsCapability>()?.EndCombat();
    }

    private static async Task<RelicModel> AddNewTransit(
        Player player,
        RelicModel canonical,
        int amount)
    {
        RelicModel relic = canonical.ToMutable();
        relic.GetOrCreateCapability<RelicLogisticsCapability>().StartOrExtendTransit(amount);

        // Intentionally bypass RelicCmd.Obtain: Transit must not remove anything
        // from either the character or shared vanilla relic grab bag.
        player.AddRelicInternal(relic);
        relic.FloorAddedToDeck = player.RunState.TotalFloor;

        if (LocalContext.IsMe(player))
        {
            NRun.Instance?.GlobalUi.RelicInventory.AnimateRelic(relic);
            NDebugAudioManager.Instance?.Play("relic_get.mp3");
            SaveManager.Instance.MarkRelicAsSeen(relic);
        }

        await relic.AfterObtained();
        return relic;
    }
}
