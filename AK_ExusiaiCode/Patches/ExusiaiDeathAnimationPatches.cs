using AK_Exusiai.Characters;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal static class ExusiaiSpineAnimation
{
    private const string SkeletonDataPath =
        $"{Entry.ResPath}/images/character/spine/default/exusiai_default_skeleton_data.tres";

    internal static void PlayDeath(MegaAnimationState? animationState)
    {
        animationState?.Call("set_animation", "Die", false, 0);
    }

    internal static IEnumerable<Node> FindExusiaiSpineSprites(Node root)
    {
        if (IsExusiaiSpineSprite(root))
            yield return root;

        foreach (Node child in root.GetChildren())
        {
            foreach (Node match in FindExusiaiSpineSprites(child))
                yield return match;
        }
    }

    private static bool IsExusiaiSpineSprite(Node node)
    {
        if (node.GetClass().ToString() != MegaSprite.spineClassName)
            return false;

        Variant skeletonDataVariant = node.Get("skeleton_data_res");
        Resource? skeletonData = skeletonDataVariant.AsGodotObject() as Resource;
        return skeletonData?.ResourcePath == SkeletonDataPath;
    }
}

internal sealed class ExusiaiDeathAnimationPatch : IPatchMethod
{
    public static string PatchId => "exusiai_death_animation_case_compatibility";
    public static string Description =>
        "Play Exusiai's case-sensitive Die animation before the base death sequence";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCreature>(nameof(NCreature.StartDeathAnim), [typeof(bool)]),
    ];

    public static void Prefix(NCreature __instance)
    {
        if (__instance.Entity.Player?.Character is not Exusiai || !__instance.HasSpineAnimation)
            return;

        ExusiaiSpineAnimation.PlayDeath(__instance.SpineAnimation.GetAnimationState());
    }
}

internal sealed class ExusiaiGameOverAnimationPatch : IPatchMethod
{
    public static string PatchId => "exusiai_game_over_animation_case_compatibility";
    public static string Description =>
        "Replace the game-over screen's lowercase die request for Exusiai's Spine model";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NGameOverScreen>(nameof(NGameOverScreen.AfterOverlayOpened)),
    ];

    public static void Postfix(NGameOverScreen __instance)
    {
        Control? creatureContainer = __instance.GetNodeOrNull<Control>("%CreatureContainer");
        if (creatureContainer is null)
            return;

        foreach (Node spineNode in ExusiaiSpineAnimation.FindExusiaiSpineSprites(creatureContainer))
        {
            MegaSprite sprite = new(spineNode);
            if (sprite.HasAnimation("Die") && !sprite.HasAnimation("die"))
                ExusiaiSpineAnimation.PlayDeath(sprite.GetAnimationState());
        }
    }
}
