using AK_Exusiai.Ancients;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.addons.mega_text;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class AncientOptionTitlePatch : IPatchMethod
{
    public static string PatchId => "ancient-option-title-size";
    public static string Description => "Keep the title size consistent in custom Ancient option buttons";
    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NEventOptionButton>(nameof(NEventOptionButton._Ready)),
    ];

    public static void Postfix(NEventOptionButton __instance)
    {
        if (__instance.Event is not (Laterano or PenguinLogistics))
            return;
        MegaRichTextLabel? label = __instance.GetNodeOrNull<MegaRichTextLabel>("%Text");
        if (label is null || label.Text.Contains("[font_size=24]"))
            return;
        string text = label.Text;
        int titleEnd = text.IndexOf("[/b]", StringComparison.Ordinal);
        if (titleEnd < 0 || !text.StartsWith("[gold][b]", StringComparison.Ordinal))
            return;
        string title = text[..titleEnd].Replace("[gold][b]", "[gold][b][font_size=24]", StringComparison.Ordinal);
        label.Text = title + "[/font_size]" + text[titleEnd..];
    }
}
