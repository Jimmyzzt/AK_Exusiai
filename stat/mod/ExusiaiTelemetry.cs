using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
using STS2RitsuLib.Utils;
using RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace AK_Exusiai.Statistics;

/// <summary>Only our explicitly sanitized event may enter the RitsuLib queue.</summary>
internal static class ExusiaiTelemetry
{
    internal const string Endpoint = "https://exusiai.zzt.si";
    internal const string Revision = "cards-v1.3-stat-v1";
    private const string EventName = "exusiai.run.v1";
    private static ulong? _localId;
    private static string? _token;
    private static string _identityPath = "";
    private static readonly object IdentityLock = new();
    private static IDisposable? _started, _loaded, _ended;
    private static readonly System.Net.Http.HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static System.Threading.Timer? _retry;
    private static bool _initialized;
    private static I18N? _localization;

    private static ModSettingsText Text(string key) => ModSettingsText.I18N(_localization!, key, key);

    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _identityPath = ProjectSettings.GlobalizePath("user://exusiai-stat-identity.txt");
        _localization = new I18N("Exusiai-Statistics", pckFolders: [$"{Entry.ResPath}/localization/statistics"]);
        RitsuLibFramework.RegisterTelemetryApplicant(new TelemetryApplicant
        {
            ApplicantId = Entry.ModId, OwnerModId = Entry.ModId,
            DisplayName = Entry.ModId, DisplayNameText = Text("title"),
            Adapter = new ExusiaiUploadAdapter(Http, () => Token,
                () => RitsuLibFramework.GetTelemetryClient(Entry.ModId).IsEnabled("run_history"),
                message => Entry.Logger.Warn(message), message => Entry.Logger.Info(message)),
            Requests = [new TelemetryRequest
            {
                RequestId = "run_history", Category = TelemetryDataCategory.RunHistory,
                Description = "consent", DescriptionText = Text("consent"),
                CaptureFilter = context => context.EventName == EventName,
            }]
        });
        _started = RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(e => _localId = LocalContext.GetMe(e.RunState)?.NetId, false);
        _loaded = RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(e => _localId = LocalContext.GetMe(e.RunState)?.NetId, false);
        _ended = RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(Capture, false);
        var uploadConsent = ModSettingsBindings.Callback(Entry.ModId, "statistics.upload",
            () => RitsuLibFramework.GetTelemetryClient(Entry.ModId).IsEnabled("run_history"),
            allowed => RitsuLibFramework.SetTelemetryApplicantConsent(Entry.ModId,
                allowed ? TelemetryConsentState.Granted : TelemetryConsentState.Denied,
                allowed ? ["run_history"] : []), () => { });
        RitsuLibFramework.RegisterModSettings(Entry.ModId, page =>
        {
            page.WithTitle(Text("title"));
            page.AddSection("statistics", section =>
            {
                section.AddToggle("upload", Text("allow_upload"), uploadConsent, description: Text("consent"));
                section.AddButton("website", Text("website"), Text("open_website"), () => OS.ShellOpen(Endpoint));
            });
        }, Entry.ModId);
        // The framework's queue survives restarts; explicit periodic flush covers transient outages.
        _retry = new System.Threading.Timer(_ => Retry(), null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(15));
        Entry.Logger.Info("Community statistics registered; RitsuLib consent is required for new local Exusiai runs.");
    }

    internal static string Token
    {
        get
        {
            lock (IdentityLock)
            {
                if (_token != null) return _token;
                if (File.Exists(_identityPath))
                {
                    string value = File.ReadAllText(_identityPath).Trim();
                    if (value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
                        throw new InvalidOperationException("Invalid local statistics credential.");
                    return _token = value;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(_identityPath)!);
                string newToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
                File.WriteAllText(_identityPath, newToken);
                return _token = newToken;
            }
        }
    }

    private static async void Retry()
    {
        try { if (RitsuLibFramework.GetTelemetryClient(Entry.ModId).IsEnabled("run_history")) await RitsuLibFramework.FlushTelemetryAsync(); }
        catch { Entry.Logger.Warn("Statistics retry deferred."); }
    }

    private static void Capture(RunEndedEvent evt)
    {
        try
        {
            var client = RitsuLibFramework.GetTelemetryClient(Entry.ModId);
            if (!client.IsEnabled("run_history")) return;
            ulong? id = _localId ?? LocalContext.NetId;
            var me = evt.Run.Players.FirstOrDefault(p => p.NetId == id);
            if (me?.CharacterId?.Entry != ModelDb.GetId<Characters.Exusiai>().Entry) return;
            JsonObject payload = RunStatistics.Build(evt, me, Token, ReleaseInfoManager.Instance.ReleaseInfo?.Version ?? "unknown");
            client.CapturePayload(EventName, "run_history", payload, new Dictionary<string, object?> { ["capture_source"] = "exusiai_sanitized" });
        }
        catch { Entry.Logger.Warn("Statistics capture skipped; gameplay is unaffected."); }
    }

}
