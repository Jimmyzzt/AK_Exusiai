using AK_Exusiai.Mechanics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace AK_Exusiai.Patches;

internal sealed class AngelFreeCostPatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<CardEnergyCost, CardModel> CardOwner =
        AccessTools.FieldRefAccess<CardEnergyCost, CardModel>("_card");

    public static string PatchId => "force-unused-angel-play-free";
    public static string Description => "Force an Angel card's refreshed first play to spend no Energy";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<CardEnergyCost>(nameof(CardEnergyCost.GetWithModifiers)),
        PatchTarget.Method<CardEnergyCost>(nameof(CardEnergyCost.GetAmountToSpend)),
    ];

    public static void Postfix(CardEnergyCost __instance, ref int __result)
    {
        CardModel card = CardOwner(__instance);
        if (!card.IsCanonical &&
            card.Pile?.IsCombatPile == true &&
            AngelCmd.HasFreePlay(card))
        {
            __result = 0;
        }
    }
}
