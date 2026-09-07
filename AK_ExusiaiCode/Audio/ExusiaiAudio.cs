using Godot;

namespace AK_Exusiai.Audio;

internal static class ExusiaiAudio
{
    private const string AudioDirectory = $"{Entry.ResPath}/audio";

    internal static void Play(string fileName, float linearVolume = 1f)
    {
        AudioStream? stream = ResourceLoader.Load<AudioStream>(
            $"{AudioDirectory}/{fileName}");
        if (stream is null)
        {
            Entry.Logger.Warn($"Unable to load Exusiai audio: {fileName}");
            return;
        }

        AudioStreamPlayer player = new()
        {
            Stream = stream,
            ProcessMode = Node.ProcessModeEnum.Always,
            VolumeDb = linearVolume > 0f
                ? Mathf.LinearToDb(linearVolume)
                : -80f,
            Bus = "Sfx",
        };
        player.Finished += player.QueueFree;

        SceneTree sceneTree = (SceneTree)Engine.GetMainLoop();
        sceneTree.Root.AddChild(player);
        player.Play();
    }
}
