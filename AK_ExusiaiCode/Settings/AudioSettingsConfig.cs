namespace AK_Exusiai.Settings;

/// <summary>Local sound preferences; never included in shared run data.</summary>
public sealed class AudioSettingsConfig
{
    public double GunshotVolumePercent { get; set; } = 40;

    public AudioSettingsConfig Copy() => new()
    {
        GunshotVolumePercent = double.IsFinite(GunshotVolumePercent) ? Math.Clamp(GunshotVolumePercent, 0, 100) : 40,
    };

    internal float ApplyToSound(string id, float independentVolume) =>
        id == "custom:shot_remix" ? independentVolume * (float)(Copy().GunshotVolumePercent / 100) : independentVolume;
}
