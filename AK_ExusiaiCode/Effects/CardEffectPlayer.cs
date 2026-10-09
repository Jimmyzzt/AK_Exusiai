using AK_Exusiai.Characters;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Nodes;

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
            AnimationDelay = r.AnimationDelay,
            PreAnimation = r.PreAnimation, EmbeddedAudio = r.EmbeddedAudio,
            LaunchOnTargets = r.LaunchOnTargets, Ground = r.Ground, Tint = r.Tint, VfxColor = r.VfxColor, Scale = r.Scale,
            LaunchExtras = config.Launch == "inherit" ? r.LaunchExtras : [],
            HitExtras = config.Hit == "inherit" ? r.HitExtras : [],
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
            foreach (string id in new[] { recipe.Launch }.Concat(recipe.LaunchExtras))
            {
                if (recipe.LaunchOnTargets)
                    foreach (Creature target in targets) Spawn(id, owner, [target], config, playback, false, recipe);
                else Spawn(id, owner, targets, config, playback, true, recipe);
            }
            Sound(recipe.LaunchSound, config.Volume, playback);
            if (recipe.Delay > 0) await Cmd.Wait(recipe.Delay, playback.Cancellation.Token);
        }
        if (start)
        {
            if (recipe.PreAnimation) await Launch();
            if (animate && recipe.Animation.Length > 0)
            {
                await CreatureCmd.TriggerAnim(owner, recipe.Animation, recipe.AnimationDelay >= 0 ? recipe.AnimationDelay : recipe.Animation == "Cast" ? owner.Player!.Character.CastAnimDelay : owner.Player!.Character.AttackAnimDelay);
                playback.Cancellation.Token.ThrowIfCancellationRequested();
            }
            if (!recipe.PreAnimation) await Launch();
        }
        foreach (Creature target in targets)
            if (target.IsAlive)
                foreach (string id in new[] { recipe.Hit }.Concat(recipe.HitExtras))
                    Spawn(id, owner, [target], config, playback, false, recipe);
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

    internal static void Spawn(string id, Creature owner, IReadOnlyList<Creature> targets, CardEffectConfig config, EffectPlayback playback, bool launch, EffectRecipe? recipe = null)
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
        Vector2 point = launch ? source : config.Ground || recipe?.Ground == true ? ground : center;
        Color tint = new(recipe?.Tint ?? "ffffff");
        VfxColor vfxColor = Enum.TryParse(recipe?.VfxColor, out VfxColor color) ? color : VfxColor.Red;
        float scale = recipe?.Scale ?? 1;
        Node? node;
        if (e.Adapter is "scene" or "fullscreen")
        {
            var sceneNode = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath(e.Resource)).Instantiate<Node2D>();
            sceneNode.Position = point;
            node = sceneNode;
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
            nameof(NDaggerSprayFlurryVfx) => NDaggerSprayFlurryVfx.Create(point, tint, center.X >= source.X),
            nameof(NDaggerSprayImpactVfx) => NDaggerSprayImpactVfx.Create(point, tint, center.X >= source.X),
            nameof(NShivThrowVfx) => NShivThrowVfx.Create(source, center, tint),
            nameof(NScratchVfx) => NScratchVfx.Create(point, center.X >= source.X),
            nameof(NFireBurstVfx) => NFireBurstVfx.Create(ground, scale),
            nameof(NFireBurningVfx) => NFireBurningVfx.Create(ground, 1f, center.X >= source.X),
            nameof(NGoopyImpactVfx) => NGoopyImpactVfx.Create(point, tint),
            nameof(NGaseousImpactVfx) => NGaseousImpactVfx.Create(point, Colors.White),
            nameof(NPoisonImpactVfx) => NPoisonImpactVfx.Create(point),
            nameof(NHeavyBluntVfx) => NHeavyBluntVfx.Create(point),
            nameof(NLineBurstVfx) => NLineBurstVfx.Create(point),
            nameof(NScreamVfx) => NScreamVfx.Create(point),
            nameof(NLargeMagicMissileVfx) => NLargeMagicMissileVfx.Create(ground, tint),
            nameof(NSmallMagicMissileVfx) => NSmallMagicMissileVfx.Create(ground, tint),
            nameof(NMinionDiveBombVfx) => NMinionDiveBombVfx.Create(source, ground),
            nameof(NSporeImpactVfx) => NSporeImpactVfx.Create(ground, Colors.White),
            nameof(NBolasVfx) => NBolasVfx.Create(owner, targets.FirstOrDefault() ?? owner),
            nameof(NGroundFireVfx) => NGroundFireVfx.Create(targets.FirstOrDefault() ?? owner, vfxColor),
            nameof(NSpikeSplashVfx) => NSpikeSplashVfx.Create(targets.FirstOrDefault() ?? owner, vfxColor),
            nameof(NThinSliceVfx) => NThinSliceVfx.Create(targets.FirstOrDefault() ?? owner, vfxColor),
            nameof(NStabVfx) => NStabVfx.Create(targets.FirstOrDefault() ?? owner, true, vfxColor),
            nameof(NHorizontalLinesVfx) => NHorizontalLinesVfx.Create(tint, 2),
            nameof(NSmokyVignetteVfx) => NSmokyVignetteVfx.Create(tint, tint),
            _ => throw new InvalidDataException($"No visual-only factory adapter for {e.Factory}"),
        };
        if (node == null) return;
        if (node is Control) NRun.Instance?.GlobalUi.AddChildSafely(node);
        else room.CombatVfxContainer.AddChildSafely(node);
        if (e.Adapter == "fullscreen" && node is Node2D fullScreen)
            fullScreen.GlobalPosition = NGame.Instance!.GetViewportRect().Size * 0.5f;
        playback.Track(node);
        // Some game effects have a long tail; bound the lifetime of our own instances.
        var timer = node.GetTree().CreateTimer(12);
        timer.Timeout += () => { if (GodotObject.IsInstanceValid(node)) node.QueueFree(); };
    }
}
