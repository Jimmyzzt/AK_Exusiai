# AK_Exusiai

《Slay the Spire 2》能天使角色 Mod 的协作开发仓库。角色原型来自《明日方舟》的能天使。

当前处于基础设施与设计审阅阶段，尚未开始批量实现卡牌。

## 当前内容

- `Exusiai_Cardlist.xlsx`：设计表，共 97 张卡牌设计。
  - 2 张初始卡；
  - 80 张常规牌（20 普通、35 罕见、25 稀有）；
  - 13 张衍生牌；
  - 2 张先古牌。
- 2 件遗物设计：初始遗物与先古遗物。
- `AGENTS.md`：两边 Codex 都必须先读的开发规则与经验。
- `docs/PROGRESS.md`：双方任务、所有权和进度账本。
- `docs/FRAMEWORK_AUDIT.md`：当前游戏测试版与框架兼容性核查。
- `references/official/`：双方开发需要的官方美术、音效源素材及来源记录。

## 当前技术基线

- 游戏分支：public beta `v0.111.0`。
- Godot：`4.5.1` Mono。
- .NET：`9.0`。
- RitsuLib：`0.5.14`，用于游戏 API `0.111.0`。

每次游戏或 RitsuLib 更新后，先按 `docs/FRAMEWORK_AUDIT.md` 重新验证，再修改版本基线。

## 协作方式

1. 克隆仓库后先阅读 `AGENTS.md` 和 `docs/PROGRESS.md`。
2. 在 `docs/PROGRESS.md` 中认领任务；同一张卡、同一个共享注册文件不要由两边同时修改。
3. 从 `main` 创建短分支，推荐命名：`card/<class-name>`、`mechanic/<name>`、`art/<asset>`、`docs/<topic>`。
4. 一个 PR 只处理一个机制或一组高度相关的文件，并写清基础版、升级版和边界测试结果。
5. 合并后立即更新进度账本。

## 素材与非盈利约定

这是非官方、非盈利的同人二次创作项目，与 Mega Crit、Hypergryph 或 Yostar 无隶属或背书关系。项目维护者已确认会在相关权利方允许的非盈利二次创作范围内使用官方美术与音效。

为保证两位开发者可以仅凭本仓库继续工作，项目实际需要的选定官方源素材和最终资源都应提交到 Git，并在 `references/official/SOURCES.md` 记录来源、用途和修改。不要提交整套游戏、可执行文件、DLL/PCK、无关批量提取内容或其他 Mod 的素材。代码许可尚未确定，因此仓库暂不附加开源许可证。
