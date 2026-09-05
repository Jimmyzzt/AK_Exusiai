using AK_Exusiai.Effects;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace AK_Exusiai.Debug;

public sealed class ExusiaiEffectConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "exusiaifx";
    public override string Args => "<on|off|status>";
    public override string Description => "Enable the local single-player card effect preview bridge.";
    public override bool IsNetworked => false;
    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        try
        {
            switch (args.FirstOrDefault()?.ToLowerInvariant())
            {
                case "on":
                    CardEffectBridge.Enable(issuingPlayer ?? throw new InvalidOperationException("请先进入单人战斗。"));
                    return new(true, "特效试播已启用，打开 tools/card_effect_manager 管理器。");
                case "off": CardEffectBridge.Instance?.QueueFree(); return new(true, "特效试播已关闭。");
                default: return new(true, CardEffectBridge.Instance == null ? "未连接。单人战斗中运行 exusiaifx on。" : "本地特效试播已连接。");
            }
        }
        catch (Exception e) { return new(false, e.Message); }
    }
}
