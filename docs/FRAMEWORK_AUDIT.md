# 游戏与框架基线

当前组合：Slay the Spire 2 public beta `v0.111.0`、Godot 4.5.1 Mono、.NET 9、`STS2.RitsuLib` 0.5.17。RitsuLib 的编译包、`mods/STS2-RitsuLib` 运行时和游戏 API 必须一致。

2026-08-28 从 0.5.14 升级到 0.5.17。该版本仍以游戏 API 0.111.0 为最新目标；累计修复包括模型能力升级/存档恢复、虚拟本地化、多人回滚及额外手牌初始化问题。`AK_Exusiai` 不直接使用额外手牌，但依赖模型能力和虚拟本地化，因此需要回归天使、快递、百科与多人状态同步。

升级游戏或框架时：

1. 记录游戏版本与 `sts2.dll` 哈希。
2. 确认 RitsuLib 包明确支持该游戏 API，再在独立分支升级。
3. 只编译检查后执行完整 PCK 构建和 Steam 启动检查。
4. 核对自动注册、资源、本地化和补丁目标数量。
5. 回归弹药、快递、天使费用、延时伤害、Replay、自动出牌和额外回合。

参考：[RitsuLib 文档](https://sts2-ritsulib.ritsukage.com/)、[RitsuLib 仓库](https://github.com/BAKAOLC/STS2-RitsuLib)、[社区教程](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials)。具体 API 仍以当前游戏程序集为准。
