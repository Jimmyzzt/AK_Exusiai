using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace AK_Exusiai.Mechanics;

/// <summary>
/// Stores Free Delivery's player-specific mystery relic redundantly on that
/// player's relics and Free Delivery deck copies, so replacing a starter relic
/// cannot discard the state.
/// </summary>
[RegisterModelCapability]
public sealed class MysteryRelicCapability : ModelCapability
{
    public string RelicId { get; private set; } = string.Empty;
    public bool IsRevealed { get; private set; }

    public void Reveal(ModelId relicId)
    {
        RelicId = relicId.ToString();
        IsRevealed = true;
        MarkDirty();
    }

    protected override JsonNode SaveAdditionalState() => new JsonObject
    {
        ["relicId"] = RelicId,
        ["isRevealed"] = IsRevealed,
    };

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        if (state is not JsonObject obj)
            return;

        RelicId = obj["relicId"]?.GetValue<string>() ?? string.Empty;
        IsRevealed = obj["isRevealed"]?.GetValue<bool>() ?? false;
    }

    protected override void OnAttach(AbstractModel owner)
    {
        if (owner is not RelicModel and not CardModel)
            throw new InvalidOperationException(
                "Mystery relic state must be attached to a relic or deck card.");
    }
}
