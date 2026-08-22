# 游戏与框架基线

当前组合：Slay the Spire 2 public beta `v0.111.0`、Godot 4.5.1 Mono、.NET 9、`STS2.RitsuLib` 0.5.14。RitsuLib 的编译包、`mods/STS2-RitsuLib` 运行时和游戏 API 必须一致。

升级游戏或框架时：

1. 记录游戏版本与 `sts2.dll` 哈希。
2. 确认 RitsuLib 包明确支持该游戏 API，再在独立分支升级。
3. 只编译检查后执行完整 PCK 构建和 Steam 启动检查。
4. 核对自动注册、资源、本地化和补丁目标数量。
5. 回归弹药、快递、天使费用、延时伤害、Replay、自动出牌和额外回合。

参考：[RitsuLib 文档](https://sts2-ritsulib.ritsukage.com/)、[RitsuLib 仓库](https://github.com/BAKAOLC/STS2-RitsuLib)、[社区教程](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials)。具体 API 仍以当前游戏程序集为准。
