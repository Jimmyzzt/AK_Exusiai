using AK_Exusiai.Characters;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace AK_Exusiai.Effects;

internal sealed class EffectPlayback : IDisposable
{
    internal readonly CancellationTokenSource Cancellation = new();
    private readonly List<Node> _nodes = [];
    internal bool Started;
    internal void Track(Node node) => _nodes.Add(node);
    public void Dispose()
    {
        Cancellation.Cancel();
        foreach (Node node in _nodes)
            if (GodotObject.IsInstanceValid(node)) node.QueueFree();
        _nodes.Clear();
    }
}

internal static class CardEffectPlayer
{
    internal static EffectRecipe Resolve(CardEffectConfig config, EffectRecipe? fallback = null)
    {
        EffectRecipe r = config.Preset == "inherit" ? fallback ?? new() : config.Preset == "none" ? new() :
            CardEffectStore.Entries[config.Preset].Recipe ?? throw new InvalidDataException("Preset has no recipe.");
        string Choose(string value, string original) => value == "inherit" ? original : value == "none" ? "" : value;
        return new()
        {
            Launch = Choose(config.Launch, r.Launch), Hit = Choose(config.Hit, r.Hit),
            LaunchSound = Choose(config.LaunchSound, r.LaunchSound), HitSound = Choose(config.HitSound, r.HitSound),
            Animation = Choose(config.Animation, r.Animation), Delay = config.Delay < 0 ? r.Delay : config.Delay,
            PreAnimation = r.PreAnimation, EmbeddedAudio = r.EmbeddedAudio,
        };
    }

    internal static async Task Play(EffectPlayback playback, CardEffectConfig config, EffectRecipe recipe,
        Creature owner, IReadOnlyList<Creature> targets, bool animate = true)
    {
        playback.Cancellation.Token.ThrowIfCancellationRequested();
        bool start = !playback.Started || config.Repeat == "per_hit";
        playback.Started = true;
        async Task Launch()
        {
            if (recipe.Launch.Length > 0) Spawn(recipe.Launch, owner, targets, config, playback, true);
            Sound(recipe.LaunchSound, config.Volume, playback);
            if (recipe.Delay > 0) await Cmd.Wait(recipe.Delay, playback.Cancellation.Token);
        }
        if (start)
        {
            if (recipe.PreAnimation) await Launch();
            if (animate && recipe.Animation.Length > 0)
            {
                await CreatureCmd.TriggerAnim(owner, recipe.Animation, recipe.Animation == "Cast" ? owner.Player!.Character.CastAnimDelay : owner.Player!.Character.AttackAnimDelay);
                playback.Cancellation.Token.ThrowIfCancellationRequested();
            }
            if (!recipe.PreAnimation) await Launch();
        }
        foreach (Creature target in targets)
            if (target.IsAlive) Spawn(recipe.Hit, owner, [target], config, playback, false);
        if (targets.Count > 0) Sound(recipe.HitSound, config.Volume, playback);
    }

    internal static void Sound(string id, float volume, EffectPlayback playback)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!CardEffectStore.Entries.TryGetValue(id, out var e) || e.Kind != "sfx" || e.Status != "adapted") return;
        if (e.Adapter == "event") SfxCmd.Play(e.Resource, volume);
        else if (e.Adapter == "audio")
        {
            var stream = ResourceLoader.Load<AudioStream>(e.Resource);
            if (stream == null) throw new InvalidDataException($"Missing audio: {e.Resource}");
            var player = new AudioStreamPlayer { Stream = stream, VolumeDb = Mathf.LinearToDb(Math.Max(0.0001f, volume)), Bus = "Sfx" };
            NCombatRoom.Instance?.CombatVfxContainer.AddChild(player);
            playback.Track(player);
            player.Finished += player.QueueFree;
            player.Play();
        }
    }

    internal static void Spawn(string id, Creature owner, IReadOnlyList<Creature> targets, CardEffectConfig config, EffectPlayback playback, bool launch)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!CardEffectStore.Entries.TryGetValue(id, out var e) || e.Kind != "vfx" || e.Status != "adapted") return;
        NCombatRoom? room = NCombatRoom.Instance;
        var attacker = room?.GetCreatureNode(owner);
        var victim = room?.GetCreatureNode(targets.FirstOrDefault() ?? owner);
        if (room == null || attacker == null || victim == null) return;
        string appearance = $"{ExusiaiAppearanceManager.SelectedCharacterIndex}:{ExusiaiAppearanceManager.SelectedSkinIndex}";
        float[] offset = config.AppearanceOffsets.GetValueOrDefault(appearance) ?? config.Offset;
        // Creature-local offsets remain stable as the combat viewport moves.
        Vector2 source = attacker.VfxSpawnPosition + attacker.GetGlobalTransform().BasisXform(new Vector2(offset[0], offset[1]));
        Vector2 center = victim.VfxSpawnPosition, ground = victim.GetBottomOfHitbox();
        Vector2 point = launch ? source : config.Ground ? ground : center;
        Node2D? node;
        if (e.Adapter == "scene")
        {
            node = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath(e.Resource)).Instantiate<Node2D>();
            node.Position = point;
        }
        else node = e.Factory switch
        {
            nameof(NHyperbeamVfx) => NHyperbeamVfx.Create(source, center),
            nameof(NHyperbeamImpactVfx) => NHyperbeamImpactVfx.Create(source, center),
            nameof(NGrandFinaleVfx) => NGrandFinaleVfx.Create(source),
            nameof(NGrandFinaleImpactVfx) => NGrandFinaleImpactVfx.Create(center, ground),
            nameof(NSweepingBeamVfx) => NSweepingBeamVfx.Create(source, new Godot.Collections.Array<Vector2>(targets.Select(t => room.GetCreatureNode(t)?.VfxSpawnPosition ?? center))),
            nameof(NSweepingBeamImpactVfx) => NSweepingBeamImpactVfx.Create(center),
            nameof(NBigSlashVfx) => NBigSlashVfx.Create(point, center.X >= source.X),
            nameof(NBigSlashImpactVfx) => NBigSlashImpactVfx.Create(point),
            nameof(NDaggerSprayFlurryVfx) => NDaggerSprayFlurryVfx.Create(point, Colors.White, center.X >= source.X),
            nameof(NDaggerSprayImpactVfx) => NDaggerSprayImpactVfx.Create(point, Colors.White, center.X >= source.X),
            nameof(NShivThrowVfx) => NShivThrowVfx.Create(source, center, Colors.White),
            nameof(NScratchVfx) => NScratchVfx.Create(point, center.X >= source.X),
            nameof(NFireBurstVfx) => NFireBurstVfx.Create(ground, 1f),
            nameof(NFireBurningVfx) => NFireBurningVfx.Create(ground, 1f, center.X >= source.X),
            nameof(NGoopyImpactVfx) => NGoopyImpactVfx.Create(point, Colors.White),
            nameof(NGaseousImpactVfx) => NGaseousImpactVfx.Create(point, Colors.White),
            nameof(NPoisonImpactVfx) => NPoisonImpactVfx.Create(point),
            nameof(NHeavyBluntVfx) => NHeavyBluntVfx.Create(point),
            nameof(NLineBurstVfx) => NLineBurstVfx.Create(point),
            nameof(NScreamVfx) => NScreamVfx.Create(point),
            nameof(NLargeMagicMissileVfx) => NLargeMagicMissileVfx.Create(ground, Colors.White),
            nameof(NSmallMagicMissileVfx) => NSmallMagicMissileVfx.Create(center, Colors.White),
            nameof(NMinionDiveBombVfx) => NMinionDiveBombVfx.Create(source, ground),
            nameof(NSporeImpactVfx) => NSporeImpactVfx.Create(ground, Colors.White),
            _ => throw new InvalidDataException($"No visual-only factory adapter for {e.Factory}"),
        };
        if (node == null) return;
        room.CombatVfxContainer.AddChildSafely(node);
        playback.Track(node);
        // Some game effects have a long tail; bound the lifetime of our own instances.
        var timer = node.GetTree().CreateTimer(12);
        timer.Timeout += () => { if (GodotObject.IsInstanceValid(node)) node.QueueFree(); };
    }
}
