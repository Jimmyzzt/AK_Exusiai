using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace AK_Exusiai.Monsters;

[RegisterMonster]
public sealed class Confess47Pet : ModMonsterTemplate
{
    private const string ScenePath = "res://AK_Exusiai/scenes/ancients/confess47_pet.tscn";

    public override MonsterAssetProfile AssetProfile => new(ScenePath);
    public override int MinInitialHp => 9999;
    public override int MaxInitialHp => 9999;
    public override bool IsHealthBarVisible => false;

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(ScenePath);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine([idle], idle);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("Idle", isLooping: true);
        AnimState attack = new("Attack") { NextState = idle };
        AnimState sleep = new("Default", isLooping: true);
        AnimState wake = new("Start") { NextState = idle };
        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Sleep", sleep);
        animator.AddAnyState("WakeUp", wake);
        return animator;
    }
}
