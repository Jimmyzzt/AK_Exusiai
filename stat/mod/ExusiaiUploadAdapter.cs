using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry;

namespace AK_Exusiai.Statistics;

/// <summary>Send only sanitized run DTOs; the RitsuLib envelope stays on the machine.</summary>
internal sealed class ExusiaiUploadAdapter(HttpClient http, Func<string> identity,
    Func<bool> authorized, Action<string> warn, Action<string> info) : ITelemetryAdapter
{
    private readonly SemaphoreSlim _sending = new(1, 1);
    public string AdapterId => "exusiai_d1_v1";
    public string EndpointDescription => ExusiaiTelemetry.Endpoint + "/api/upload";

    public async ValueTask<TelemetrySendResult> SendAsync(TelemetryApplicant applicant,
        IReadOnlyList<TelemetryEnvelope> events, CancellationToken cancellationToken = default)
    {
        await _sending.WaitAsync(cancellationToken);
        try
        {
            int confirmed = 0;
            foreach (var envelope in events)
            {
                if (!authorized()) return TelemetrySendResult.Fail("Statistics consent disabled.");
                if (envelope.EventName != "exusiai.run.v1" || envelope.Payload?["applicant_payload"] is not JsonObject source) continue;
                string? credential = source["_owner_token"]?.GetValue<string>();
                if (credential != identity()) continue; // Never migrate a previous identity's queued events.
                var body = (JsonObject)source.DeepClone();
                body.Remove("_owner_token");
                using var request = new HttpRequestMessage(HttpMethod.Post, EndpointDescription);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
                request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
                using var response = await http.SendAsync(request, cancellationToken);
                if ((int)response.StatusCode is 410 or 422)
                {
                    warn($"Statistics event discarded (HTTP {(int)response.StatusCode}): identity revoked or schema rejected.");
                    continue;
                }
                if (!response.IsSuccessStatusCode) return TelemetrySendResult.Fail($"Statistics service returned HTTP {(int)response.StatusCode}; retained for retry.");
                confirmed++;
            }
            if (confirmed > 0) info($"Statistics server confirmed {confirmed} run event(s).");
            return TelemetrySendResult.Ok();
        }
        catch (Exception) { return TelemetrySendResult.Fail("Statistics connection failed; retained for retry."); }
        finally { _sending.Release(); }
    }
}
