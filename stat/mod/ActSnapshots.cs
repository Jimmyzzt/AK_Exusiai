using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Models.Capabilities;
using AK_Exusiai.Mechanics;

namespace AK_Exusiai.Statistics;

internal static class ActSnapshots
{
    private static JsonObject _decks = new();
    private static string? _key;
    private static bool _fresh;
    private static string? _path;
    internal static void InitializePath(string path) => _path = path;
    internal static void Begin(bool newRun) { _key = null; _decks = new(); _fresh = newRun; }
    private static string Key(string seed, ulong id, string token) => Convert.ToHexString(HMACSHA256.HashData(Convert.FromHexString(token), Encoding.UTF8.GetBytes(seed + "|" + id)));
    private static void Ensure(string key)
    {
        if (_key == key) return;
        _key = key; _decks = new();
        if (_fresh || _path == null || !File.Exists(_path)) return;
        try
        {
            var saved = JsonNode.Parse(File.ReadAllText(_path));
            if (saved?["key"]?.GetValue<string>() == key && saved["decks"] is JsonObject decks) _decks = (JsonObject)decks.DeepClone();
        }
        catch { /* A missing cache only makes boundary ownership unavailable. */ }
    }
    internal static void Boundary(ActEnteringEvent evt, string token)
    {
        var state = evt.RunManager.State;
        if (state == null || _path == null) return;
        var me = LocalContext.GetMe(state);
        if (me == null || me.Character.Id.Entry != ModelDb.GetId<Characters.Exusiai>().Entry || evt.TargetActIndex <= 0) return;
        Ensure(Key(state.Rng.StringSeed, me.NetId, token));
        int completed = Math.Min(evt.TargetActIndex, state.MapPointHistory.Count);
        if (completed < 1) return;
        _decks[completed.ToString()] = Deck(me.ToSerializable());
        File.WriteAllText(_path + ".tmp", new JsonObject { ["key"] = _key, ["decks"] = _decks.DeepClone() }.ToJsonString());
        File.Move(_path + ".tmp", _path, true);
    }
    internal static JsonArray? Get(string? seed, ulong id, string token, int act)
    {
        if (seed == null) return null;
        Ensure(Key(seed, id, token));
        return _decks[act.ToString()] is JsonArray deck ? (JsonArray)deck.DeepClone() : null;
    }
    internal static JsonArray Deck(SerializablePlayer me)
    {
        var keys = new HashSet<(string, int)>();
        foreach (var card in me.Deck)
            if (card.Id?.Entry.StartsWith("AK_EXUSIAI_CARD_", StringComparison.Ordinal) == true) keys.Add((card.Id.Entry, card.CurrentUpgradeLevel > 0 ? 1 : 0));
        foreach (var relic in me.Relics)
        {
            if (relic.Id?.Entry.StartsWith("AK_EXUSIAI_RELIC_", StringComparison.Ordinal) != true) continue;
            if (RelicModel.FromSerializable(relic).Capability<RelicLogisticsCapability>()?.IsTransit == true) continue;
            keys.Add((relic.Id.Entry, 0));
        }
        return new JsonArray(keys.OrderBy(k => k.Item1, StringComparer.Ordinal).ThenBy(k => k.Item2)
            .Select(k => (JsonNode)new JsonObject { ["id"] = k.Item1, ["variant"] = k.Item2 }).ToArray());
    }
}
