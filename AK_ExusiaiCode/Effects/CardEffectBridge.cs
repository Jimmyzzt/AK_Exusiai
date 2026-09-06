using System.Text.Json;
using AK_Exusiai.Characters;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace AK_Exusiai.Effects;

/// <summary>Explicitly enabled, single-player, visual-only development bridge.</summary>
public partial class CardEffectBridge : Node
{
    internal static CardEffectBridge? Instance { get; private set; }
    private Player _player = null!;
    private string _lastRequest = "";
    private string _message = "等待管理器请求";
    private string _state = "idle";
    private string _overrideText = "";
    private double _poll;
    private EffectPlayback? _playback;

    internal static void Enable(Player player)
    {
        if (!CardEffectStore.CatalogMatchesGame)
            throw new InvalidOperationException("游戏已更新，请重建特效目录并重新构建 Mod 后试播。");
        if (player.Character is not Exusiai || player.RunState.Players.Count != 1 || NCombatRoom.Instance == null)
            throw new InvalidOperationException("请在能天使单人战斗中启用试播。");
        if (Instance != null && GodotObject.IsInstanceValid(Instance)) Instance.QueueFree();
        var bridge = new CardEffectBridge { _player = player };
        NCombatRoom.Instance.AddChild(bridge);
    }

    public override void _Ready()
    {
        Instance = this;
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(CardEffectStore.BridgeDirectory));
        // Do not replay stale commands when re-enabling a combat session.
        string request = CardEffectStore.BridgeDirectory + "/request.json";
        if (Godot.FileAccess.FileExists(request))
        {
            try { _lastRequest = CardEffectStore.Read<Request>(request).Id; } catch { /* stale/incomplete request */ }
        }
        WriteStatus();
    }

    public override void _Process(double delta)
    {
        _poll += delta;
        if (_poll < 0.2) return;
        _poll = 0;
        if (_player.RunState.Players.Count != 1 || !CombatManager.Instance.IsInProgress)
        {
            QueueFree();
            return;
        }
        try
        {
            if (_state != "playing") ReloadOverride();
            string path = CardEffectStore.BridgeDirectory + "/request.json";
            if (Godot.FileAccess.FileExists(path))
            {
                Request request = CardEffectStore.Read<Request>(path);
                if (request.Id.Length > 0 && request.Id != _lastRequest)
                {
                    _lastRequest = request.Id;
                    if (request.Action == "stop") Stop();
                    else if (request.Action == "audit") AuditResources();
                    else if (request.Action == "play") TaskHelper.RunSafely(Play(request));
                    else throw new InvalidDataException("Unknown preview action.");
                }
            }
        }
        catch (Exception e) { _state = "error"; _message = e.Message; }
        WriteStatus();
    }

    private void ReloadOverride()
    {
        string path = CardEffectStore.BridgeDirectory + "/override.json";
        if (!Godot.FileAccess.FileExists(path)) return;
        string text = Godot.FileAccess.GetFileAsString(path);
        if (text == _overrideText) return;
        EffectDocument doc = JsonSerializer.Deserialize<EffectDocument>(text, CardEffectStore.Json) ?? throw new InvalidDataException("Empty override.");
        CardEffectStore.Validate(doc);
        CardEffectStore.Override = doc;
        _overrideText = text;
        _message = "开发配置已热重载";
    }

    private async Task Play(Request request)
    {
        Stop();
        var session = new EffectPlayback();
        _playback = session;
        _state = "playing";
        _message = "试播中（不结算伤害或弹药）";
        WriteStatus();
        try
        {
            CardEffectConfig config = request.Config ?? new();
            CardEffectStore.Validate(config);
            var enemies = _player.Creature.CombatState!.HittableEnemies.ToList();
            IReadOnlyList<Creature> targets = request.Target == -2 ? [_player.Creature] : request.Target == -1 ? enemies :
                request.Target >= 0 && request.Target < enemies.Count ? [enemies[request.Target]] : throw new InvalidDataException("目标已失效，请刷新目标列表。");
            if (request.Entry.Length > 0)
            {
                if (!CardEffectStore.Entries.TryGetValue(request.Entry, out var e) || e.Status != "adapted") throw new InvalidDataException("条目尚未适配。");
                if (e.Kind == "sfx") CardEffectPlayer.Sound(e.Id, config.Volume, session);
                else if (e.Kind == "vfx") CardEffectPlayer.Spawn(e.Id, _player.Creature, targets, config, session, request.Slot == "launch");
                else { config.Preset = e.Id; await CardEffectPlayer.Play(session, config, CardEffectPlayer.Resolve(config), _player.Creature, targets); }
            }
            else
            {
                for (int i = 0; i < Math.Clamp(request.Hits, 1, 12); i++)
                {
                    await CardEffectPlayer.Play(session, config, CardEffectPlayer.Resolve(config), _player.Creature, targets);
                    if (i + 1 < request.Hits) await MegaCrit.Sts2.Core.Commands.Cmd.Wait(0.15f, session.Cancellation.Token);
                }
            }
            // Keep Stop usable while particles/audio finish.
            await MegaCrit.Sts2.Core.Commands.Cmd.Wait(4f, session.Cancellation.Token);
            if (_playback == session) { _state = "done"; _message = "试播完成；可标记本机验证结果"; }
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { if (_playback == session) { _state = "error"; _message = e.Message; } Entry.Logger.Warn($"Effect preview: {e}"); }
        finally { if (_playback == session && GodotObject.IsInstanceValid(this)) WriteStatus(); }
    }

    internal void Stop()
    {
        _playback?.Dispose();
        _playback = null;
        _state = "idle";
        _message = "试播已停止";
    }

    private void AuditResources()
    {
        var results = CardEffectStore.Entries.Values.Where(e => e.Status == "adapted" && e.Kind != "preset")
            .Select(e => new { e.Id, Exists = e.Adapter == "event" ? (bool?)null : ResourceLoader.Exists(e.Adapter == "audio" ? e.Resource : SceneHelper.GetScenePath(e.Resource)) }).ToArray();
        WriteJson("resource_audit.json", new { game_sha256 = CardEffectStore.GameHash, results });
        _message = $"资源检查完成：{results.Count(x => x.Exists == false)} 项缺失；音频事件仍需试播确认";
        _state = "done";
    }

    private void WriteStatus()
    {
        var enemies = _player.Creature.CombatState?.HittableEnemies.Select((x, i) => new { index = i, name = x.ModelId.Entry }).ToArray();
        WriteJson("status.json", new { enabled = true, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), request_id = _lastRequest, state = _state, message = _message, game_sha256 = CardEffectStore.GameHash, enemies, appearance = $"{ExusiaiAppearanceManager.SelectedCharacterIndex}:{ExusiaiAppearanceManager.SelectedSkinIndex}" });
    }

    private static void WriteJson(string name, object value)
    {
        string path = ProjectSettings.GlobalizePath(CardEffectStore.BridgeDirectory + "/" + name);
        System.IO.File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(value, CardEffectStore.Json));
        System.IO.File.Move(path + ".tmp", path, true);
    }

    public override void _ExitTree()
    {
        Stop();
        if (Instance == this)
        {
            Instance = null;
            CardEffectStore.Override = null;
            WriteJson("status.json", new { enabled = false, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), state = "offline", message = "战斗已结束或试播已关闭" });
        }
    }

    public sealed class Request
    {
        public string Id { get; set; } = "";
        public string Action { get; set; } = "play";
        public string Entry { get; set; } = "";
        public string Slot { get; set; } = "hit";
        public int Target { get; set; } = -1;
        public int Hits { get; set; } = 1;
        public CardEffectConfig? Config { get; set; }
    }
}
