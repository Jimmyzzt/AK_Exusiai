using System.Text.Json;
using System.Security.Cryptography;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace AK_Exusiai.Effects;

public sealed class CardEffectConfig
{
    public string Preset { get; set; } = "inherit";
    public string Launch { get; set; } = "inherit";
    public string Hit { get; set; } = "inherit";
    public string LaunchSound { get; set; } = "inherit";
    public string HitSound { get; set; } = "inherit";
    public string Animation { get; set; } = "inherit";
    public string Repeat { get; set; } = "per_hit";
    public string Phase { get; set; } = "start";
    public float Delay { get; set; } = -1;
    public float Volume { get; set; } = 1;
    public float[] Offset { get; set; } = [0, 0];
    public Dictionary<string, float[]> AppearanceOffsets { get; set; } = [];
    public bool Ground { get; set; }
}

public sealed class EffectRecipe
{
    public string Launch { get; set; } = "";
    public string Hit { get; set; } = "";
    public string LaunchSound { get; set; } = "";
    public string HitSound { get; set; } = "";
    public string Animation { get; set; } = "Attack";
    public float Delay { get; set; }
    public float AnimationDelay { get; set; } = -1;
    public bool PreAnimation { get; set; }
    public bool EmbeddedAudio { get; set; }
    public bool LaunchOnTargets { get; set; }
    public bool Ground { get; set; }
    public string Tint { get; set; } = "ffffff";
    public string VfxColor { get; set; } = "Red";
    public float Scale { get; set; } = 1;
    public string[] LaunchExtras { get; set; } = [];
    public string[] HitExtras { get; set; } = [];
}

public sealed class EffectEntry
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Status { get; set; } = "";
    public string Adapter { get; set; } = "";
    public string Resource { get; set; } = "";
    public string Factory { get; set; } = "";
    public string Signature { get; set; } = "";
    public EffectRecipe? Recipe { get; set; }
}

public sealed class EffectBinding
{
    public CardEffectConfig Base { get; set; } = new();
    // Legacy JSON's upgrade field is ignored: both versions share Base.
}

public sealed class EffectDocument
{
    public int Schema { get; set; }
    public Dictionary<string, EffectBinding> Cards { get; set; } = [];
}

public sealed class EffectCatalog
{
    public int Schema { get; set; }
    public string GameSha256 { get; set; } = "";
    public List<EffectEntry> Entries { get; set; } = [];
}

internal static class CardEffectStore
{
    internal static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = true };
    internal const string BridgeDirectory = "user://AK_Exusiai/card_effect_manager";
    internal static Dictionary<string, EffectEntry> Entries { get; private set; } = [];
    internal static string GameHash { get; private set; } = "";
    internal static bool CatalogMatchesGame { get; private set; }
    private static EffectDocument _packaged = new();
    internal static EffectDocument? Override { get; set; }

    internal static void Initialize(string? gameAssemblyPath = null)
    {
        EffectCatalog catalog = Read<EffectCatalog>($"{Entry.ResPath}/config/card_effect_catalog.json");
        if (catalog.Schema != 1) throw new InvalidDataException("Unknown effect catalog schema.");
        Entries = catalog.Entries.ToDictionary(x => x.Id);
        string assemblyPath = gameAssemblyPath ?? typeof(CardModel).Assembly.Location;
        // A standalone Godot editor can load references from memory. Unknown builds
        // retain native card visuals rather than breaking the character's initialization.
        GameHash = "";
        if (System.IO.File.Exists(assemblyPath))
            using (var assembly = System.IO.File.OpenRead(assemblyPath))
                GameHash = Convert.ToHexString(SHA256.HashData(assembly)).ToLowerInvariant();
        CatalogMatchesGame = GameHash == catalog.GameSha256;
        _packaged = Read<EffectDocument>($"{Entry.ResPath}/config/card_effects.json");
        Validate(_packaged);
    }

    internal static T Read<T>(string path) => JsonSerializer.Deserialize<T>(Godot.FileAccess.GetFileAsString(path), Json)
        ?? throw new InvalidDataException($"Empty effect document: {path}");

    internal static EffectBinding? Find(string id, bool singlePlayer) =>
        CatalogMatchesGame ? (singlePlayer && Override != null ? Override : _packaged).Cards.GetValueOrDefault(id) : null;

    internal static void Validate(EffectDocument doc)
    {
        if (doc.Schema != 1 || doc.Cards == null) throw new InvalidDataException("Unknown effect config schema.");
        foreach (var (id, binding) in doc.Cards)
        {
            if (!id.StartsWith("AK_EXUSIAI_CARD_", StringComparison.Ordinal) || binding?.Base == null)
                throw new InvalidDataException($"Invalid card binding: {id}");
            Validate(binding.Base);
        }
    }

    internal static void Validate(CardEffectConfig c)
    {
        foreach (var (id, kind) in new[] { (c.Preset, "preset"), (c.Launch, "vfx"), (c.Hit, "vfx"), (c.LaunchSound, "sfx"), (c.HitSound, "sfx") })
        {
            if (id is "inherit" or "none") continue;
            if (!Entries.TryGetValue(id, out var e) || e.Status != "adapted" || e.Kind != kind)
                throw new InvalidDataException($"Unsupported {kind}: {id}");
        }
        if (c.Animation is not ("inherit" or "Attack" or "Cast" or "none") || c.Repeat is not ("once" or "per_hit") || c.Phase is not ("start" or "end"))
            throw new InvalidDataException("Invalid effect playback option.");
        if (!float.IsFinite(c.Delay) || c.Delay < -1 || c.Delay > 5 || !float.IsFinite(c.Volume) || c.Volume < 0 || c.Volume > 2)
            throw new InvalidDataException("Effect delay/volume out of range.");
        ValidateOffset(c.Offset);
        if (c.AppearanceOffsets == null) throw new InvalidDataException("Invalid appearance offsets.");
        foreach (var offset in c.AppearanceOffsets.Values) ValidateOffset(offset);
    }

    private static void ValidateOffset(float[] value)
    {
        if (value == null || value.Length != 2 || value.Any(x => !float.IsFinite(x) || Math.Abs(x) > 1500))
            throw new InvalidDataException("Effect offset must be two finite coordinates within ±1500.");
    }
}
