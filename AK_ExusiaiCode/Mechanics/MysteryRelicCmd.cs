using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

public static class MysteryRelicCmd
{
    private const string RngName = "ak_exusiai_free_delivery_mystery_relic";

    public static RelicModel? GetRevealed(Player player)
    {
        MysteryRelicCapability? state = GetState(player, create: false);
        if (state?.IsRevealed != true || string.IsNullOrEmpty(state.RelicId))
            return null;

        return RelicLogisticsCmd.GetTransitPool(player)
            .FirstOrDefault(relic => relic.Id.ToString() == state.RelicId);
    }

    public static RelicModel? Reveal(Player player)
    {
        RelicModel? revealed = GetRevealed(player);
        if (revealed != null)
            return revealed;

        MysteryRelicCapability? state = GetState(player, create: true);
        if (state == null)
            return null;

        List<RelicModel> candidates = RelicLogisticsCmd.GetTransitPool(player)
            .Where(relic => relic.IsAllowed(player.RunState))
            .OrderBy(relic => relic.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            return null;

        // All players shuffle the same ordered pool, then take their own slot.
        // With matching unlock pools this guarantees distinct results until the
        // pool is exhausted, while remaining deterministic across peers/load.
        new Rng(player.RunState.Rng.Seed, RngName).Shuffle(candidates);
        int playerSlot = Math.Max(0, player.RunState.GetPlayerSlotIndex(player));
        RelicModel selected = candidates[playerSlot % candidates.Count];
        state.Reveal(selected.Id);
        return selected;
    }

    private static MysteryRelicCapability? GetState(Player player, bool create)
    {
        RelicModel? carrier = player.Relics.FirstOrDefault(relic => relic.Rarity == RelicRarity.Starter)
            ?? player.Relics.FirstOrDefault();
        if (carrier == null)
            return null;

        return create
            ? carrier.GetOrCreateCapability<MysteryRelicCapability>()
            : carrier.Capability<MysteryRelicCapability>();
    }
}
