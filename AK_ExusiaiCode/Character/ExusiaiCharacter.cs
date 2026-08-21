using AK_Exusiai.AK_ExusiaiCode.Cards;
using AK_Exusiai.AK_ExusiaiCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;

namespace AK_Exusiai.AK_ExusiaiCode.Character;

[RegisterCharacter]
public sealed class ExusiaiCharacter : ModCharacterTemplate<AKExusiaiCardPool, AKExusiaiRelicPool, AKExusiaiPotionPool>
{
    public override Color NameColor => new("ffffff");
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 70;
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.15f;
    public override bool RequiresEpochAndTimeline => false;

    public override List<string> GetArchitectAttackVfx()
    {
        return [];
    }

    [Obsolete("RitsuLib 0.5.14 marks type-based starting entries obsolete, but they remain usable for the initial scaffold.")]
    protected override IEnumerable<StartingDeckEntry> StartingDeckEntries =>
    [
        new(typeof(StrikeIronclad), 4),
        new(typeof(DefendIronclad), 4),
        new(typeof(ChargingMode), 1),
        new(typeof(LockedAndLoaded), 1)
    ];

    [Obsolete("RitsuLib 0.5.14 marks type-based starting entries obsolete, but they remain usable for the initial scaffold.")]
    protected override IEnumerable<Type> StartingRelicTypes =>
    [
        typeof(ExusiaiBadge)
    ];
}
