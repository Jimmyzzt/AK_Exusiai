# AK_Exusiai 协作开发指南

本文件适用于本仓库全部任务。两位开发者使用 Codex 时，都应在修改前完整读取本文件，并以本机当前游戏程序集、当前 RitsuLib 包和仓库中的设计表为准。

## 项目身份

- 暂定 Mod ID、程序集名与 PCK 名：`AK_Exusiai`。
- 角色原型：《明日方舟》能天使（Exusiai）。
- 中文与英文展示名、作者字段尚未最终确认；不要擅自把占位值发布为正式名称。
- 设计表：`Exusiai_Cardlist.xlsx`，不得覆盖或机械改写。
- 当前阶段：基础设施、机制拆分与原型验证。

任何会改变公开 ID、类名、资源文件名、Mod ID 或存档兼容性的重命名，必须先由双方确认。

## 当前工具链基线

- 游戏：Slay the Spire 2 public beta `v0.111.0`。
- 游戏程序集：`D:\Apps\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll`。
- Godot：4.5.1 Mono。
- .NET SDK：9.x，目标框架 `net9.0`。
- 新项目 RitsuLib：`STS2.RitsuLib` 0.5.14；该包明确包含游戏 API `0.111.0` 目标。
- 旧项目 `DpxVanillaExpansion` 的 RitsuLib 0.5.11 仅作已验证经验参考，不应成为本项目默认版本。

游戏、NuGet 编译包和游戏 `mods/STS2-RitsuLib` 运行时必须匹配。只通过编译不能替代进游戏测试。版本核查见 `docs/FRAMEWORK_AUDIT.md`。

## 预期目录

- `AK_ExusiaiCode/`：C# 玩法代码。
- `AK_Exusiai/`：PCK 资源、本地化、图片和场景。
- `AK_ExusiaiCode/Cards/`：卡牌类，一张卡一个主文件。
- `AK_ExusiaiCode/Powers/`：能力与持续状态。
- `AK_ExusiaiCode/Patches/`：确有必要时才放 Harmony/RitsuLib patch。
- `AK_Exusiai/localization/zhs/` 与 `eng/`：简中和英文文本。
- `AK_Exusiai/images/cards/`：无卡框、费用、标题和规则文字的卡图。
- `AK_Exusiai/images/powers/`：透明背景、32–64 px 仍清晰的能力图标。
- `docs/`：设计决策、框架审计和协作进度。

本地安装路径写入未跟踪的 `local.props`，不得提交绝对路径、日志、游戏 DLL、PCK、密钥或个人凭据。

## 从 DpxVanillaExpansion 继承的有效经验

1. 具体 API 永远以当前本机 `sts2.dll` 的反编译结果为准；早期教程只用于定位思路。
2. 注册与资源能力优先使用 RitsuLib；普通机制优先组合原版命令、Hook 和模型。
3. 只有没有合适 Hook 时才使用 Harmony。异步逻辑采用窄范围 patch，并明确校验调用点数量，避免更新后静默误补丁。
4. 入口初始化必须同时完成 Godot 脚本注册和 Mod 程序集类型发现；具体 API 名称以 0.5.14 文档为准。
5. 卡牌可升级数值使用 `DynamicVar`、`DamageVar`、`PowerVar<T>` 等动态变量，并在本地化中引用占位符，不把升级数值硬编码进描述。
6. 资源只从本 Mod 的 `res://AK_Exusiai/...` 根路径加载，不从工作区参考目录直接加载。
7. 游戏运行时可能锁定 DLL；完整构建与部署前先退出游戏。只做 C# 验证时关闭 PCK 导出和部署。
8. 加载失败时先查缺失依赖、Mod ID、本地化键、资源路径和 patch 报告，不先猜机制代码。

## 设计表事实与待澄清项

设计表包含：2 张初始卡、80 张常规牌、13 张衍生牌、2 张先古牌，以及 2 件遗物。核心机制至少包括：

- 弹药：攻击牌消耗弹药并获得伤害加成；
- 快递：获得保留与消耗，倒计时归零时自动打出；
- 天使：耗能不会增加，并与生成、消耗、重复打出联动；
- 物流卡：特定衍生牌池和权重；
- 混乱、诅咒联动、敌人特殊能力失效、额外回合等复杂机制。

表中括号通常表示升级后变化，但也有“升级移除关键词”“耗能降低”“X 费”“只对一个敌人生效”等特殊写法。开始实现前必须逐张确认基础版、升级版、目标类型、触发时序和边界条件。有两种合理解释时停止编码并在 Issue/PR 中提出问题。

在当前检查工具的渲染中，`统计及修改!G2:J5` 显示 `#NAME?`。这可能来自公式兼容性或原公式写法；在桌面 Excel 中复核前，不要据此改动设计表。

## 命名与公开 ID

- C# 类使用正确的 PascalCase 英文名；英文名未定时先在 Issue 中确认，不用拼音或临时直译直接形成公开 ID。
- 建议卡牌键：`AK_EXUSIAI_CARD_<UPPER_SNAKE_CLASS>.title/description/smartDescription`。
- 建议能力键：`AK_EXUSIAI_POWER_<UPPER_SNAKE_CLASS>.title/description/smartDescription`。
- 遗物、药水、关键词和附魔使用同一前缀规则。
- 展示文本使用自然语言；类名、文件名和本地化键保持一一对应。

## 实现顺序

不要直接按表格顺序并行实现 97 张卡。建议按依赖关系推进：

1. 建立可加载的空角色、卡池、基础牌与初始遗物。
2. 实现并测试“弹药”资源及其 UI、伤害修正和消耗时序。
3. 实现“快递”卡牌状态、回合结束倒计时、自动打出与目标选择策略。
4. 实现“天使”标签/关键词及耗能保护。
5. 实现物流衍生牌池、权重和选择界面。
6. 再处理混乱、诅咒、敌人能力失效、额外回合、批量自动打牌等高风险机制。
7. 机制稳定后批量接入普通卡，最后实现先古牌和高耦合稀有牌。

每个机制先做一张最小原型卡，再扩展到使用该机制的整组卡。

## 一张卡的完成标准

1. 对照设计表确认费用、类型、稀有度、颜色、基础与升级差异。
2. 在当前 `sts2.dll` 中寻找最接近的原版卡、命令、能力和 Hook。
3. 实现 C# 类与必要的能力/命令，不复制其他 Mod 的大段代码或美术。
4. 添加简中、英文文本；英文设计名缺失时不得擅自定稿。
5. 添加最终资源或明确的临时占位资源；占位资源不能进入发布包。
6. 完成只编译检查、PCK 内容检查和游戏内基础/升级/边界测试。
7. 在 PR 中记录测试命令、结果和已知限制，更新 `docs/PROGRESS.md`。

## 协作与 Git 规则

- `main` 保持可构建、可加载；不直接在 `main` 上开发新机制。
- 分支名使用 `card/<name>`、`mechanic/<name>`、`art/<name>`、`docs/<name>` 或 `fix/<name>`。
- 开工前在 `docs/PROGRESS.md` 认领任务。状态使用：`待办`、`进行中`、`待审阅`、`已完成`、`阻塞`。
- 按卡牌拆分文件；避免两边同时编辑 `Entry.cs`、项目文件、清单、`cards.json`、`powers.json` 和角色注册文件。
- 共享文件由当期“集成负责人”修改；另一边在独立文件中完成实现并在 PR 描述中列出需要集成的注册项。
- 提交信息推荐 Conventional Commits，例如 `feat(ammo): add ammo resource model`、`docs: update collaborator progress`。
- 合并优先使用 PR；至少由另一边或其 Codex 做一次代码审阅。
- 不提交 `bin/`、`obj/`、`.godot/`、部署产物、游戏程序集、日志、缓存和本地绝对路径。

## 测试要求

基础测试至少覆盖：

- 基础版与升级版；
- 0、1、上限附近和超过上限的弹药；
- 单段、多段、AOE、随机目标、击杀中断；
- 快递层数增加、减少、归零、保留、消耗、手牌满和无合法目标；
- 天使牌的耗能增加免疫、消耗和重复打出；
- 多个同名能力叠加、多个玩家和回放/复制效果；
- 弃牌堆、抽牌堆、消耗堆、战斗结束与存档加载；
- 游戏更新后 patch 目标数量与签名是否变化。

完整发布验证至少确认：0 个编译错误；PCK 包含新增图片与本地化；部署 DLL/PCK/JSON 版本一致；游戏中实际加载的 RitsuLib 运行时与编译包兼容。

## 参考资料优先顺序

1. 本仓库已经构建并在当前游戏版本中测试成功的代码。
2. 本机当前 `sts2.dll` 与游戏附带 XML 文档。
3. 本机 RitsuLib 0.5.14 的 README、XML 文档和 DLL。
4. RitsuLib 官方文档与仓库：
   - https://sts2-ritsulib.ritsukage.com/guide/getting-started
   - https://sts2-ritsulib.ritsukage.com/guide/content-authoring-toolkit
   - https://sts2-ritsulib.ritsukage.com/guide/patching-guide
   - https://github.com/BAKAOLC/STS2-RitsuLib
5. 持续更新的社区教程：https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials
6. 工作区旧项目 `../DpxVanillaExpansion/`，仅用于学习已验证模式，不复制其项目身份或无关机制。

## 进度同步

每次 Codex 完成实质工作后必须更新 `docs/PROGRESS.md`：记录负责人、分支/PR、涉及文件、验证结果、下一步和阻塞项。只更新自己认领的任务行；合并冲突时保留双方记录，由集成负责人统一整理“当前里程碑”。
