using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using System.Text.Json;
using AK_Exusiai.Ancients;
using AK_Exusiai.Settings;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;

int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
var assembly = typeof(AncientAppearanceConfig).Assembly;
var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
var weightsType = assembly.GetType("AK_Exusiai.Settings.AncientAppearanceWeights")!;
var weightsMethod = weightsType.GetMethod("Build", flags, [typeof(int), typeof(int), typeof(bool), typeof(AncientAppearanceRule)])!;
double[] Weights(int n, int index, bool enabled, AncientAppearanceMode mode, double p = 25) =>
    (double[])weightsMethod.Invoke(null, [n, index, enabled, new AncientAppearanceRule { Mode = mode, Percent = p }])!;
foreach (int n in new[] { 2, 3, 4, 5, 10 })
foreach (int target in new[] { 0, n - 1 })
{
    Check(Weights(n, target, true, AncientAppearanceMode.Default).All(w => w == 1), "Default must be equal weight");
    foreach (double p in new[] { 0d, 1, 25, 50, 99, 100 })
    {
        var weights = Weights(n, target, true, AncientAppearanceMode.Custom, p);
        Check(Math.Abs(weights[target] / weights.Sum() - p / 100) < 1e-12, "Actual probability differs from requested percentage");
        Check(p != 100 || weights.Where((_, i) => i != target).All(w => w == 0), "100% must zero every other candidate, including Darv");
    }
    Check(Weights(n, target, true, AncientAppearanceMode.Disabled)[target] == 0, "Disabled must have zero weight");
    Check(Weights(n, target, true, AncientAppearanceMode.Guaranteed)[target] == 1, "Guaranteed must retain target");
    Check(Weights(n, target, false, AncientAppearanceMode.Guaranteed)[target] == 0, "Master toggle must override Guaranteed");
}
Check(new AncientAppearanceRule { Percent = double.NaN, Mode = (AncientAppearanceMode)99 }.Copy().Mode == AncientAppearanceMode.Default, "Unknown mode must normalize");
Check(new AncientAppearanceRule { Percent = -1 }.Copy().Percent == 0 && new AncientAppearanceRule { Percent = 101 }.Copy().Percent == 100, "Clamp persisted percentages");
var local = new AncientAppearanceConfig { Laterano = new() { Mode = AncientAppearanceMode.Custom, Percent = 72 } };
var snapshot = local.Copy(); local.Laterano.Percent = 3;
Check(snapshot.Laterano.Percent == 72, "Run snapshot must not share mutable local rules");
Check(JsonSerializer.Deserialize<AncientAppearanceConfig>(JsonSerializer.Serialize(snapshot))!.Laterano.Percent == 72, "Configuration must round-trip");

T Raw<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
var choose = assembly.GetType("AK_Exusiai.Patches.AncientSelectionPatch")!.GetMethod("ChooseFromPool", flags)!;
AncientEventModel? Choose(Rng rng, AncientEventModel[] pool, AncientAppearanceConfig config) => (AncientEventModel?)choose.Invoke(null, [rng, pool, config]);
foreach (bool penguin in new[] { false, true })
{
    AncientEventModel custom = penguin ? Raw<PenguinLogistics>() : Raw<Laterano>();
    AncientEventModel[] pool = [custom, Raw<Darv>(), Raw<Orobas>(), Raw<Pael>()];
    var rng = new Rng(123u); var referenceRng = new Rng(123u);
    for (int i = 0; i < 100; i++) Check(Choose(rng, pool, new()) == referenceRng.NextItem(pool), "Default must preserve seeded vanilla rolls");
    foreach (double p in new[] { 0d, 1, 50, 99, 100 })
    {
        var config = new AncientAppearanceConfig();
        var rule = penguin ? config.PenguinLogistics : config.Laterano;
        rule.Mode = AncientAppearanceMode.Custom; rule.Percent = p;
        int selected = 0;
        for (int i = 0; i < 20000; i++) if (Choose(rng, pool, config) == custom) selected++;
        Check(p != 0 || selected == 0, "0% must never be selected");
        Check(p != 100 || selected == 20000, "100% must always be selected");
        Check(Math.Abs(selected / 20000d - p / 100) < .02, "Seeded weighted distribution out of tolerance");
    }
}

// Use the real RitsuLib payload codec to simulate host -> client and save -> reload.
var slot = RunSavedDataStore.For("AncientSettingsTests").Register<AncientAppearanceConfig>("ancients", () => new());
var hostRun = Raw<RunState>(); var clientRun = Raw<RunState>();
slot.Set(hostRun, snapshot);
var registry = typeof(RunSavedDataStore).Assembly.GetType("STS2RitsuLib.RunData.RunSavedDataRegistry")!;
string payload = (string)registry.GetMethod("BuildPayload", flags)!.Invoke(null, [hostRun])!;
registry.GetMethod("ImportPayloadIntoRun", flags)!.Invoke(null, [clientRun, payload]);
Check(slot.TryGet(clientRun, out var clientConfig) && clientConfig.Laterano.Percent == 72, "Client must receive host's run snapshot");
var loadedRun = Raw<RunState>();
registry.GetMethod("ImportPayloadIntoRun", flags)!.Invoke(null, [loadedRun, payload]);
Check(slot.Get(loadedRun).Laterano.Percent == 72, "Reload must preserve host configuration");

// Exercise the framework's real shared-slot authority check: a client contribution
// must not overwrite the session's host-owned run settings.
var runtimeAssembly = typeof(RunSavedDataStore).Assembly;
var realSlot = slot.GetType().GetField("_slot", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(slot)!;
var session = Activator.CreateInstance(runtimeAssembly.GetType("STS2RitsuLib.RunData.RunSavedDataLobbySession")!, true)!;
var document = runtimeAssembly.GetType("STS2RitsuLib.RunData.RunSavedDataDocument")!.GetMethod("FromJson", flags)!.Invoke(null, [payload])!;
object?[] rawArgs = ["AncientSettingsTests", "ancients", null];
document.GetType().GetMethod("TryGetRaw")!.Invoke(document, rawArgs);
var importContribution = realSlot.GetType().GetMethod("ImportLobbyContribution")!;
importContribution.Invoke(realSlot, [session, 11UL, false, rawArgs[2]]);
var rejectedClientRun = Raw<RunState>();
realSlot.GetType().GetMethod("CommitLobbyStaging")!.Invoke(realSlot, [session, rejectedClientRun]);
Check(!slot.TryGet(rejectedClientRun, out _), "Client contribution must not populate the shared slot");
importContribution.Invoke(realSlot, [session, 1UL, true, rawArgs[2]]);
var authoritativeRun = Raw<RunState>();
realSlot.GetType().GetMethod("CommitLobbyStaging")!.Invoke(realSlot, [session, authoritativeRun]);
Check(slot.Get(authoritativeRun).Laterano.Percent == 72, "Host contribution must populate the shared slot");

var transpiler = assembly.GetType("AK_Exusiai.Patches.AncientSelectionPatch")!.GetMethod("Transpiler", flags)!;
var next = typeof(Rng).GetMethods().Single(m => m.Name == "NextItem").MakeGenericMethod(typeof(AncientEventModel));
var rewritten = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, [new[] { new CodeInstruction(OpCodes.Callvirt, next) }])!).ToArray();
Check(((MethodInfo)rewritten.Single().operand).Name == "Choose", "Transpiler must replace only the Ancient selection call");
var rewrite = assembly.GetType("AK_Exusiai.Patches.AncientSelectionPatch")!.GetMethod("Rewrite", flags)!;
var unsupported = ((IEnumerable<CodeInstruction>)rewrite.Invoke(null, [new[] { new CodeInstruction(OpCodes.Nop) }, null])!).ToArray();
Check(unsupported.Length == 1 && unsupported[0].opcode == OpCodes.Nop, "Another mod's unsupported selection must remain unchanged");

// Unequal weights stand in for third-party Ancients and modified vanilla weights.
var weightedChoose = assembly.GetType("AK_Exusiai.Patches.AncientSelectionPatch")!.GetMethod("ChooseWeightedFromPool", flags)!;
AncientEventModel[] weightedPool = [Raw<Laterano>(), Raw<Darv>(), Raw<Orobas>(), Raw<Pael>()];
float[] baseWeights = [3, 0, 7, 2];
Func<AncientEventModel?, float> fetchWeight = c => baseWeights[Array.IndexOf(weightedPool, c!)];
var weightedRng = new Rng(987u); var originalWeightedRng = new Rng(987u);
for (int i = 0; i < 300; i++)
{
    var actual = weightedChoose.Invoke(null, [weightedRng, weightedPool, new AncientAppearanceConfig(), fetchWeight]);
    Check(ReferenceEquals(actual, originalWeightedRng.WeightedNextItem(weightedPool, fetchWeight)), "Default must preserve other mods' exact weighted draws");
}
var buildWeighted = weightsType.GetMethod("Build", flags, [typeof(double[]), typeof(int), typeof(bool), typeof(AncientAppearanceRule)])!;
var adjusted = (double[])buildWeighted.Invoke(null, [baseWeights.Select(w => (double)w).ToArray(), 0, true,
    new AncientAppearanceRule { Mode = AncientAppearanceMode.Custom, Percent = 60 }])!;
Check(Math.Abs(adjusted[0] / adjusted.Sum() - .6) < 1e-12, "Probability conversion must use real weights");
Check(adjusted[1] == 0 && adjusted[2] / adjusted[3] == 3.5, "Other weights, zero exclusions and relative odds must survive");
var weightedNext = typeof(Rng).GetMethods().Single(m => m.Name == "WeightedNextItem" && !m.IsStatic).MakeGenericMethod(typeof(AncientEventModel));
var weightedRewrite = ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, [new[] { new CodeInstruction(OpCodes.Callvirt, weightedNext) }])!).ToArray();
Check(((MethodInfo)weightedRewrite[0].operand).Name == "ChooseWeighted", "Weighted selection patches must retain the weighting delegate");
var mixedPool = new AncientEventModel[] { Raw<Laterano>(), Raw<PenguinLogistics>(), Raw<Orobas>() };
var masterOff = new AncientAppearanceConfig { Enabled = false };
for (int i = 0; i < 30; i++) Check(Choose(weightedRng, mixedPool, masterOff) == mixedPool[2], "Master toggle excludes both Ancients even in another mod's mixed pool");
Console.WriteLine($"PASS: {checks} checks covering weights, seeded selection, both Ancients, host payload, reload, and patch target validation.");
