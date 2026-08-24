using AK_Exusiai.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class AngelEnergyCostPatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<CardEnergyCost, CardModel> CardOwner =
        AccessTools.FieldRefAccess<CardEnergyCost, CardModel>("_card");

    public static string PatchId => "record-angel-energy-cost-floor";
    public static string Description =>
        "Record Angel cost reductions only when deterministic card cost mutations occur";

    public static ModPatchTarget[] GetTargets() =>
    [
        CostMutation(nameof(CardEnergyCost.SetUntilPlayed), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.SetThisTurnOrUntilPlayed), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.SetThisTurn), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.SetThisCombat), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.AddUntilPlayed), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.AddThisTurnOrUntilPlayed), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.AddThisTurn), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.AddThisCombat), typeof(int), typeof(bool)),
        CostMutation(nameof(CardEnergyCost.SetCustomBaseCost), typeof(int)),
    ];

    public static void Postfix(CardEnergyCost __instance)
    {
        AngelCmd.RecordCurrentCombatCost(CardOwner(__instance));
    }

    private static ModPatchTarget CostMutation(string methodName, params Type[] parameterTypes) =>
        PatchTarget.Method<CardEnergyCost>(methodName, parameterTypes);
}
