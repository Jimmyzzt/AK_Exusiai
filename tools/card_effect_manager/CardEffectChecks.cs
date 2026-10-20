using System.Text.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using AK_Exusiai.Effects;
using AK_Exusiai.Patches;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

public partial class CardEffectChecks : Node
{
    public override void _Ready()
    {
        try
        {
            var command = new AK_Exusiai.Debug.ExusiaiConsoleCmd();
            Check(command.GetArgumentCompletions(null, ["f"]).CommonPrefix == "exusiai fx ", "FX subcommand completion");
            var actions = command.GetArgumentCompletions(null, ["fx", ""]);
            Check(actions.CommandPrefix == "exusiai fx " && actions.Candidates.SequenceEqual(new[] { "on", "off", "status" }), "FX action completion");
            Check(command.GetArgumentCompletions(null, ["fx", "of"]).CommonPrefix == "exusiai fx off ", "FX partial action completion");
            CardEffectStore.Initialize(ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/sts2.dll"));
            Check(CardEffectStore.CatalogMatchesGame, "Catalog matches the referenced game DLL");
            var audioConfig = new AK_Exusiai.Settings.AudioSettingsConfig();
            Check(Math.Abs(audioConfig.ApplyToSound("custom:shot_remix", 1) - 0.4f) < 0.00001f, "Default gunshot volume is 40 percent");
            Check(Math.Abs(audioConfig.ApplyToSound("custom:shot_remix", 0.5f) - 0.2f) < 0.00001f, "Gunshot setting multiplies independent card volume");
            Check(audioConfig.ApplyToSound("custom:reload_1911", 0.7f) == 0.7f, "Reload audio keeps its configured volume");
            Check(audioConfig.ApplyToSound("event:event:/sfx/buff", 0.7f) == 0.7f, "Unrelated sound events are unaffected");
            audioConfig.GunshotVolumePercent = 0;
            Check(audioConfig.ApplyToSound("custom:shot_remix", 1) == 0, "Zero percent mutes gunfire");
            audioConfig.GunshotVolumePercent = 100;
            Check(audioConfig.ApplyToSound("custom:shot_remix", 0.7f) == 0.7f, "100 percent restores the original gunshot level");
            audioConfig.GunshotVolumePercent = double.NaN;
            Check(audioConfig.Copy().GunshotVolumePercent == 40, "Invalid stored audio preferences recover to 40 percent");
            foreach (var (input, expected) in new[] { (-1d, 0d), (101d, 100d), (37d, 37d) })
            {
                audioConfig.GunshotVolumePercent = input;
                Check(audioConfig.Copy().GunshotVolumePercent == expected, "Audio preference bounds");
            }
            var savedAudio = JsonSerializer.Deserialize<AK_Exusiai.Settings.AudioSettingsConfig>(JsonSerializer.Serialize(audioConfig));
            Check(savedAudio?.GunshotVolumePercent == 37, "Local gunshot preference JSON round trip");
            foreach (var audio in CardEffectStore.Entries.Values.Where(e => e.Kind == "sfx" && e.Resource.StartsWith("res://AK_Exusiai/audio/card_effects/", StringComparison.Ordinal)))
            {
                CardEffectStore.Validate(new CardEffectConfig { LaunchSound = audio.Id });
                var stream = ResourceLoader.Load<AudioStream>(audio.Resource);
                Check(stream != null && stream.GetLength() > 0, "Custom audio imports and decodes: " + audio.Id);
                GD.Print($"CUSTOM_AUDIO: {audio.Id} duration={stream!.GetLength():F3}s");
            }
            // This fixture is written by Godot's JSON serializer in the UI smoke check.
            var document = CardEffectStore.Read<EffectDocument>("res://tmp/effect_manager_contract.json");
            CardEffectStore.Validate(document);
            var binding = document.Cards["AK_EXUSIAI_CARD_LASER_CANNON"];
            Check(binding.Base.Offset.SequenceEqual(new float[] { 25, -12 }), "Godot/C# coordinates");
            Check(!JsonSerializer.Serialize(binding, CardEffectStore.Json).Contains("upgrade"), "Legacy upgrade is ignored");
            Check(document.Cards["AK_EXUSIAI_CARD_TALENT"].Base.Delay == 0.5f && binding.Base.Delay == 0.5f, "Shared preset changes reach every exported card");
            Check(binding.Base.HitInterval == 0.075f && document.Cards["AK_EXUSIAI_CARD_TALENT"].Base.HitInterval == 0.075f, "Shared interval exported from Godot to C#");
            Check(binding.Base.AnimationOnce && document.Cards["AK_EXUSIAI_CARD_TALENT"].Base.AnimationOnce, "Animation once exported for all shared cards");
            var request = CardEffectStore.Read<CardEffectBridge.Request>("res://tmp/effect_manager_preview_contract.json");
            Check(request.Target == -2 && request.Hits == 2 && request.Config?.Delay == 0.5f, "Prepared F5 request round trip");
            Check(request.Config?.HitInterval == 0.075f && !request.IsAttack, "Prepared F5 interval and card type round trip");
            Check(request.Config?.AnimationOnce == true && !new CardEffectConfig().AnimationOnce, "Animation option round trip and backwards compatible default");
            using (var singleAnimation = new EffectPlayback())
            using (var nativeAnimations = new EffectPlayback())
            using (var nextCard = new EffectPlayback())
            {
                Check(Enumerable.Range(0, 5).Count(_ => singleAnimation.TryStartAnimation(true)) == 1, "Five hits trigger exactly one opted-in character animation");
                Check(Enumerable.Range(0, 5).Count(_ => nativeAnimations.TryStartAnimation(false)) == 5, "Disabling the option preserves repeated character animations");
                Check(nextCard.TryStartAnimation(true), "The next card/replay has its own animation state");
                Check(!singleAnimation.Started, "Animation state never consumes the launch/sound repeat state");
            }
            Check(new CardEffectConfig().HitInterval == -1, "Old configurations preserve native timing");
            foreach (float interval in new[] { -1f, 0f, 0.05f, 0.075f, 0.09f, 5f })
                CardEffectStore.Validate(new CardEffectConfig { HitInterval = interval });
            foreach (float interval in new[] { -2f, -0.5f, -0.01f, 5.001f, float.NaN, float.PositiveInfinity })
                Reject(new() { HitInterval = interval });
            Check(Math.Abs(CardEffectRuntime.RemainingInterval(0.075f, 0.02) - 0.055f) < 0.00001f, "Cadence subtracts effect/damage processing time");
            Check(CardEffectRuntime.RemainingInterval(0.075f, 0.2) == 0, "Slow gameplay hooks never get an additional cadence wait");
            Check(CardEffectRuntime.RemainingInterval(0, 0) == 0, "Zero interval allows fastest normal execution");
            Check(!CardEffectRuntime.TryGetRemainingInterval(null, null, null, ValueProp.Move, out _), "Unscoped damage retains original wait");
            CheckAttackIntervalPatch();
            var recipe = CardEffectPlayer.Resolve(binding.Base);
            Check(recipe.Launch == "factory:NHyperbeamVfx" && recipe.Hit == "factory:NHyperbeamImpactVfx", "Preset resolution");
            var suppressed = CardEffectPlayer.Resolve(new() { Preset = "card:PerfectedStrike", Hit = "none" });
            Check(suppressed.Hit.Length == 0 && suppressed.HitExtras.Length == 0, "Disabling a compound hit clears every layer");
            var fallback = CardEffectPlayer.Resolve(new(), new() { Hit = "scene:vfx/vfx_attack_blunt", Animation = "Cast" });
            Check(fallback.Hit == "scene:vfx/vfx_attack_blunt" && fallback.Animation == "Cast", "Inheritance preserves native recipe");
            Reject(new() { Hit = "factory:missing" });
            Reject(new() { Hit = "event:event:/sfx/characters/attack_fire" });
            Reject(new() { Offset = [1] });
            Reject(new() { Volume = float.NaN });
            Reject(new() { Delay = 6 });
            foreach (var e in CardEffectStore.Entries.Values.Where(e => e.Status != "adapted"))
                if (e.Kind == "sfx") Reject(new() { HitSound = e.Id });
            GD.Print($"EFFECT_CONTRACT: PASS entries={CardEffectStore.Entries.Count}");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr($"EFFECT_CONTRACT: FAIL {e}"); GetTree().Quit(1); }
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidDataException(name);
    }

    private static void CheckAttackIntervalPatch()
    {
        var damage = AccessTools.DeclaredMethod(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
            [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay)]);
        Type stateMachine = damage.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        var moveNext = AccessTools.DeclaredMethod(stateMachine, "MoveNext");
        var replacement = AccessTools.DeclaredMethod(typeof(CardAttackIntervalPatch), nameof(CardAttackIntervalPatch.WaitAfterDamage));
        var originalWait = AccessTools.DeclaredMethod(typeof(Cmd), nameof(Cmd.CustomScaledWait),
            [typeof(float), typeof(float), typeof(bool), typeof(CancellationToken)]);
        var harmony = new Harmony("AK_Exusiai.interval-contract");
        try
        {
            // Apply the actual transpiler to this installed game's real async IL.
            // This checks the state fields, stack shape and precisely one wait site.
            harmony.Patch(moveNext, transpiler: new HarmonyMethod(typeof(CardAttackIntervalPatch), nameof(CardAttackIntervalPatch.Transpiler)));
            var code = PatchProcessor.GetCurrentInstructions(moveNext);
            Check(code.Count(i => i.Calls(replacement)) == 1 && code.All(i => !i.Calls(originalWait)), "Exactly one final damage wait redirected");
            var repeated = CardAttackIntervalPatch.Transpiler(code, moveNext).ToArray();
            Check(repeated.Count(i => i.Calls(replacement)) == 1, "Async interval patch is idempotent");
            GD.Print("ATTACK_INTERVAL_PATCH: PASS; installed game async IL; exactly one wait; idempotent");
        }
        finally { harmony.Unpatch(moveNext, HarmonyPatchType.All, harmony.Id); }
    }

    private static void Reject(CardEffectConfig config)
    {
        try { CardEffectStore.Validate(config); }
        catch (InvalidDataException) { return; }
        throw new InvalidDataException("Invalid config accepted: " + JsonSerializer.Serialize(config));
    }
}
