using Godot;
using AK_Exusiai.Characters;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal static class ExusiaiAmbientAnimation
{
    internal const string MerchantSceneRootName = "ExusiaiCharacterMerchant";
    internal const string RestSiteSceneRootName = "ExusiaiCharacterRestSite";
    internal const string MerchantInteractAnimation = "Interact";
    internal const string RestSiteAnimation = "Sit";

    internal static Node? FindSpineSprite(Node root, string sceneRootName)
    {
        if (!ContainsNodeNamed(root, sceneRootName))
            return null;

        return FindSpineSpriteRecursive(root);
    }

    internal static void Play(
        Node owner,
        Node spineNode,
        string animation,
        bool loop)
    {
        MegaSprite sprite = new(spineNode);
        owner.RunWhenSpineReady(sprite, animationState =>
            animationState.Call("set_animation", animation, loop, 0));
    }

    internal static bool ContainsNodeNamed(Node node, string nodeName)
    {
        if (node.Name.ToString() == nodeName)
            return true;

        foreach (Node child in node.GetChildren())
        {
            if (ContainsNodeNamed(child, nodeName))
                return true;
        }

        return false;
    }

    private static Node? FindSpineSpriteRecursive(Node node)
    {
        if (node.GetClass().ToString() == MegaSprite.spineClassName)
            return node;

        foreach (Node child in node.GetChildren())
        {
            Node? match = FindSpineSpriteRecursive(child);
            if (match is not null)
                return match;
        }

        return null;
    }
}

internal sealed class ExusiaiMerchantCharacterReadyPatch : IPatchMethod
{
    public static string PatchId => "exusiai_merchant_relax_ready";
    public static string Description =>
        "Start Exusiai's selected looping merchant animation when the scene becomes ready";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NMerchantCharacter>(nameof(NMerchantCharacter._Ready)),
    ];

    public static bool Prefix(NMerchantCharacter __instance)
    {
        Node? spineNode = ExusiaiAmbientAnimation.FindSpineSprite(
            __instance,
            ExusiaiAmbientAnimation.MerchantSceneRootName);
        if (spineNode is null)
            return true;

        ExusiaiAppearanceManager.ApplyBuildSkinToSprite(spineNode);
        ExusiaiAmbientAnimation.Play(
            __instance,
            spineNode,
            ExusiaiAppearanceManager.SelectedMerchantAnimation,
            loop: true);
        return false;
    }
}

internal sealed class ExusiaiMerchantCharacterPlayAnimationPatch : IPatchMethod
{
    public static string PatchId => "exusiai_merchant_relax_play_animation";
    public static string Description =>
        "Map Exusiai's merchant idle and interaction requests to Relax and Interact";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NMerchantCharacter>(
            nameof(NMerchantCharacter.PlayAnimation),
            [typeof(string), typeof(bool)]),
    ];

    public static bool Prefix(
        NMerchantCharacter __instance,
        string anim,
        bool loop)
    {
        Node? spineNode = ExusiaiAmbientAnimation.FindSpineSprite(
            __instance,
            ExusiaiAmbientAnimation.MerchantSceneRootName);
        if (spineNode is null)
            return true;

        bool isIdleRequest = anim == CharacterModel.relaxedAnim
            || anim == "Relax"
            || anim == "Special";
        string idleAnimation = ExusiaiAppearanceManager.SelectedMerchantAnimation;
        MegaSprite sprite = new(spineNode);
        __instance.RunWhenSpineReady(sprite, animationState =>
        {
            if (isIdleRequest)
            {
                animationState.SetAnimation(
                    idleAnimation,
                    loop: true);
                return;
            }

            animationState.SetAnimation(
                ExusiaiAmbientAnimation.MerchantInteractAnimation,
                loop: false);
        });
        return false;
    }

    internal static IEnumerable<Node2D> FindNodesNamed(Node node, string nodeName)
    {
        if (node is Node2D node2D && node.Name.ToString() == nodeName)
            yield return node2D;

        foreach (Node child in node.GetChildren())
        {
            foreach (Node2D match in FindNodesNamed(child, nodeName))
                yield return match;
        }
    }
}

internal sealed class ExusiaiFakeMerchantScalePatch : IPatchMethod
{
    private const float FakeMerchantScale = 0.35f;

    public static string PatchId => "exusiai_fake_merchant_scale";
    public static string Description =>
        "Match Exusiai's merchant skeleton to her combat scale in the Fake Merchant event";

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NFakeMerchant), "AfterRoomIsLoaded"),
    ];

    public static void Postfix(NFakeMerchant __instance)
    {
        // RitsuLib instantiates the custom merchant scene here, but Godot still
        // reports its scene root as Node2D rather than NMerchantCharacter.
        // Locate the authored root by name instead of filtering by CLR type.
        foreach (Node2D merchantVisual in ExusiaiAmbientAnimation.FindNodesNamed(
                     __instance,
                     ExusiaiAmbientAnimation.MerchantSceneRootName))
        {
            merchantVisual.Scale = Vector2.One * FakeMerchantScale;
        }
    }
}

internal sealed class ExusiaiRestSiteCharacterReadyPatch : IPatchMethod
{
    public static string PatchId => "exusiai_rest_site_sit_ready";
    public static string Description =>
        "Start Exusiai's looping Sit animation when the rest-site scene becomes ready";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NRestSiteCharacter>(nameof(NRestSiteCharacter._Ready)),
    ];

    public static void Postfix(NRestSiteCharacter __instance)
    {
        Node? spineNode = ExusiaiAmbientAnimation.FindSpineSprite(
            __instance,
            ExusiaiAmbientAnimation.RestSiteSceneRootName);
        if (spineNode is null)
            return;

        ExusiaiAppearanceManager.ApplyBuildSkinToSprite(spineNode);
        ExusiaiAmbientAnimation.Play(
            __instance,
            spineNode,
            ExusiaiAmbientAnimation.RestSiteAnimation,
            loop: true);
    }
}
