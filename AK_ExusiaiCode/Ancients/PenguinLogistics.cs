using AK_Exusiai.Relics;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Ancients;

[RegisterActAncient(typeof(Glory))]
public sealed class PenguinLogistics : ModAncientEventTemplate
{
    public override Color ButtonColor => new("373648");
    public override Color DialogueColor => new("242D3B");

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"{Entry.ResPath}/images/ancients/emperor/portrait.png",
        BackgroundScenePath: $"{Entry.ResPath}/scenes/ancients/emperor_background.tscn");

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: $"{Entry.ResPath}/images/ancients/emperor/map_icon.png",
        MapIconOutlinePath: $"{Entry.ResPath}/images/ancients/emperor/map_iconOutline.png",
        RunHistoryIconPath: $"{Entry.ResPath}/images/ancients/emperor/map_icon.png",
        RunHistoryIconOutlinePath: $"{Entry.ResPath}/images/ancients/emperor/map_iconOutline.png");

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<PenguinLogisticsId>(),
        CreateModRelicOption<AFewFineVintages>(),
        CreateModRelicOption<BlackCard>(),
        CreateModRelicOption<MasterTape>(),
        CreateModRelicOption<IllGottenGains>(),
        CreateModRelicOption<CompanyVan>(),
        CreateModRelicOption<ReturnToSender>(),
        CreateModRelicOption<DjDeck>(),
        CreateModRelicOption<PrizedRecord>(),
        CreateModRelicOption<BossBusinessCard>(),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        EventOption[] first = [CreateModRelicOption<PenguinLogisticsId>(), CreateModRelicOption<AFewFineVintages>(), CreateModRelicOption<BlackCard>(), CreateModRelicOption<MasterTape>()];
        List<EventOption> second = [CreateModRelicOption<IllGottenGains>(), CreateModRelicOption<ReturnToSender>()];
        // The van's map transition has no multiplayer equivalent.
        if (Owner?.RunState.Players.Count == 1)
            second.Add(CreateModRelicOption<CompanyVan>());
        EventOption[] third = [CreateModRelicOption<DjDeck>(), CreateModRelicOption<PrizedRecord>(), CreateModRelicOption<BossBusinessCard>()];
        return [Rng.NextItem(first)!, Rng.NextItem(second)!, Rng.NextItem(third)!];
    }
}
