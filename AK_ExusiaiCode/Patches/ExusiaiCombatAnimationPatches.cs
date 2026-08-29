using AK_Exusiai.Characters;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class ExusiaiNewCovenantAttackAnimationPatch : IPatchMethod
{
    public static string PatchId => "exusiai_new_covenant_attack_animation_reset";
    public static string Description =>
        "Reset New Covenant Spine state before its optimized attack animation";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCreature>(
            nameof(NCreature.SetAnimationTrigger),
            [typeof(string)]),
    ];

    public static void Prefix(NCreature __instance, string trigger)
    {
        if (trigger != "Attack"
            || __instance.Entity.Player?.Character is not Exusiai
            || !ExusiaiAppearanceManager.IsNewCovenant
            || !__instance.HasSpineAnimation)
        {
            return;
        }

        MegaSprite? sprite = __instance.Visuals.SpineBody;
        if (sprite is null)
            return;

        using MegaAnimationState? animationState = sprite.TryGetAnimationState();
        using MegaSkeleton? skeleton = sprite.GetSkeleton();
        animationState?.BoundObject.Call("clear_tracks");
        skeleton?.BoundObject.Call("set_to_setup_pose");
    }
}
