using AK_Exusiai.Effects;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Players;

namespace AK_Exusiai.Debug;

internal static class ExusiaiEffectConsoleCmd
{
    internal static CmdResult Process(Player? issuingPlayer, string[] args)
    {
        // The parent command is networked for gameplay fixtures; FX never acts in multiplayer.
        if (issuingPlayer?.RunState.Players.Count > 1)
            return new(false, "特效试播仅支持单人战斗。");
        if (args.Length > 1)
            return new(false, "用法：exusiai fx <on|off|status>");
        try
        {
            switch (args.FirstOrDefault()?.ToLowerInvariant())
            {
                case "on":
                    CardEffectBridge.Enable(issuingPlayer ?? throw new InvalidOperationException("请先进入单人战斗。"));
                    return new(true, "特效试播已启用，打开 tools/card_effect_manager 管理器。");
                case "off": CardEffectBridge.Instance?.QueueFree(); return new(true, "特效试播已关闭。");
                case null:
                case "status": return new(true, CardEffectBridge.Instance == null ? "未连接。单人战斗中运行 exusiai fx on。" : "本地特效试播已连接。");
                default: return new(false, "用法：exusiai fx <on|off|status>");
            }
        }
        catch (Exception e) { return new(false, e.Message); }
    }
}
