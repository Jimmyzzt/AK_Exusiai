using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Saves.RawProgress;

namespace AK_Exusiai.Debug;

/// <summary>
/// Explicit, guarded progress import for players who alternate between Vanilla and Modded profiles.
/// Only progress.save is copied; preferences, active runs, and run history remain untouched.
/// </summary>
internal static class ExusiaiSaveConsoleCmd
{
    private const string OwnerId = "AK_Exusiai";
    private const string Confirmation = "confirm";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    internal static CmdResult Process(string[] args)
    {
        if (!UserDataPathProvider.IsRunningModded)
        {
            return Fail("This command is only available while the game is running in Modded mode.");
        }

        if (!SaveManager.Instance.IsProfileInitialized)
            return Fail("The active profile has not finished loading.");

        if (RunManager.Instance.IsInProgress)
        {
            return Fail(
                "Save import is disabled during a run. Return to the main menu before using this command.");
        }

        string operation = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
        return operation switch
        {
            "status" => ShowStatus(),
            "import-vanilla" => ImportVanilla(args.Skip(1).ToArray()),
            "help" => Help(),
            _ => Fail($"Unknown save operation '{args[0]}'.\n{UsageSummary()}"),
        };
    }

    private static CmdResult Help() => new(
        success: true,
        "[gold]Exusiai save commands[/gold]\n" +
        "  exusiai save status\n" +
        "  exusiai save import-vanilla confirm\n" +
        "The import replaces only the current slot's Modded progress.save. " +
        "Preferences, current runs, and run history are not changed.");

    private static CmdResult ShowStatus()
    {
        int profileId = SaveManager.Instance.CurrentProfileId;
        ITargetedRawProgressCommitBridge bridge = RawProgressBridge.TargetedInstance;
        RawProgressReadResult vanilla = Capture(bridge, profileId, RawProgressEnvironment.Vanilla);
        RawProgressReadResult modded = Capture(bridge, profileId, RawProgressEnvironment.Modded);

        return new CmdResult(
            success: vanilla.Outcome == RawProgressReadOutcome.Succeeded &&
                     modded.Outcome == RawProgressReadOutcome.Succeeded,
            $"[gold]Profile slot {profileId} progress[/gold]\n" +
            $"  Vanilla: {Describe(vanilla)}\n" +
            $"  Modded:  {Describe(modded)}\n" +
            "Use 'exusiai save import-vanilla confirm' at the main menu to replace Modded progress.");
    }

    private static CmdResult ImportVanilla(string[] args)
    {
        if (args.Length != 1 || !args[0].Equals(Confirmation, StringComparison.OrdinalIgnoreCase))
        {
            return Fail(
                "This permanently replaces the current slot's Modded progress with its Vanilla progress. " +
                "RitsuLib creates and verifies a recovery backup before changing the destination.\n" +
                "Run 'exusiai save import-vanilla confirm' to continue.");
        }

        int profileId = SaveManager.Instance.CurrentProfileId;
        RawProgressDestination source = CreateDestination(profileId, RawProgressEnvironment.Vanilla);
        RawProgressDestination destination = CreateDestination(profileId, RawProgressEnvironment.Modded);
        ITargetedRawProgressCommitBridge bridge = RawProgressBridge.TargetedInstance;
        TargetedRawProgressBridgeDescriptor descriptor = bridge.DescribeTargeted();

        RawProgressReadResult sourceRead = Capture(bridge, source);
        if (sourceRead is not { Outcome: RawProgressReadOutcome.Succeeded, Snapshot: not null })
            return Fail($"Could not read Vanilla progress for slot {profileId}: {sourceRead.Outcome}.");

        RawProgressReadResult destinationRead = Capture(bridge, destination);
        if (destinationRead is not { Outcome: RawProgressReadOutcome.Succeeded, Snapshot: not null })
            return Fail($"Could not read Modded progress for slot {profileId}: {destinationRead.Outcome}.");

        RawProgressSnapshot sourceSnapshot = sourceRead.Snapshot;
        RawProgressSnapshot destinationSnapshot = destinationRead.Snapshot;
        if (sourceSnapshot.SchemaVersion != destinationSnapshot.SchemaVersion ||
            !descriptor.SupportedSchemas.Contains(sourceSnapshot.SchemaVersion))
        {
            return Fail(
                $"The two saves use incompatible progress schemas " +
                $"({sourceSnapshot.SchemaVersion} vs {destinationSnapshot.SchemaVersion}). Nothing was changed.");
        }

        if (!TryPrepareReplacement(
                sourceSnapshot.RawJson,
                destinationSnapshot.Generation.ProgressUniqueId,
                out string proposedJson,
                out string preparationError))
        {
            return Fail($"Could not prepare the replacement progress: {preparationError}. Nothing was changed.");
        }

        byte[] proposedBytes = Encoding.UTF8.GetBytes(proposedJson);
        string proposedSha256 = Convert.ToHexString(SHA256.HashData(proposedBytes)).ToLowerInvariant();
        RawProgressCommitRequest destinationCommit = new()
        {
            ProtocolVersion = descriptor.BaseProtocolVersion,
            SchemaVersion = sourceSnapshot.SchemaVersion,
            OwnerId = OwnerId,
            TransactionId = Guid.NewGuid(),
            ExpectedGeneration = destinationSnapshot.Generation,
            ProposedRawJson = proposedJson,
            ProposedSha256 = proposedSha256,
            ProposedUtf8Length = proposedBytes.LongLength,
        };
        TargetedRawProgressCommitRequest request = new()
        {
            ProtocolVersion = descriptor.ProtocolVersion,
            Source = source,
            ExpectedSourceGeneration = sourceSnapshot.Generation,
            Destination = destination,
            DestinationCommit = destinationCommit,
        };

        TargetedRawProgressCommitResult result;
        try
        {
            result = bridge.CommitAsync(request).AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            return Fail($"The guarded import failed before completion: {ex.GetType().Name}: {ex.Message}");
        }

        if (result.GuardOutcome != TargetedRawProgressCommitGuardOutcome.Passed || result.CommitResult == null)
        {
            string readFailure = result.ReadFailure is null ? string.Empty : $" Read failure: {result.ReadFailure}.";
            return Fail($"Nothing was changed. Guard result: {result.GuardOutcome}.{readFailure}");
        }

        RawProgressCommitResult commit = result.CommitResult;
        if (commit.Outcome != RawProgressCommitOutcome.CommittedVerified)
        {
            string recovery = commit.RecoveryJournalRetained
                ? " A verified recovery journal was retained; do not repeat the command before checking the log."
                : string.Empty;
            return Fail(
                $"Import was not fully verified: {commit.Outcome}. " +
                $"Destination may have changed: {commit.DestinationMayHaveChanged}.{recovery}");
        }

        return new CmdResult(
            success: true,
            $"Imported Vanilla progress into Modded profile slot {profileId}. " +
            "The write and read-back were verified, the destination identity was preserved, and live progress was refreshed. " +
            "Preferences, active runs, and run history were not changed.");
    }

    private static RawProgressReadResult Capture(
        ITargetedRawProgressCommitBridge bridge,
        int profileId,
        RawProgressEnvironment environment) =>
        Capture(bridge, CreateDestination(profileId, environment));

    private static RawProgressReadResult Capture(
        ITargetedRawProgressCommitBridge bridge,
        RawProgressDestination destination)
    {
        try
        {
            return bridge.CaptureAsync(destination).AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Warn($"[AK_Exusiai] Progress snapshot capture failed: {ex}");
            return new RawProgressReadResult
            {
                Outcome = RawProgressReadOutcome.LocalReadUnavailable,
                Snapshot = null,
            };
        }
    }

    private static RawProgressDestination CreateDestination(
        int profileId,
        RawProgressEnvironment environment) => new()
        {
            ProfileId = profileId,
            Environment = environment,
        };

    private static string Describe(RawProgressReadResult result)
    {
        if (result is not { Outcome: RawProgressReadOutcome.Succeeded, Snapshot: not null })
            return result.Outcome.ToString();

        ProgressGeneration generation = result.Snapshot.Generation;
        DateTime modified = new(generation.LocalLastModifiedUtcTicks, DateTimeKind.Utc);
        string cloud = generation.CloudAvailable
            ? generation.CloudPersisted ? "cloud persisted" : "cloud not persisted"
            : "local only";
        return $"{generation.LocalLength:N0} bytes, {modified.ToLocalTime():yyyy-MM-dd HH:mm:ss}, " +
               $"sha256 {generation.LocalSha256[..12]}, {cloud}";
    }

    private static bool TryPrepareReplacement(
        string sourceJson,
        string destinationUniqueId,
        out string proposedJson,
        out string error)
    {
        proposedJson = string.Empty;
        error = string.Empty;
        try
        {
            JsonNode? root = JsonNode.Parse(
                sourceJson,
                nodeOptions: null,
                documentOptions: new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 128,
                });
            if (root is not JsonObject progress)
            {
                error = "the source document is not a JSON object";
                return false;
            }

            if (!progress.ContainsKey("unique_id"))
            {
                error = "the source document has no unique_id";
                return false;
            }

            progress["unique_id"] = destinationUniqueId;
            proposedJson = progress.ToJsonString(JsonOptions);
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string UsageSummary() =>
        "Usage: exusiai save <status|import-vanilla confirm>";

    private static CmdResult Fail(string message) => new(success: false, message);
}
