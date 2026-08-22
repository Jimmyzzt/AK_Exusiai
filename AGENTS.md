# AK_Exusiai 开发指南

本文件适用于整个仓库。新对话不得依赖历史聊天或仓库外文件；开始工作前同时阅读 `docs/PROGRESS.md`，卡图任务再读 `docs/CARD_ART_HANDOFF.md`。

## 当前基线与事实来源

- Mod ID、程序集名、PCK 名：`AK_Exusiai`。
- 游戏：Slay the Spire 2 public beta `v0.111.0`。
- Godot：4.5.1 Mono；.NET：9.x；RitsuLib：0.5.14。
- 设计唯一来源：`docs/AK_Exusiai-Card.csv`。实现任务不得机械改写设计表。
- 代码现状：20 张普通牌已回归；35 张罕见牌、25 张稀有牌及相关衍生牌已完成首版，仍需集中测试。
- 任何公开 ID、类名、资源文件名或 Mod ID 的重命名都必须先确认，因为会影响控制台 ID 与存档兼容性。

API 判断优先级：当前游戏 `sts2.dll` > 当前仓库已验证代码 > RitsuLib 0.5.14 文档/XML > 社区教程。

## 目录

- `AK_ExusiaiCode/Cards/`：卡牌；一张牌一个主类。
- `AK_ExusiaiCode/Powers/`：能力与持续状态。
- `AK_ExusiaiCode/Mechanics/`：弹药、天使、快递等共享机制。
- `AK_ExusiaiCode/Patches/`：没有合适 Hook 时才使用的窄范围补丁。
- `AK_Exusiai/`：PCK 实际加载的场景、图片和本地化。
- `references/official/`、`references/free/`：源素材与来源记录。
- `docs/PROGRESS.md`：只记录当前结果、待测项和下一步，不保留流水账。

## 构建与依赖

本机路径只写入未跟踪的 `local.props`。从 `local.props.template` 创建后执行：

```powershell
dotnet build .\AK_Exusiai.csproj
```

模板已配置 `RitsuLibDeployDir=$(Sts2Dir)\mods\STS2-RitsuLib\`，完整构建会从 NuGet 包自动部署匹配的 0.5.14 运行时。若启动报告缺少依赖，先检查并补齐该目录，再继续测试；不要把游戏 DLL 或部署产物提交到仓库。

只做 C# 检查：

```powershell
dotnet build .\AK_Exusiai.csproj /p:RunPckExport=false /p:CopyModOnBuild=false
```

完整构建前退出游戏，避免 DLL 被锁定。完成时至少确认 0 错误、PCK 包含新增资源、部署 DLL 哈希一致，并通过 Steam 启动检查 Mod 注册和主菜单加载。

## 已验证的实现经验

- 初始化必须保留 Godot 脚本注册、程序集类型发现、玩法补丁和战斗 Hook 注册。
- 普通机制优先复用原版命令/Hook；Harmony 仅用于没有替代的调用点，异步补丁必须校验目标数量和时序。
- 可升级数值使用 `DynamicVar`、`DamageVar`、`EnergyVar`、`PowerVar<T>`；伤害预览使用 `{Damage:diff()}`，能量使用 `{Energy:energyIcons()}`。
- `PowerVar<T>` 的文本键通常是完整类型名，如 `WeakPower`。任一变量名错误都可能让整条描述保留原始占位符。
- 固定关键词放入 `CanonicalKeywords`；规范模型构造阶段不要调用要求可变实例的 `AddKeyword`/`RemoveKeyword`。
- Token 衍生牌使用 `CardRarity.Token` 和 `TokenCardPool`，不要进入普通无色奖励或商店。
- 弹药：有弹药时，每次实际打出攻击牌消耗 1 发，并让该次出牌的每段 Powered Attack 伤害 +2；Replay 分别结算。延时伤害应保存打出时的最终值，触发时不重新计算增伤。
- 天使：本场战斗内费用不会高于其曾达到的最低值；身份判断检查能力组件，避免默认能力与全局费用 Hook 重复计算。
- 快递：运行时层数变化统一经过可等待的 `DeliveryCmd`；规范模型初始层数无触发，层数变化效果按变化量结算。
- 卡面伤害预览必须包含已存在的临时能力，以及本牌会在伤害前创建的额外增益；预览和实际伤害分别测试。
- 逐次消耗人工制品的多层减益必须逐次调用 `PowerCmd.Apply`，不能先合并总数。
- 能力图标接口加载静态纹理；GIF 不会自动成为逐帧图标。动画图标需要单独的 Godot 场景/UI 实现。
- `*.import` 是被 Git 忽略的可再生元数据。提交 PNG/JPG/SVG 源文件，并用完整构建日志中的 `reimport` 与 `savepack` 确认导入和打包。

## 卡牌与资源约定

- C# 类名、文件名、英文名和本地化公开键保持一一对应。
- 运行时资源只引用 `res://AK_Exusiai/...`，不得引用 `references/`。
- 正式卡图放在 `AK_Exusiai/images/cards/<ClassName>.png`；图片不含卡框、费用、标题、规则文字或水印。
- 能力图标放在 `AK_Exusiai/images/powers/`，透明背景且在 32–64 px 下可辨认。
- 源素材保留在 `references/`；使用过的源素材和最终资源都提交，并更新对应 `SOURCES.md`。
- 不提交整套游戏资源、其他 Mod 素材、DLL/PCK、缓存、日志、绝对路径或凭据。

## 测试与协作

卡牌测试至少覆盖基础/升级、0 与多发弹药、单段/多段/AOE、Replay、自动打出、消耗、抽弃耗各区域，以及同名能力叠加。高风险回归清单以 `docs/PROGRESS.md` 为准。

在功能分支开发，避免双方同时修改 `Entry.cs`、项目文件、角色注册和本地化 JSON。完成实质工作后更新 `docs/PROGRESS.md`，记录当前结果、验证命令和仍需人工确认的内容。提交应保持可构建，并保留用户已有的无关改动。
