using System.Reflection;
using System.Reflection.Emit;
using AK_Exusiai.Effects;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace AK_Exusiai.Patches;

internal sealed class CardAttackIntervalPatch : IPatchMethod
{
    public static string PatchId => "card-preset-attack-interval";
    public static string Description => "Use an opted-in card preset's cadence for its final damage presentation wait";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.AsyncMethod(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
            typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp),
            typeof(Creature), typeof(CardModel), typeof(CardPlay))
    ];

    private static readonly MethodInfo OriginalWait = AccessTools.DeclaredMethod(typeof(Cmd), nameof(Cmd.CustomScaledWait),
        [typeof(float), typeof(float), typeof(bool), typeof(CancellationToken)]);
    private static readonly MethodInfo ReplacementWait = AccessTools.DeclaredMethod(typeof(CardAttackIntervalPatch), nameof(WaitAfterDamage));

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        Type stateMachine = __originalMethod.DeclaringType ?? throw new InvalidOperationException("Missing Damage state machine.");
        FieldInfo Field(string name, Type type)
        {
            FieldInfo field = AccessTools.Field(stateMachine, name) ?? throw new MissingFieldException(stateMachine.FullName, name);
            if (field.FieldType != type) throw new InvalidOperationException($"Damage state field changed: {name}");
            return field;
        }
        FieldInfo play = Field("cardPlay", typeof(CardPlay));
        FieldInfo source = Field("cardSource", typeof(CardModel));
        FieldInfo dealer = Field("dealer", typeof(Creature));
        FieldInfo props = Field("props", typeof(ValueProp));
        var rewriter = HarmonyIlRewriter.From(instructions);
        var report = HarmonyAsyncIl.ReplaceAwaitedCalls(rewriter, "replace Damage's final presentation wait", OriginalWait, _ =>
        [
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, play),
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, source),
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, dealer),
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, props),
            new CodeInstruction(OpCodes.Call, ReplacementWait),
        ], code => code.Any(i => HarmonyIl.IsCallTo(i, ReplacementWait)));
        report.RequireExactSitesOrAlreadySatisfied(1);
        return rewriter.InstructionsChecked(report);
    }

    internal static Task WaitAfterDamage(float fastSeconds, float standardSeconds, bool ignoreCombatEnd, CancellationToken cancellationToken,
        CardPlay? play, CardModel? source, Creature? dealer, ValueProp props)
    {
        if (CardEffectRuntime.TryGetRemainingInterval(play, source, dealer, props, out float remaining))
            return Cmd.CustomScaledWait(remaining, remaining, ignoreCombatEnd, cancellationToken);
        return Cmd.CustomScaledWait(fastSeconds, standardSeconds, ignoreCombatEnd, cancellationToken);
    }
}
