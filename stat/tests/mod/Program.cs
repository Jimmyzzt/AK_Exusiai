using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Telemetry;

const string Card = "AK_EXUSIAI_CARD_CHARGING_MODE";
var me = new SerializablePlayer { NetId = 123456789, CharacterId = new ModelId("CHARACTER", "AK_EXUSIAI_CHARACTER_EXUSIAI") };
var card = new SerializableCard { Id = new ModelId("CARD", Card), FloorAddedToDeck = 1 };
me.Deck.Add(card);
var p = new PlayerMapPointHistoryEntry { PlayerId = me.NetId };
p.CardChoices.Add(new CardChoiceHistoryEntry { Card = card, wasPicked = true });
p.CardChoices.Add(new CardChoiceHistoryEntry { Card = card, wasPicked = true });
p.CardsGained.Add(card); p.CardsGained.Add(card);
p.CardsRemoved.Add(card); p.UpgradedCards.Add(card.Id);
const string Ancient = "AK_EXUSIAI_RELIC_PHOTO_WITH_THE_LORD";
p.AncientChoices.Add(new AncientChoiceHistoryEntry(new LocString("relics", Ancient + ".title"), true));
p.BoughtRelics.Add(new ModelId("RELIC", Ancient)); // A duplicate source at the same floor must not double acquisition counts.
var teammate = new PlayerMapPointHistoryEntry { PlayerId = 987654321 };
teammate.CardChoices.Add(new CardChoiceHistoryEntry { Card = card, wasPicked = true });
var run = new SerializableRun
{
    StartTime = 1000000, Players = [me, new SerializablePlayer { NetId = teammate.PlayerId }],
    MapPointHistory = [[new MapPointHistoryEntry { PlayerStats = [p, teammate] }]],
    GameMode = GameMode.Standard, RunTime = 1800,
    SerializableRng = new SerializableRunRngSet { Seed = "private-run-seed" },
};
var type = typeof(AK_Exusiai.Entry).Assembly.GetType("AK_Exusiai.Statistics.RunStatistics")!;
var build = type.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Static)!;
JsonObject Capture(string token, bool abandon = false) => (JsonObject)build.Invoke(null, [new RunEndedEvent(run, true, abandon, DateTimeOffset.Parse("2026-10-06T12:00:00Z")), me, token, "v0.111.0"])!;
void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
var result = Capture(new string('1', 64));
var act = result["entities"]!.AsArray().Single(e => e!["act"]!.GetValue<int>() == 1)!;
var overall = result["entities"]!.AsArray().Single(e => e!["act"]!.GetValue<int>() == 0 && e["id"]!.GetValue<string>() == Card)!;
var ancient = result["entities"]!.AsArray().Single(e => e!["id"]!.GetValue<string>() == Ancient)!;
Assert(ancient["offered"]!.GetValue<int>() == 1 && ancient["picked"]!.GetValue<int>() == 1 && ancient["obtained"]!.GetValue<int>() == 1 && ancient["floor_sum"]!.GetValue<int>() == 1, "Ancient choice key and deduplicated historical relic acquisition");
Assert(act["offered"]!.GetValue<int>() == 2 && act["picked"]!.GetValue<int>() == 2, "Teammate events must not enter local player's statistics");
Assert(overall["owned"]!.GetValue<int>() == 1 && overall["obtained"]!.GetValue<int>() == 2 && overall["removed"]!.GetValue<int>() == 1 && overall["upgraded"]!.GetValue<int>() == 1, "Card history counts");
Assert(result["id"]!.GetValue<string>() == Capture(new string('1', 64))["id"]!.GetValue<string>(), "Run ID must survive replay");
Assert(result["id"]!.GetValue<string>() != Capture(new string('2', 64))["id"]!.GetValue<string>(), "Installations must have independent pseudonyms");
Assert(!Capture(new string('1', 64), true)["victory"]!.GetValue<bool>(), "Abandon must never count as victory");
result.Remove("_owner_token");
var json = result.ToJsonString();
foreach (var secret in new[] { "123456789", "987654321", "1000000", "private-run-seed", "net_id", "player_id", "rng", "seed", "session_id", "anonymous_install_id" }) Assert(!json.Contains(secret, StringComparison.OrdinalIgnoreCase), "Raw identity/seed escaped sanitizer: " + secret);
Console.WriteLine("PASS: local-only history, card counts, deterministic private run ID, abandon outcome, identity allowlist.");

Assert(result["schema"]!.GetValue<string>() == "exusiai.run.v2", "New uploads must use detailed schema");
var detail = result["details"]!;
Assert(detail["offers"]!.AsArray().Count == 2 && detail["offers"]![0]!["position"]!.GetValue<int>() == 1, "Offer floors and local-only selection details");
Assert(detail["items"]!.AsArray().Single(i => i!["id"]!.GetValue<string>() == Ancient)!["obtained"]!.GetValue<int>() == 1, "Detailed relic sources must deduplicate too");
var originalHistory = run.MapPointHistory;
var upgraded = new SerializableCard { Id = card.Id, CurrentUpgradeLevel = 1 };
me.Deck.Add(upgraded);
run.MapPointHistory = Enumerable.Range(1, 4).Select(a => new List<MapPointHistoryEntry>
{
    new() { PlayerStats = [new PlayerMapPointHistoryEntry { PlayerId = me.NetId, CurrentHp = a < 4 ? 30 : 0, DamageTaken = 8 }],
        Rooms = [new MapPointRoomHistoryEntry { RoomType = a < 4 ? MegaCrit.Sts2.Core.Rooms.RoomType.Boss : MegaCrit.Sts2.Core.Rooms.RoomType.Monster, TurnsTaken = 3 }] }
}).ToList();
var laterLoss = (JsonObject)build.Invoke(null, [new RunEndedEvent(run, false, true, DateTimeOffset.UtcNow), me, new string('1',64), "v0.111.0"])!;
Assert(!laterLoss["victory"]!.GetValue<bool>() && laterLoss["details"]!["win3"]!.GetValue<bool>(), "A later-act loss must preserve third-act completion");
Assert(!laterLoss["details"]!["acts"]![2]!["snapshot_known"]!.GetValue<bool>(), "Missing third-act boundary deck must stay unknown");
Assert(laterLoss["details"]!["acts"]![3]!["deck"]!.AsArray().Count == 2, "Upgraded and base ownership variants remain distinct");
Assert(laterLoss["details"]!["fights"]!.AsArray().Count == 4 && laterLoss["details"]!["fights"]![0]!["damage"]!.GetValue<int>() == 8, "Combat summaries must come from history");
run.MapPointHistory = originalHistory; me.Deck.Remove(upgraded);
Console.WriteLine("PASS: v2 offer details, relic deduplication, third-act wins, missing boundary coverage, upgrade variants and combat summaries.");

var adapterType = typeof(AK_Exusiai.Entry).Assembly.GetType("AK_Exusiai.Statistics.ExusiaiUploadAdapter")!;
bool allowed = true;
var messages = new List<string>();
var handler = new RecordingHandler();
using var http = new HttpClient(handler);
var adapter = (ITelemetryAdapter)Activator.CreateInstance(adapterType, [http,
    (Func<string>)(() => new string('1', 64)), (Func<bool>)(() => allowed),
    (Action<string>)(m => messages.Add(m)), (Action<string>)(m => messages.Add(m))])!;
var applicant = new TelemetryApplicant { ApplicantId = "AK_Exusiai", OwnerModId = "AK_Exusiai", DisplayName = "Test", Adapter = adapter };
TelemetryEnvelope Event(string eventName = "exusiai.run.v1", char owner = '1') => new()
{
    ApplicantId = "AK_Exusiai", RequestId = "run_history", Category = TelemetryDataCategory.RunHistory,
    EventName = eventName,
    Payload = new JsonObject
    {
        ["anonymous_install_id"] = "must-not-upload", ["raw_run"] = "private-envelope",
        ["applicant_payload"] = Capture(new string(owner, 64)),
    },
};
var queued = new[] { Event(), Event() };
handler.Reply = () => handler.Bodies.Count == 2 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : new HttpResponseMessage(HttpStatusCode.OK);
Assert(!(await adapter.SendAsync(applicant, queued)).Success, "Partial batch must remain queued on outage");
Assert(handler.Bodies.Count == 2, "One run per HTTP request");
handler.Reply = () => new HttpResponseMessage(HttpStatusCode.OK);
Assert((await adapter.SendAsync(applicant, queued)).Success && handler.Bodies.Count == 4, "Retry must resubmit until the whole batch is confirmed");
foreach (var body in handler.Bodies)
{
    foreach (var secret in new[] { "_owner_token", "anonymous_install_id", "raw_run", "private-envelope", new string('1', 64) })
        Assert(!body.Contains(secret), "Envelope or credential escaped upload adapter");
}
Assert(queued[0].Payload!["applicant_payload"]!["_owner_token"] != null, "Sending must not mutate queued records");
Assert(handler.Authorization.All(h => h == "Bearer " + new string('1', 64)), "Upload credential belongs only in Authorization header");
allowed = false;
Assert(!(await adapter.SendAsync(applicant, queued)).Success && handler.Bodies.Count == 4, "Disabled consent must not send");
allowed = true;
Assert((await adapter.SendAsync(applicant, [Event("run_history"), Event(owner: '2')])).Success && handler.Bodies.Count == 4, "Reject automatic raw events and old identity queues");
handler.Reply = () => { allowed = false; return new HttpResponseMessage(HttpStatusCode.OK); };
Assert(!(await adapter.SendAsync(applicant, queued)).Success && handler.Bodies.Count == 5, "Consent revoked mid-batch stops the next send");
allowed = true;
foreach (var code in new[] { HttpStatusCode.UnprocessableEntity })
{
    handler.Reply = () => new HttpResponseMessage(code);
    Assert((await adapter.SendAsync(applicant, [Event()])).Success, "Rejected events must not permanently block subsequent runs");
}
handler.Reply = () => new HttpResponseMessage(HttpStatusCode.TooManyRequests);
Assert(!(await adapter.SendAsync(applicant, [Event()])).Success, "Rate-limited runs must remain queued");
handler.Reply = () => throw new HttpRequestException("offline");
Assert(!(await adapter.SendAsync(applicant, [Event()])).Success, "Network failure must remain queued");
foreach (var message in messages) Assert(!message.Contains(new string('1', 64)), "Logs must not disclose credentials");
Console.WriteLine("PASS: upload authorization, envelope privacy, partial-batch retry, HTTP limits and offline recovery.");

string translationFolder = Path.GetFullPath("AK_Exusiai/localization/statistics");
var chinese = JsonNode.Parse(File.ReadAllText(Path.Combine(translationFolder, "zhs.json")))!.AsObject();
var english = JsonNode.Parse(File.ReadAllText(Path.Combine(translationFolder, "eng.json")))!.AsObject();
Assert(chinese.Select(k => k.Key).Order().SequenceEqual(english.Select(k => k.Key).Order()), "Translation keys must match");
foreach (var table in new[] { chinese, english }) foreach (var (key, value) in table)
    Assert(!string.IsNullOrWhiteSpace(value?.GetValue<string>()), "Missing translation: " + key);
Assert(chinese.Count == 5, "Only settings title, consent description, upload toggle and website text belong in localization");
Console.WriteLine("PASS: Chinese/English statistics localization keys and values match.");

sealed class RecordingHandler : HttpMessageHandler
{
    public List<string> Bodies { get; } = [];
    public List<string?> Authorization { get; } = [];
    public Func<HttpResponseMessage> Reply { get; set; } = () => new HttpResponseMessage(HttpStatusCode.OK);
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri?.ToString() != "https://exusiai.zzt.si/api/upload") throw new Exception("Unexpected destination");
        Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
        Authorization.Add(request.Headers.Authorization?.ToString());
        return Reply();
    }
}
