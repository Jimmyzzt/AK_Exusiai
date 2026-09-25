using AK_Exusiai.Relics;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace AK_Exusiai.Ancients;

[RegisterActAncient(typeof(Hive))]
public sealed class Laterano : ModAncientEventTemplate
{
    public override Color ButtonColor => new("49433C");
    public override Color DialogueColor => new("5B4631");

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"{Entry.ResPath}/images/ancients/laterano/portrait.png",
        BackgroundScenePath: $"{Entry.ResPath}/scenes/ancients/laterano_background.tscn");

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: $"{Entry.ResPath}/images/ancients/laterano/map_icon.png",
        MapIconOutlinePath: $"{Entry.ResPath}/images/ancients/laterano/map_iconOutline.png",
        RunHistoryIconPath: $"{Entry.ResPath}/images/ancients/laterano/map_icon.png",
        RunHistoryIconOutlinePath: $"{Entry.ResPath}/images/ancients/laterano/map_iconOutline.png");

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        CreateModRelicOption<PhotoWithTheLord>(),
        CreateModRelicOption<EntryPermit>(),
        CreateModRelicOption<StudyTourCertificate>(),
        CreateModRelicOption<LordDrone>(),
        CreateModRelicOption<SprayCan>(),
        CreateModRelicOption<CactusTart>(),
        CreateModRelicOption<PrismaticWings>(),
        CreateModRelicOption<Confess47>(),
        CreateModRelicOption<BeaconOfNations>(),
        CreateModRelicOption<TheLaw>(),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        EventOption[] first = [CreateModRelicOption<PhotoWithTheLord>(), CreateModRelicOption<EntryPermit>(), CreateModRelicOption<StudyTourCertificate>()];
        EventOption[] second = [CreateModRelicOption<LordDrone>(), CreateModRelicOption<SprayCan>(), CreateModRelicOption<CactusTart>(), CreateModRelicOption<PrismaticWings>()];
        List<EventOption> third = [CreateModRelicOption<BeaconOfNations>(), CreateModRelicOption<TheLaw>()];
        if (Owner is { } owner && !owner.HasEventPet())
            third.Add(CreateModRelicOption<Confess47>());
        return [Rng.NextItem(first)!, Rng.NextItem(second)!, Rng.NextItem(third)!];
    }
}
