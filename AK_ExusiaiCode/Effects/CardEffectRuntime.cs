using System.Runtime.CompilerServices;
using AK_Exusiai.Cards;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace AK_Exusiai.Effects;

internal static class CardEffectRuntime
{
    private sealed class PlayState(CardEffectConfig config)
    {
        internal readonly CardEffectConfig Config = config;
        internal readonly EffectPlayback Playback = new();
    }
    private static readonly ConditionalWeakTable<CardPlay, PlayState> States = new();

    internal static async Task BeforeCard(CardPlay play)
    {
        if (play.Card is not ExusiaiCardTemplate) return;
        EffectBinding? binding = CardEffectStore.Find(play.Card.Id.Entry, play.Player.RunState.Players.Count == 1);
        if (binding == null) return;
        // Snapshot once per CardPlay; hot reload never mutates an in-flight play.
        var config = play.Card.IsUpgraded ? binding.Upgrade ?? binding.Base : binding.Base;
        States.Remove(play);
        var state = new PlayState(config);
        States.Add(play, state);
        if (play.Card.Type != CardType.Attack && config.Phase == "start") await PlayNonAttack(play, state);
    }

    internal static async Task AfterCard(CardPlay play)
    {
        if (!States.TryGetValue(play, out var state)) return;
        try
        {
            if (play.Card.Type != CardType.Attack && state.Config.Phase == "end") await PlayNonAttack(play, state);
        }
        finally { States.Remove(play); }
        // Nodes finish their own visual tails; do not cut off the effect when the card leaves play.
    }

    private static async Task PlayNonAttack(CardPlay play, PlayState state)
    {
        try { await CardEffectPlayer.Play(state.Playback, state.Config, CardEffectPlayer.Resolve(state.Config, new() { Animation = "" }), play.Player.Creature, [play.Target ?? play.Player.Creature], animate: state.Config.Animation != "inherit"); }
        catch (Exception e) { Entry.Logger.Warn($"Card effect skipped: {e.Message}"); }
    }

    internal static void Configure(AttackCommand command)
    {
        if (command.CardPlay == null || !States.TryGetValue(command.CardPlay, out var state)) return;
        if (command.ModelSource is not ExusiaiCardTemplate || command.Attacker != command.CardPlay.Player.Creature) return;
        string Visual(string? s) => s == null ? "" : "scene:" + s;
        string Audio(string? s, string? t) => s != null ? "event:" + s : t != null ? "audio:" + t : "";
        var fallback = new EffectRecipe
        {
            Launch = Visual(command._attackerVfx), Hit = Visual(command.HitVfx),
            LaunchSound = Audio(command._attackerSfx, command._tmpAttackerSfx), HitSound = Audio(command.HitSfx, command.TmpHitSfx),
            Animation = command._shouldPlayAnimation ? command._attackerAnimName ?? "" : "",
        };
        EffectRecipe recipe = CardEffectPlayer.Resolve(state.Config, fallback);
        // Capture the actual target chosen by AttackCommand, including random targeting.
        // Never draw another RNG value for a visual effect.
        List<Creature> actualTargets = [];
        command.WithHitVfxNode(target => { actualTargets.Add(target); return null; });
        command.WithHitFx().WithAttackerFx().WithNoAttackerAnim();
        Func<Task>? original = command._beforeDamage;
        command.BeforeDamage(async () =>
        {
            try { await CardEffectPlayer.Play(state.Playback, state.Config, recipe, command.Attacker!, actualTargets.ToArray()); }
            catch (Exception e) { Entry.Logger.Warn($"Attack effect skipped: {e.Message}"); }
            finally { actualTargets.Clear(); }
            // Always execute the original callback, including ammunition consumption.
            if (original != null) await original();
        });
    }
}
