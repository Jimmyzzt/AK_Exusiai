using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils;

namespace AK_Exusiai.Settings;

internal static class ExusiaiModSettings
{
    private static AncientAppearanceConfig _local = new();
    private static AudioSettingsConfig _audio = new();
    private static string _path = "";
    private static string _audioPath = "";
    private static I18N _text = null!;
    private static RunSavedData<AncientAppearanceConfig> _run = null!;
    private static readonly List<IModSettingsBinding> Bindings = [];
    private static IDisposable? _staging, _preparing;

    internal static ModSettingsText Text(string key) => ModSettingsText.I18N(_text, key, key);
    internal static string ButtonText => _text.Get("more_settings", "More settings");
    internal static float AudioVolume(string id, float independentVolume) => _audio.ApplyToSound(id, independentVolume);

    internal static void Initialize()
    {
        _path = ProjectSettings.GlobalizePath($"user://{Entry.ModId}/ancients.json");
        _audioPath = ProjectSettings.GlobalizePath($"user://{Entry.ModId}/audio.json");
        _text = new I18N("Exusiai-Settings", pckFolders: [$"{Entry.ResPath}/localization/settings"]);
        try
        {
            if (File.Exists(_path))
                _local = (JsonSerializer.Deserialize<AncientAppearanceConfig>(File.ReadAllText(_path)) ?? new()).Copy();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            Entry.Logger.Warn($"Unable to load Ancient settings; using defaults. {e.Message}");
        }
        try
        {
            if (File.Exists(_audioPath))
                _audio = (JsonSerializer.Deserialize<AudioSettingsConfig>(File.ReadAllText(_audioPath)) ?? new()).Copy();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            Entry.Logger.Warn($"Unable to load audio settings; using defaults. {e.Message}");
        }
        _run = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).Register<AncientAppearanceConfig>(
            "ancientAppearance", () => new(), new RunSavedDataOptions { SchemaVersion = 1 });
        _staging = RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(e =>
        {
            // Shared run slots accept host contributions only. Capture again on Committing,
            // so settings edited after entering the lobby are included in the start message.
            if (e.IsHost && e.Reason == RunSavedDataLobbyStagingReason.Committing)
                _run.Lobby.Set(e.Lobby, _local.Copy());
        }, false);
        _preparing = RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(e =>
        {
            // Also covers single-player entry points without a character-select lobby.
            if (!e.IsMultiplayer && !_run.TryGet(e.RunState, out _))
                _run.Set(e.RunState, _local.Copy());
        }, false);
        RitsuLibFramework.RegisterModSettings(Entry.ModId, page =>
        {
            page.WithTitle(Text("title"));
            page.AddSection("ancients", section =>
            {
                section.WithTitle(Text("ancients")).Collapsible(true);
                section.AddToggle("enabled", Text("enabled"), Bind("enabled", () => _local.Enabled, v => _local.Enabled = v), Text("enabled_help"));
                AddAncient(section, "laterano", () => _local.Laterano);
                AddAncient(section, "penguin", () => _local.PenguinLogistics);
                section.AddButton("reset", Text("reset"), Text("reset"), host =>
                {
                    _local = new();
                    Save();
                    foreach (IModSettingsBinding binding in Bindings) host.MarkDirty(binding);
                    host.RequestRefreshAfterDataModelBatchChange();
                });
            });
            page.AddSection("audio", section =>
            {
                section.WithTitle(Text("audio"));
                var volume = ModSettingsBindings.Callback(Entry.ModId, "audio.gunshotVolume",
                    () => _audio.GunshotVolumePercent, v => _audio.GunshotVolumePercent = v, SaveAudio);
                section.AddSlider("gunshot_volume", Text("gunshot_volume"), volume,
                    0, 100, 1, value => $"{value:0}%", Text("gunshot_volume_help"));
            });
            Statistics.ExusiaiTelemetry.AddSettings(page);
        }, Entry.ModId);
    }

    private static IModSettingsValueBinding<T> Bind<T>(string key, Func<T> read, Action<T> write)
    {
        var binding = ModSettingsBindings.Callback(Entry.ModId, $"ancients.{key}", read, write, Save);
        Bindings.Add(binding);
        return binding;
    }

    private static void AddAncient(ModSettingsSectionBuilder section, string id, Func<AncientAppearanceRule> rule)
    {
        section.AddEnumChoice($"{id}_mode", Text(id), Bind($"{id}.mode", () => rule().Mode, v => rule().Mode = v),
            mode => Text($"mode_{mode.ToString().ToLowerInvariant()}"), presentation: ModSettingsChoicePresentation.Dropdown);
        section.AddSlider($"{id}_percent", Text($"{id}_percent"), Bind($"{id}.percent", () => rule().Percent, v => rule().Percent = v),
            0, 100, 1, value => $"{value:0}%", Text("percent_help"));
        section.WithEntryEnabledWhen($"{id}_mode", () => _local.Enabled);
        section.WithEntryVisibleWhen($"{id}_percent", () => _local.Enabled && rule().Mode == AncientAppearanceMode.Custom);
    }

    private static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(_local.Copy()));
    }

    private static void SaveAudio()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_audioPath)!);
        File.WriteAllText(_audioPath, JsonSerializer.Serialize(_audio.Copy()));
    }

    internal static AncientAppearanceConfig ForRun(RunState run)
    {
        if (_run.TryGet(run, out AncientAppearanceConfig? config)) return config.Copy();
        // Other mods may generate rooms outside the standard run setup. Preserve their
        // selection in that case, and never substitute a client's local preferences.
        return new();
    }

    internal static void Open()
    {
        ModSettingsOpenResult result = ModSettingsNavigator.RequestOpenByIds(Entry.ModId, Entry.ModId, "ancients", null);
        if (!result.Success) Entry.Logger.Warn($"Unable to open Exusiai settings: {result.Message}");
    }
}
