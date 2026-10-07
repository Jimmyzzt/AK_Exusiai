using System.Net.Http;
using System.Net.Http.Headers;
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
    private static string _status = "统计身份由本机保存。重新生成前须先在网站确认删除旧身份。";

    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _identityPath = ProjectSettings.GlobalizePath("user://exusiai-stat-identity.txt");
        RitsuLibFramework.RegisterTelemetryApplicant(new TelemetryApplicant
        {
            ApplicantId = Entry.ModId, OwnerModId = Entry.ModId,
            DisplayName = "能天使社区统计 / Exusiai community statistics",
            Adapter = new ExusiaiUploadAdapter(Http, () => Token,
                () => RitsuLibFramework.GetTelemetryClient(Entry.ModId).IsEnabled("run_history"),
                message => Entry.Logger.Warn(message), message => Entry.Logger.Info(message)),
            Requests = [new TelemetryRequest
            {
                RequestId = "run_history", Category = TelemetryDataCategory.RunHistory,
                Description = "仅上传今后本机能天使对局的脱敏统计：选牌、卡组、遗物、幕数、结果、版本与启用Mod。含放弃和联机局；不上传Steam身份、种子、日志或队友明细。仅公开聚合统计，暂无保存期限。隐私与删除：https://exusiai.zzt.si/?page=privacy",
                CaptureFilter = context => context.EventName == EventName,
            }]
        });
        _started = RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(e => _localId = LocalContext.GetMe(e.RunState)?.NetId, false);
        _loaded = RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(e => _localId = LocalContext.GetMe(e.RunState)?.NetId, false);
        _ended = RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(Capture, false);
        RitsuLibFramework.RegisterModSettings(Entry.ModId, page => page.AddSection("statistics", section =>
        {
            section.AddParagraph("privacy", ModSettingsText.Literal("统计上传由 RitsuLib 的遥测授权控制，可随时关闭。关闭不删除已上传数据；删除请使用下方入口。"));
            section.AddParagraph("status", ModSettingsText.Literal(_status));
            section.AddButton("website", ModSettingsText.Literal("能天使数据统计"), ModSettingsText.Literal("打开网站"), () => OS.ShellOpen(Endpoint));
            section.AddButton("delete", ModSettingsText.Literal("删除本机已上传数据"), ModSettingsText.Literal("关闭上传并打开删除页面"), () =>
            {
                RitsuLibFramework.SetTelemetryApplicantConsent(Entry.ModId, TelemetryConsentState.Denied);
                OS.ShellOpen(Endpoint + "/?page=privacy#delete=" + Token);
            });
            section.AddButton("reset", ModSettingsText.Literal("删除后重新参与统计"), ModSettingsText.Literal("验证删除并生成新身份"), (IModSettingsUiActionHost host) => ResetIdentity(host));
        }), "能天使 / Exusiai");
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
                        throw new InvalidOperationException("Invalid statistics identity; reset it in Mod settings.");
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

    private static async void ResetIdentity(IModSettingsUiActionHost host)
    {
        RitsuLibFramework.SetTelemetryApplicantConsent(Entry.ModId, TelemetryConsentState.Denied);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint + "/api/identity");
            string oldToken = Token;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oldToken);
            using var response = await Http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            if (result?["revoked"]?.GetValue<bool>() != true) _status = "请先打开删除页面并完成确认；旧身份仍已保留，上传已关闭。";
            else
            {
                lock (IdentityLock)
                {
                    if (_token != oldToken) return;
                    string newToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
                    File.WriteAllText(_identityPath, newToken);
                    _token = newToken;
                }
                _status = "已生成新统计身份。请在 RitsuLib 遥测设置中重新授权，之后的新局才会上传。";
            }
        }
        catch { _status = "未能验证删除状态，旧凭证已保留。请稍后重试；上传仍已关闭。"; }
        host.RequestRefresh();
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
