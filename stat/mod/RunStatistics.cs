using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AK_Exusiai.Mechanics;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Statistics;

internal static class RunStatistics
{
    private sealed class Metric(string id, int act)
    {
        public string Id = id;
        public int Act = act, Owned, Offered, Picked, Obtained, FloorSum, Upgraded, Removed;
        public JsonObject Json() => new()
        {
            ["id"] = Id, ["act"] = Act, ["owned"] = Owned,
            ["offered"] = Offered, ["picked"] = Picked, ["obtained"] = Obtained,
            ["floor_sum"] = FloorSum, ["upgraded"] = Upgraded, ["removed"] = Removed,
        };
    }
    private static bool Ours(string? id) => id?.StartsWith("AK_EXUSIAI_CARD_", StringComparison.Ordinal) == true || id?.StartsWith("AK_EXUSIAI_RELIC_", StringComparison.Ordinal) == true;

    internal static JsonObject Build(RunEndedEvent evt, SerializablePlayer me, string token, string gameVersion)
    {
        var run = evt.Run;
        var metrics = new Dictionary<(string, int), Metric>();
        var relicAcquisitions = new Dictionary<(string, int), int>();
        void RecordRelics(IEnumerable<string> ids, int floor)
        {
            foreach (var group in ids.Where(id => Ours(id)).GroupBy(id => id))
            {
                var key = (group.Key, Math.Max(0, floor));
                relicAcquisitions[key] = Math.Max(relicAcquisitions.GetValueOrDefault(key), group.Count());
            }
        }
        Metric? Get(string? id, int act = 0)
        {
            if (!Ours(id)) return null;
            var key = (id!, act);
            if (!metrics.TryGetValue(key, out var metric)) metrics[key] = metric = new Metric(id!, act);
            return metric;
        }
        foreach (var card in me.Deck) if (Get(card.Id?.Entry) is { } m) m.Owned = 1;
        // Exclude temporary Transit copies from permanent ownership, using the existing saved state.
        var permanentRelics = new List<SerializableRelic>();
        foreach (var relic in me.Relics)
        {
            if (Get(relic.Id?.Entry) is not { } m) continue;
            var mutable = RelicModel.FromSerializable(relic);
            if (mutable.Capability<RelicLogisticsCapability>()?.IsTransit == true) continue;
            m.Owned = 1;
            permanentRelics.Add(relic);
        }
        // Use actual acquisition floors for final-inventory fallback, grouped to preserve duplicate instances.
        foreach (var group in permanentRelics.GroupBy(r => r.FloorAddedToDeck ?? 0))
            RecordRelics(group.Select(r => r.Id!.Entry), group.Key);
        int floor = 0;
        for (int a = 0; a < run.MapPointHistory.Count; a++)
        foreach (var point in run.MapPointHistory[a])
        {
            floor++;
            var p = point.PlayerStats.FirstOrDefault(p => p.PlayerId == me.NetId);
            if (p == null) continue;
            foreach (var choice in p.CardChoices)
            {
                if (Get(choice.Card.Id?.Entry) is not { } total || Get(choice.Card.Id?.Entry, a + 1) is not { } act) continue;
                total.Offered++; act.Offered++;
                if (choice.wasPicked) { total.Picked++; act.Picked++; }
            }
            foreach (var card in p.CardsGained) if (Get(card.Id?.Entry) is { } m) { m.Obtained++; m.FloorSum += floor; }
            foreach (var card in p.CardsRemoved) if (Get(card.Id?.Entry) is { } m) m.Removed++;
            foreach (var card in p.UpgradedCards) if (Get(card.Entry) is { } m) m.Upgraded++;
            foreach (var choice in p.RelicChoices)
            {
                if (Get(choice.choice.Entry) is not { } m) continue;
                m.Offered++; if (choice.wasPicked) m.Picked++;
            }
            foreach (var choice in p.AncientChoices)
            {
                if (Get(choice.TextKey) is not { } m) continue;
                m.Offered++; if (choice.WasChosen) m.Picked++;
            }
            // Vanilla can log the same acquisition both as a choice and in the final inventory.
            // For each ID/floor, take the highest source count instead of summing duplicate views.
            RecordRelics(p.RelicChoices.Where(c => c.wasPicked).Select(c => c.choice.Entry), floor);
            RecordRelics(p.AncientChoices.Where(c => c.WasChosen).Select(c => c.TextKey), floor);
            RecordRelics(p.BoughtRelics.Select(r => r.Entry), floor);
        }
        foreach (var ((id, acquiredFloor), count) in relicAcquisitions)
            if (Get(id) is { } m) { m.Obtained += count; m.FloorSum += acquiredFloor * count; }
        string revision = ExusiaiTelemetry.Revision;
        var ownMod = ModManager.GetLoadedMods().FirstOrDefault(m => m.manifest?.id == Entry.ModId);
        var mods = new JsonArray();
        foreach (var mod in ModManager.GetLoadedMods().OrderBy(m => m.manifest?.id, StringComparer.Ordinal))
            if (mod.manifest is { } manifest)
            {
                var metadata = new JsonObject { ["id"] = manifest.id, ["version"] = manifest.version ?? "unknown" };
                if (!string.IsNullOrWhiteSpace(manifest.name)) metadata["title"] = manifest.name;
                if (mod.workshopId is > 0) metadata["workshop_id"] = mod.workshopId.Value.ToString();
                mods.Add(metadata);
            }
        // Only an HMAC leaves the machine; raw net IDs/start times/seeds are never sent.
        string runId = Convert.ToHexString(HMACSHA256.HashData(Convert.FromHexString(token), Encoding.UTF8.GetBytes($"{run.StartTime}|{run.SerializableRng.Seed}|{me.NetId}"))).ToLowerInvariant();
        return new JsonObject
        {
            ["schema"] = "exusiai.run.v2", ["id"] = runId, ["_owner_token"] = token,
            ["day"] = evt.OccurredAtUtc.ToString("yyyy-MM-dd"),
            ["version"] = ownMod?.manifest?.version ?? "unknown", ["revision"] = revision,
            ["game_version"] = gameVersion,
            ["ascension"] = run.Ascension, ["players"] = run.Players.Count,
            ["mode"] = run.DailyTime.HasValue ? "Daily" : run.GameMode.ToString(),
            ["victory"] = evt.IsVictory && !evt.IsAbandoned, ["abandoned"] = evt.IsAbandoned,
            ["floor"] = run.FloorReached, ["duration"] = run.RunTime,
            ["mods"] = mods, ["entities"] = new JsonArray(metrics.Values.OrderBy(m => m.Id, StringComparer.Ordinal).ThenBy(m => m.Act).Select(m => (JsonNode)m.Json()).ToArray()),
            ["details"] = DetailedStatistics.Build(evt, me, token, relicAcquisitions),
        };
    }
}
