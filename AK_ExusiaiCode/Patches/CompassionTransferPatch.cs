using System.Reflection;
using AK_Exusiai.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace AK_Exusiai.Patches;

internal sealed class CompassionTransferPatch : IPatchMethod
{
    public static string PatchId => "compassion-transfer-power-card-effects";
    public static string Description => "Transfer Angel Power card effects to a targeted teammate";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.AsyncMethod(
            typeof(CardModel),
            nameof(CardModel.OnPlayWrapper),
            typeof(PlayerChoiceContext),
            typeof(Creature),
            typeof(bool),
            typeof(ResourceInfo),
            typeof(bool))
    ];

    private static readonly MethodInfo OriginalCardEffect = AccessTools.DeclaredMethod(
        typeof(CardModel), "OnPlay", [typeof(PlayerChoiceContext), typeof(CardPlay)]);
    private static readonly MethodInfo RitsuCardEffect = AccessTools.DeclaredMethod(
        typeof(CardOnPlayHook), nameof(CardOnPlayHook.RunCardOnPlayHooks),
        [typeof(CardModel), typeof(PlayerChoiceContext), typeof(CardPlay)]);
    private static readonly MethodInfo ReplacementCardEffect = AccessTools.DeclaredMethod(
        typeof(CompassionTransferCmd), nameof(CompassionTransferCmd.PlayCardEffect));
    private static readonly MethodInfo OriginalEnchantmentEffect = AccessTools.DeclaredMethod(
        typeof(EnchantmentModel), nameof(EnchantmentModel.OnPlay), [typeof(PlayerChoiceContext), typeof(CardPlay)]);
    private static readonly MethodInfo ReplacementEnchantmentEffect = AccessTools.DeclaredMethod(
        typeof(CompassionTransferCmd), nameof(CompassionTransferCmd.PlayEnchantmentEffect));
    private static readonly MethodInfo OriginalAfflictionEffect = AccessTools.DeclaredMethod(
        typeof(AfflictionModel), nameof(AfflictionModel.OnPlay), [typeof(PlayerChoiceContext), typeof(Creature)]);
    private static readonly MethodInfo ReplacementAfflictionEffect = AccessTools.DeclaredMethod(
        typeof(CompassionTransferCmd), nameof(CompassionTransferCmd.PlayAfflictionEffect));

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        HarmonyIlRewriter rewriter = HarmonyIlRewriter.From(instructions);
        // RitsuLib redirects CardModel.OnPlay through CardOnPlayHook before this mod's
        // patch group runs. Match either form so this remains compatible with both the
        // framework-patched and pristine game method.
        HarmonyIlRewriteReport cardReport = rewriter.RedirectCalls(
            "redirect power card effect for Compassion",
            called => called == OriginalCardEffect || called == RitsuCardEffect
                ? ReplacementCardEffect
                : null,
            code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, ReplacementCardEffect)));
        cardReport.RequireExactSitesOrAlreadySatisfied(1);
        HarmonyIlRewriteReport enchantmentReport = HarmonyAsyncIl.RedirectAwaitedCalls(
            rewriter, "suppress original enchantment during Compassion transfer", OriginalEnchantmentEffect,
            ReplacementEnchantmentEffect,
            code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, ReplacementEnchantmentEffect)));
        enchantmentReport.RequireExactSitesOrAlreadySatisfied(1);
        HarmonyIlRewriteReport afflictionReport = HarmonyAsyncIl.RedirectAwaitedCalls(
            rewriter, "suppress original affliction during Compassion transfer", OriginalAfflictionEffect,
            ReplacementAfflictionEffect,
            code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, ReplacementAfflictionEffect)));
        afflictionReport.RequireExactSitesOrAlreadySatisfied(1);
        return rewriter.InstructionsChecked([cardReport, enchantmentReport, afflictionReport]);
    }
}
