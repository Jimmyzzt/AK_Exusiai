using AK_Exusiai.Characters;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class ExusiaiCustomSfxTokenPatch : IPatchMethod
{
    public static string PatchId => "exusiai_custom_sfx_token";
    public static string Description =>
        "Keep Exusiai's raw character-select stream out of the FMOD event player";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(
            typeof(SfxCmd),
            nameof(SfxCmd.Play),
            [typeof(string), typeof(float)]),
    ];

    public static bool Prefix(string sfx)
    {
        return sfx != Exusiai.CustomCharacterSelectSfxToken;
    }
}
