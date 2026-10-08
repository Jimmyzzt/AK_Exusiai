using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;

namespace AK_Exusiai.Statistics;

internal static class DetailedStatistics
{
    private static bool Ours(string? id) => id?.StartsWith("AK_EXUSIAI_CARD_", StringComparison.Ordinal) == true || id?.StartsWith("AK_EXUSIAI_RELIC_", StringComparison.Ordinal) == true;
    internal static JsonObject Build(RunEndedEvent evt, SerializablePlayer me, string token, Dictionary<(string, int), int> relics)
    {
        var run = evt.Run; var acts = new JsonArray(); var offers = new JsonArray(); var fights = new JsonArray();
        var items = new Dictionary<(string Id, int Act, int Variant), JsonObject>();
        JsonObject? Item(string? id, int act, int variant)
        {
            if (!Ours(id)) return null;
            var key = (id!, act, variant);
            if (!items.TryGetValue(key, out var item)) items[key] = item = new JsonObject
            { ["id"] = id, ["act"] = act, ["variant"] = variant, ["offered"] = 0, ["picked"] = 0, ["obtained"] = 0, ["floor_sum"] = 0, ["upgraded"] = 0, ["removed"] = 0 };
            return item;
        }
        void Add(JsonObject? item, string key, int amount = 1) { if (item != null) item[key] = item[key]!.GetValue<int>() + amount; }
        int position = 0;
        for (int a = 0; a < run.MapPointHistory.Count; a++)
        {
            var history = run.MapPointHistory[a];
            var boundary = ActSnapshots.Get(run.SerializableRng.Seed, me.NetId, token, a + 1);
            var lastBoss = history.LastOrDefault(p => p.Rooms.Any(r => r.RoomType == RoomType.Boss));
            bool completed = a < run.MapPointHistory.Count - 1
                || (evt.IsVictory && !evt.IsAbandoned && a == run.MapPointHistory.Count - 1);
            if (lastBoss != null && a == run.MapPointHistory.Count - 1 && lastBoss.PlayerStats.Any(p => p.CurrentHp > 0)) completed = true;
            bool final = a == run.MapPointHistory.Count - 1;
            acts.Add(new JsonObject { ["act"] = a + 1, ["floors"] = history.Count, ["completed"] = completed,
                ["snapshot_known"] = final || boundary != null, ["deck"] = final ? ActSnapshots.Deck(me) : boundary ?? new JsonArray() });
            for (int f = 0; f < history.Count; f++)
            {
                position++;
                var point = history[f]; var player = point.PlayerStats.FirstOrDefault(p => p.PlayerId == me.NetId);
                if (player == null) continue;
                foreach (var choice in player.CardChoices)
                {
                    int variant = choice.Card.CurrentUpgradeLevel > 0 ? 1 : 0;
                    var item = Item(choice.Card.Id?.Entry, a + 1, variant);
                    if (item == null) continue;
                    Add(item, "offered"); if (choice.wasPicked) Add(item, "picked");
                    offers.Add(new JsonObject { ["id"] = choice.Card.Id!.Entry, ["act"] = a + 1, ["floor"] = f + 1,
                        ["position"] = position, ["variant"] = variant, ["picked"] = choice.wasPicked });
                }
                foreach (var card in player.CardsGained) { var m = Item(card.Id?.Entry, a + 1, card.CurrentUpgradeLevel > 0 ? 1 : 0); Add(m, "obtained"); Add(m, "floor_sum", position); }
                foreach (var card in player.CardsRemoved) Add(Item(card.Id?.Entry, a + 1, card.CurrentUpgradeLevel > 0 ? 1 : 0), "removed");
                foreach (var card in player.UpgradedCards) Add(Item(card.Entry, a + 1, 1), "upgraded");
                foreach (var c in player.RelicChoices) { var m = Item(c.choice.Entry, a + 1, 0); Add(m, "offered"); if (c.wasPicked) Add(m, "picked"); }
                foreach (var c in player.AncientChoices) { var m = Item(c.TextKey, a + 1, 0); Add(m, "offered"); if (c.WasChosen) Add(m, "picked"); }
                var rooms = point.Rooms.Where(r => r.TurnsTaken > 0).ToArray();
                if (rooms.Length > 0) fights.Add(new JsonObject { ["act"] = a + 1, ["floor"] = f + 1, ["position"] = position,
                    ["encounter"] = string.Join("+", rooms.Select(r => r.ModelId?.Entry ?? (r.MonsterIds.Count > 0 ? string.Join("_", r.MonsterIds.Select(m => m.Entry)) : r.RoomType.ToString()))),
                    ["damage"] = Math.Max(0, player.DamageTaken), ["turns"] = rooms.Sum(r => r.TurnsTaken) });
            }
        }
        foreach (var ((id, floor), count) in relics)
        {
            int remaining = floor, act = 1;
            while (act < run.MapPointHistory.Count && remaining > run.MapPointHistory[act - 1].Count) { remaining -= run.MapPointHistory[act - 1].Count; act++; }
            Add(Item(id, act, 0), "obtained", count); Add(Item(id, act, 0), "floor_sum", floor * count);
        }
        return new JsonObject { ["win3"] = acts.Count >= 3 && acts[2]!["completed"]!.GetValue<bool>(), ["acts"] = acts,
            ["offers"] = offers, ["fights"] = fights, ["items"] = new JsonArray(items.Values.Select(m => (JsonNode)m).ToArray()) };
    }
}
