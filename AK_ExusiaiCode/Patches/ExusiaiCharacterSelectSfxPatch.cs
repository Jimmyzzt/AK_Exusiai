using AK_Exusiai.Audio;
using AK_Exusiai.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class ExusiaiCharacterSelectSfxPatch : IPatchMethod
{
    private const string CharacterSelectStream = "exusiai_select.wav";
    private const float CharacterSelectVolume = 0.5f;

    public static string PatchId => "exusiai_character_select_sfx";
    public static string Description => "Play Exusiai's packaged character-select sound";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(
            typeof(NCharacterSelectScreen),
            nameof(NCharacterSelectScreen.SelectCharacter),
#if STS2_0111
            [typeof(NCharacterSelectButton), typeof(CharacterModel)]),
#else
            [typeof(NCharacterSelectButton)]),
#endif
    ];

    public static void Postfix(NCharacterSelectButton charSelectButton)
    {
        if (charSelectButton.Character is Exusiai)
            ExusiaiAudio.Play(CharacterSelectStream, CharacterSelectVolume);
    }
}
