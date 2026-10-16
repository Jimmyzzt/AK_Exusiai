using System.Reflection;
using System.Reflection.Emit;
using AK_Exusiai.Ancients;
using AK_Exusiai.Settings;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class AncientSelectionPatch : IPatchMethod
{
    public static string PatchId => "configured-ancient-selection";
    public static string Description => "Apply explicit host Ancient preferences while preserving default selection";
    public static ModPatchTarget[] GetTargets() => [PatchTarget.Method<ActModel>(nameof(ActModel.GenerateRooms))];

    [HarmonyPriority(Priority.Last)]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        => Rewrite(instructions, message => Entry.Logger.Warn(message));

    internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions, Action<string>? warn)
    {
        var code = instructions.ToList();
        var sites = new List<(CodeInstruction Code, string Replacement)>();
        foreach (CodeInstruction instruction in code)
        {
            if (instruction.operand is not MethodInfo method || method.DeclaringType != typeof(Rng) ||
                !method.IsGenericMethod || !method.GetGenericArguments().SequenceEqual([typeof(AncientEventModel)])) continue;
            string? replacement = method.Name switch
            {
                nameof(Rng.NextItem) => nameof(Choose),
                nameof(Rng.WeightedNextItem) when !method.IsStatic => nameof(ChooseWeighted),
                nameof(Rng.WeightedNextItem) when method.GetParameters().Length == 4 => nameof(ChooseWeightedStatic),
                _ => null,
            };
            if (replacement is not null) sites.Add((instruction, replacement));
        }
        // A mod may completely replace this selection implementation. Do not prevent
        // it from loading or guess which of several Ancient rolls is the final one.
        if (sites.Count != 1)
        {
            warn?.Invoke($"Ancient settings: expected 1 supported selection call, found {sites.Count}; preserving the existing method.");
            return code;
        }
        sites[0].Code.opcode = OpCodes.Call;
        sites[0].Code.operand = AccessTools.Method(typeof(AncientSelectionPatch), sites[0].Replacement);
        return code;
    }

    private static AncientAppearanceConfig CurrentConfig() =>
        RunManager.Instance.DebugOnlyGetState() is { } run ? ExusiaiModSettings.ForRun(run) : new();

    internal static AncientEventModel? Choose(Rng rng, IEnumerable<AncientEventModel> candidates)
        => ChooseFromPool(rng, candidates.ToArray(), CurrentConfig());

    internal static AncientEventModel? ChooseFromPool(Rng rng, AncientEventModel[] pool, AncientAppearanceConfig config)
    {
        if (!HasOverride(pool, config)) return rng.NextItem(pool);
        return Select(pool, Weights(pool, config, _ => 1), rng.NextDouble());
    }

    internal static AncientEventModel? ChooseWeighted(Rng rng, IEnumerable<AncientEventModel> candidates,
        Func<AncientEventModel?, float> weightFetcher)
        => ChooseWeightedFromPool(rng, candidates.ToArray(), CurrentConfig(), weightFetcher);

    internal static AncientEventModel? ChooseWeightedFromPool(Rng rng, AncientEventModel[] pool,
        AncientAppearanceConfig config, Func<AncientEventModel?, float> weightFetcher)
    {
        // Default calls the existing implementation unchanged, without evaluating
        // weights an extra time or advancing the random stream.
        if (!HasOverride(pool, config)) return rng.WeightedNextItem(pool, weightFetcher);
        return Select(pool, Weights(pool, config, weightFetcher), rng.NextDouble());
    }

    internal static AncientEventModel? ChooseWeightedStatic(float roll, IEnumerable<AncientEventModel> candidates,
        Func<AncientEventModel, float> weightFetcher, AncientEventModel fallback)
    {
        var pool = candidates.ToArray();
        AncientAppearanceConfig config = CurrentConfig();
        if (!HasOverride(pool, config)) return Rng.WeightedNextItem(roll, pool, weightFetcher, fallback);
        return Select(pool, Weights(pool, config, c => weightFetcher(c!)), roll);
    }

    private static AncientAppearanceRule? Rule(AncientEventModel ancient, AncientAppearanceConfig config) => ancient switch
    {
        Laterano => config.Laterano,
        PenguinLogistics => config.PenguinLogistics,
        _ => null,
    };

    private static bool HasOverride(AncientEventModel[] pool, AncientAppearanceConfig config) =>
        pool.Any(ancient => Rule(ancient, config) is { } rule &&
            (!config.Enabled || rule.Copy().Mode != AncientAppearanceMode.Default));

    private static double[] Weights(AncientEventModel[] pool, AncientAppearanceConfig config,
        Func<AncientEventModel?, float> weightFetcher)
    {
        double[] original = pool.Select(ancient => (double)weightFetcher(ancient)).ToArray();
        var requested = new Dictionary<int, double>();
        for (int i = 0; i < pool.Length; i++)
        {
            AncientAppearanceRule? rule = Rule(pool[i], config)?.Copy();
            if (rule is null) continue;
            if (!config.Enabled || rule.Mode == AncientAppearanceMode.Disabled) requested[i] = 0;
            else if (rule.Mode == AncientAppearanceMode.Guaranteed) requested[i] = 1;
            else if (rule.Mode == AncientAppearanceMode.Custom) requested[i] = rule.Percent / 100;
        }
        return AncientAppearanceWeights.BuildOverrides(original, requested);
    }

    private static AncientEventModel? Select(AncientEventModel[] pool, double[] weights, double roll)
    {
        roll *= weights.Sum();
        for (int i = 0; i < pool.Length; i++)
        {
            if (weights[i] <= 0) continue;
            if (roll < weights[i]) return pool[i];
            roll -= weights[i];
        }
        return pool.Where((_, i) => weights[i] > 0).LastOrDefault();
    }
}
