namespace AK_Exusiai.Settings;

public enum AncientAppearanceMode { Default, Disabled, Guaranteed, Custom }

public sealed class AncientAppearanceRule
{
    public AncientAppearanceMode Mode { get; set; }
    public double Percent { get; set; } = 25;

    public AncientAppearanceRule Copy() => new()
    {
        Mode = Enum.IsDefined(Mode) ? Mode : AncientAppearanceMode.Default,
        Percent = double.IsFinite(Percent) ? Math.Clamp(Percent, 0, 100) : 25,
    };
}

public sealed class AncientAppearanceConfig
{
    public bool Enabled { get; set; } = true;
    public AncientAppearanceRule Laterano { get; set; } = new();
    public AncientAppearanceRule PenguinLogistics { get; set; } = new();

    public AncientAppearanceConfig Copy() => new()
    {
        Enabled = Enabled,
        Laterano = (Laterano ?? new()).Copy(),
        PenguinLogistics = (PenguinLogistics ?? new()).Copy(),
    };
}

internal static class AncientAppearanceWeights
{
    // The supplied pool has already passed the game's unlock and shared-Ancient rules.
    // Preserve supplied weights, including those supplied by other mods.
    internal static double[] Build(int count, int targetIndex, bool enabled, AncientAppearanceRule rule)
        => Build(Enumerable.Repeat(1d, count).ToArray(), targetIndex, enabled, rule);

    internal static double[] Build(double[] original, int targetIndex, bool enabled, AncientAppearanceRule rule)
    {
        if (targetIndex < 0 || targetIndex >= original.Length) return (double[])original.Clone();
        AncientAppearanceRule normalized = rule.Copy();
        double? probability = !enabled ? 0 : normalized.Mode switch
        {
            AncientAppearanceMode.Disabled => 0,
            AncientAppearanceMode.Guaranteed => 1,
            AncientAppearanceMode.Custom => normalized.Percent / 100,
            _ => null,
        };
        return probability is { } p ? BuildOverrides(original, new Dictionary<int, double> { [targetIndex] = p }) : (double[])original.Clone();
    }

    internal static double[] BuildOverrides(double[] original, IReadOnlyDictionary<int, double> requested)
    {
        double[] weights = (double[])original.Clone();
        if (requested.Count == 0) return weights;
        int[] guaranteed = requested.Where(pair => pair.Value >= 1).Select(pair => pair.Key).ToArray();
        if (guaranteed.Length > 0)
        {
            Array.Fill(weights, 0);
            // The two Ancients normally belong to different acts. If another mod puts
            // both guaranteed choices in one pool, they share that pool equally.
            foreach (int i in guaranteed) weights[i] = 1;
            return weights;
        }
        double totalRequested = requested.Values.Sum();
        double otherWeight = weights.Where((_, i) => !requested.ContainsKey(i)).Sum();
        if (totalRequested >= 1 || otherWeight <= 0)
        {
            Array.Fill(weights, 0);
            foreach ((int i, double p) in requested) weights[i] = p;
            return weights;
        }
        foreach ((int i, double p) in requested) weights[i] = p * otherWeight / (1 - totalRequested);
        return weights;
    }
}
