using AK_Exusiai.Content;
using AK_Exusiai.Powers;
using AK_Exusiai.Relics;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
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

    public static bool IsDeliveryTarget(RelicModel relic)
    {
        RelicLogisticsCapability? state = relic.Capability<RelicLogisticsCapability>();
        return relic.Rarity != RelicRarity.Starter &&
               !relic.IsMelted &&
               relic.Status != RelicStatus.Disabled &&
               state?.IsExpiredTransit != true &&
               (state?.IsOperational ?? true);
    }

    public static IReadOnlyList<RelicModel> SortForDisplay(
        Player player,
        IEnumerable<RelicModel> relics) =>
        relics
            .Select((relic, inputIndex) => new
            {
                Relic = relic,
                InputIndex = inputIndex,
                State = relic.Capability<RelicLogisticsCapability>(),
                AcquisitionIndex = FindInstanceIndex(player.Relics, relic),
            })
            .OrderBy(item => item.Relic is Circlet ? 1 : 0)
            .ThenBy(item => item.State?.IsTransit == true ? 1 : 0)
            .ThenBy(item => item.State?.DeliveryRemaining ?? 0)
            .ThenByDescending(item => item.State?.IsTransit == true
                ? item.State.TransitRemaining
                : 0)
            .ThenBy(item => item.AcquisitionIndex >= 0
                ? item.AcquisitionIndex
                : int.MaxValue)
            .ThenBy(item => item.InputIndex)
            .Select(item => item.Relic)
            .ToList();

    public static IReadOnlyList<RelicModel> GetDeliveryTargets(Player player) =>
        SortForDisplay(player, player.Relics.Where(IsDeliveryTarget));

    public static async Task<RelicModel?> ChooseDeliveryTarget(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        IReadOnlyList<RelicModel> targets = GetDeliveryTargets(player);
        return targets.Count == 0
            ? null
            : await SelectRelic(choiceContext, player, targets);
    }

    public static async Task<bool> ChooseAndAddDelivery(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount)
    {
        RelicModel? relic = await ChooseDeliveryTarget(choiceContext, player);
        if (relic == null)
            return false;

        return await AddDelivery(choiceContext, relic, amount);
    }

    public static async Task<bool> AddRandomDelivery(
        PlayerChoiceContext choiceContext,
        Player player,
        int amount)
    {
        if (amount <= 0)
            return false;

        RelicModel? relic = player.RunState.Rng.Niche.NextItem(GetDeliveryTargets(player));
        return relic != null && await AddDelivery(choiceContext, relic, amount);
    }

    public static async Task<bool> AddDelivery(
        PlayerChoiceContext choiceContext,
        RelicModel relic,
        int amount)
    {
        if (amount <= 0 || !IsDeliveryTarget(relic))
            return false;

        List<BossMedal> activeBossMedals = relic.Owner.Relics
            .OfType<BossMedal>()
            .Where(IsOperational)
            .ToList();
        relic.GetOrCreateCapability<RelicLogisticsCapability>().AddDelivery(amount);

        foreach (BossMedal bossMedal in activeBossMedals)
            await bossMedal.AfterDeliveryAdded(choiceContext);

        CargoInMotionPower? cargo = relic.Owner.Creature.GetPower<CargoInMotionPower>();
        bool isTransit = relic.Capability<RelicLogisticsCapability>()?.IsTransit == true;
        bool transitAllowed = relic.Owner.Creature.HasPower<CargoInMotionTransitPower>();
        if (cargo != null && (!isTransit || transitAllowed))
        {
            for (int i = 0; i < cargo.Amount; i++)
                await AddRandomPermanentRelic(relic.Owner);
        }

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
        SortForDisplay(
            player,
            player.Relics.Where(relic =>
                relic.Capability<RelicLogisticsCapability>()?.DeliveryRemaining > 0));

    public static IReadOnlyList<RelicModel> GetTransitRelics(Player player) =>
        SortForDisplay(
            player,
            player.Relics.Where(relic =>
                relic.Capability<RelicLogisticsCapability>()?.IsTransit == true));

    public static void ReactivateDelivery(RelicModel relic) =>
        relic.Capability<RelicLogisticsCapability>()?.ReactivateDelivery();

    public static async Task<RelicModel?> ChooseDeliveredRelic(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        IReadOnlyList<RelicModel> targets = GetDeliveredRelics(player);
        return targets.Count == 0
            ? null
            : await SelectRelic(choiceContext, player, targets);
    }

    public static async Task<IReadOnlyList<RelicModel>> ChooseDeliveredRelics(
        PlayerChoiceContext choiceContext,
        Player player,
        int maxCount)
    {
        List<RelicModel> available = GetDeliveredRelics(player).ToList();
        List<RelicModel> selected = [];
        while (available.Count > 0 && selected.Count < maxCount)
        {
            RelicModel? relic = await SelectRelic(choiceContext, player, available);
            if (relic == null)
                break;

            selected.Add(relic);
            available.Remove(relic);
        }

        return selected;
    }

    public static async Task<RelicModel?> ChooseTransitRelic(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        IReadOnlyList<RelicModel> targets = GetTransitRelics(player);
        return targets.Count == 0
            ? null
            : await SelectRelic(choiceContext, player, targets);
    }

    public static async Task<IReadOnlyList<RelicModel>> ChooseTransitRelics(
        PlayerChoiceContext choiceContext,
        Player player,
        int maxCount)
    {
        List<RelicModel> available = GetTransitRelics(player).ToList();
        if (maxCount <= 0 || available.Count == 0)
            return [];
        if (available.Count <= maxCount)
            return available;

        List<RelicModel> selected = [];
        while (selected.Count < maxCount)
        {
            RelicModel? relic = await SelectRelic(choiceContext, player, available);
            if (relic == null)
                break;

            selected.Add(relic);
            available.Remove(relic);
        }

        return selected;
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

    public static async Task<RelicModel?> AddRandomPermanentRelic(Player player)
    {
        HashSet<ModelId> permanentlyOwnedNonStackable = player.Relics
            .Where(relic => relic.Capability<RelicLogisticsCapability>()?.IsTransit != true)
            .Where(relic => !relic.IsStackable)
            .Select(relic => relic.Id)
            .ToHashSet();
        List<RelicModel> candidates = GetTransitPool(player)
            .Where(relic => relic.IsAllowed(player.RunState))
            .Where(relic => !permanentlyOwnedNonStackable.Contains(relic.Id))
            .OrderBy(relic => relic.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        RelicModel? selected = player.RunState.Rng.Niche.NextItem(candidates);
        return selected == null
            ? null
            : await RelicCmd.Obtain(selected.ToMutable(), player);
    }

    public static void EndCombat(Player player)
    {
        foreach (RelicModel relic in player.Relics)
        {
            if (relic is BossMedal bossMedal)
                bossMedal.ResetAfterCombat();
            relic.Capability<RelicLogisticsCapability>()?.EndCombat();
        }
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

    private static int FindInstanceIndex(
        IReadOnlyList<RelicModel> relics,
        RelicModel target)
    {
        for (int i = 0; i < relics.Count; i++)
        {
            if (ReferenceEquals(relics[i], target))
                return i;
        }

        return -1;
    }

    private static async Task<RelicModel?> SelectRelic(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<RelicModel> relics)
    {
        await choiceContext.SignalPlayerChoiceBegun(player, PlayerChoiceOptions.None);
        try
        {
            return await RelicSelectCmd.FromChooseARelicScreen(player, relics);
        }
        finally
        {
            await choiceContext.SignalPlayerChoiceEnded();
        }
    }
}
