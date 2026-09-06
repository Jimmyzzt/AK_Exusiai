using System.Text.Json;
using AK_Exusiai.Effects;
using Godot;

public partial class CardEffectChecks : Node
{
    public override void _Ready()
    {
        try
        {
            CardEffectStore.Initialize(ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/sts2.dll"));
            Check(CardEffectStore.CatalogMatchesGame, "Catalog matches the referenced game DLL");
            // This fixture is written by Godot's JSON serializer in the UI smoke check.
            var document = CardEffectStore.Read<EffectDocument>("res://tmp/effect_manager_contract.json");
            CardEffectStore.Validate(document);
            var binding = document.Cards["AK_EXUSIAI_CARD_LASER_CANNON"];
            Check(binding.Base.Offset.SequenceEqual(new float[] { 25, -12 }), "Godot/C# coordinates");
            Check(!JsonSerializer.Serialize(binding, CardEffectStore.Json).Contains("upgrade"), "Legacy upgrade is ignored");
            Check(document.Cards["AK_EXUSIAI_CARD_TALENT"].Base.Delay == 0.5f && binding.Base.Delay == 0.5f, "Shared preset changes reach every exported card");
            var request = CardEffectStore.Read<CardEffectBridge.Request>("res://tmp/effect_manager_preview_contract.json");
            Check(request.Target == -2 && request.Hits == 2 && request.Config?.Delay == 0.5f, "Prepared F5 request round trip");
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

    private static void Reject(CardEffectConfig config)
    {
        try { CardEffectStore.Validate(config); }
        catch (InvalidDataException) { return; }
        throw new InvalidDataException("Invalid config accepted: " + JsonSerializer.Serialize(config));
    }
}
