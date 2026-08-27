# AK_Exusiai 协作开发指南

本文件适用于仓库内全部任务。新 agent 先读本页，再按任务查阅 `docs/PROGRESS.md`、设计 CSV 和相关源码；不得依赖历史聊天、仓库外文件或另一位开发者的本机目录。具体游戏 API 以开发者当前安装的 `sts2.dll` 为准。

## 1. 项目速览

- Mod ID、程序集名、PCK 名：`AK_Exusiai`。
- 角色：能天使 / Exusiai；初始最大生命 77，初始金币 99。
- 当前卡牌设计：`docs/AK_Exusiai-Card_V0.1.csv`；旧版与平衡调整记录位于 `docs/archive/`。
- 遗物与药水设计：`docs/AK_Exusiai-Relic_Potion.csv`。
- 当前状态：角色、弹药、初始牌、9 个角色遗物、3 个角色药水、20 张普通牌、35 张罕见牌、25 张稀有牌及相关衍生牌已有实现；普通牌完成过回归，其余内容仍需集中测试。正式卡图尚未批量接入。
- 当前进度和待测项只维护在 `docs/PROGRESS.md`，不要在本页追加流水账。

设计 CSV 由双方在线定稿后导出。实现任务应只做必要的名称/稀有度一致性修正，不机械重排或覆盖设计内容。英文展示名决定默认类名和公开 ID；任何改名都必须同步类、文件、本地化键、关联能力/资源名，并全仓清除旧 ID。

## 2. 工具链与构建

- 游戏基线：Slay the Spire 2 public beta `v0.111.0`。
- Godot：4.5.1 Mono。
- .NET SDK：9.x；目标框架 `net9.0`。
- RitsuLib：`STS2.RitsuLib` 0.5.14，对应游戏 API 0.111.0。
- 各自通过未跟踪的 `local.props` 配置游戏、Godot 和部署目录；模板见 `local.props.template`。共享文件不得包含绝对路径。

游戏、NuGet 编译包和游戏中部署的 RitsuLib 运行时必须匹配。不要仅因线上出现新版本就升级；先核对游戏 API、NuGet 包和运行时，再单独升级并回归。审计记录见 `docs/FRAMEWORK_AUDIT.md`。

游戏可能锁定 Mod DLL，完整构建前先退出游戏：

```powershell
dotnet build .\AK_Exusiai.csproj
```

只检查 C#，不导出 PCK、不部署：

```powershell
dotnet build .\AK_Exusiai.csproj /p:RunPckExport=false /p:CopyModOnBuild=false
```

完整验证至少确认：0 个编译错误；PCK 中包含新增本地化和资源；本地输出与部署 DLL/PCK/JSON 一致；启动日志中 Mod 初始化、自动注册和 `ModelDbDefer` 全部成功。

## 3. 目录与注册约定

- `AK_ExusiaiCode/Cards/`：卡牌，一张主牌一个文件。
- `AK_ExusiaiCode/Powers/`：能力与持续状态。
- `AK_ExusiaiCode/Mechanics/`：弹药、快递、天使、目录和共享命令。
- `AK_ExusiaiCode/Patches/`：确无合适 Hook 时使用的窄范围 patch。
- `AK_ExusiaiCode/Relics/`、`Potions/`：遗物和药水。
- `AK_Exusiai/localization/{zhs,eng}/`：简中与英文。
- `AK_Exusiai/images/`、`audio/`：运行时最终资源。
- `references/{official,free}/`：可复现制作所需的源素材及来源记录。

注册优先使用 RitsuLib：

- 角色卡：`[RegisterCard(typeof(ExusiaiCardPool))]`。
- 不进常规奖励/商店的衍生牌：`CardRarity.Token` + `TokenCardPool`。
- 角色遗物/药水：`ExusiaiRelicPool` / `ExusiaiPotionPool`。
- 能力：`[RegisterPower]`。
- 入口必须保留 Godot 脚本注册和 Mod 程序集类型发现。

当卡牌与角色同名时，注册特性中的角色类型写完整限定名，避免短类型名解析成卡牌。运行时资源只能从本 Mod 的 `res://AK_Exusiai/...` 加载。

## 4. 核心机制合同

### 弹药

- 无常规上限，战斗结束清空。
- 有弹药时，每个实际打出的攻击牌消耗 1 发；0 弹药不阻止攻击。
- “福音摇滚”按能力层数提高攻击牌的弹药消耗上限；与牌自身的多弹药上限相加，并同步用于卡面预览。无限上限牌仍保持无限。
- 每发弹药使该次出牌的每段 Powered Attack 基础额外造成 2 点伤害，再叠加角色的“火力”。“临时火力”必须仿照原版临时集中：施加时同步增加火力，叠层时同步增加，回合结束时扣除等量火力。
- Replay 的每个实际 `CardPlay` 分别结算；明确声明“不消耗弹药”的自动攻击按其专用逻辑处理。
- 多段、AOE、延时伤害和卡面预览都必须分别核对，不能只验证生命值结果。

### 快递

- 快递牌获得保留与消耗；回合结束倒计时，达到自动打出阈值时触发。
- 运行时增减统一经过可等待的 `DeliveryCmd`，按实际变化量触发响应；规范模型的初始层数使用无触发的 `Set`。
- 层数响应先结算，再执行“加急”等自动打出。测试满手牌、无合法目标、批量变化、保存恢复和多人同步。

### 天使

- 天使牌在本场战斗中的耗能不会增加；最低费用会随随机降费继续下降，降到 0 后保持 0。
- 费用读取、描述刷新和悬停预览必须保持只读；最低费用只在 `CardEnergyCost` 的确定性修改命令执行后记录，绝不能在费用贡献者的查询回调里写模型状态，否则多人会因 UI 查询时机不同而产生状态分叉。
- 身份判断检查能力组件本身，不依赖 `OnAttach`；默认身份与战斗中动态授予都要显示关键词。
- 本地费用贡献者和全局费用 Hook 不得重复修正同一费用。
- 天使、消耗和额外打出联动按每个效果的明确时序实现；能力牌是否计入以设计表为准。

### 物流与衍生牌

- 物流卡使用专用目录和权重，不等同于普通角色卡池。
- 德克萨斯、圣城净化等衍生牌使用无色外观，但属于 Token，不进入商店或无色奖励。
- 随机生成要明确是否允许重复、是否升级、手牌满时去向和随机源。

## 5. 分类开发经验

### API、Hook 与模型状态

- 优先复用原版命令、Hook 和模型；RitsuLib 已覆盖的注册/资源能力优先于自写 patch。
- 只有没有合适 Hook 时使用 Harmony。异步 patch 必须窄范围并校验目标调用点数量，避免游戏更新后静默误补丁。
- `ModelDb` 构造阶段得到规范模型。构造函数和默认能力附加阶段不得调用要求可变模型的 `AddKeyword`、`RemoveKeyword` 等 API；固定关键词放入 `CanonicalKeywords`，动态修改前检查 `card.IsMutable`。
- 加载失败先查依赖版本、Mod ID、本地化键、资源路径、规范模型异常和 patch 报告。
- 战斗开始时向手牌生成卡牌应参考原版 `BigHat`，在玩家首个 `AfterSideTurnStart` 结算；`BeforeCombatStart` 更适合资源和能力初始化，不保证手牌已可安全写入。

### 数值、费用与本地化

- 升级数值使用 `DynamicVar`、`DamageVar`、`BlockVar`、`PowerVar<T>`、`EnergyVar` 等，不把升级值硬编码进描述。
- 会受战斗修正的伤害/格挡使用 `{Damage:diff()}`、`{Block:diff()}`；`PowerVar<T>` 默认键是完整类型名，如 `WeakPower`。
- 能量使用 `{Energy:energyIcons()}` 且变量必须是 `EnergyVar`。1–3 点自动显示多个图标，4 点以上显示“数字+图标”。
- 任一错误变量键或格式器都可能令整段 SmartFormat 原样显示；简中和英文必须一起检查。
- 能力栏说明中的实际数值使用 `[blue]...[/blue]`；已知能力层数的卡牌/遗物悬浮说明必须调用带数值的 `HoverTipFactory.FromPower<T>(amount)`，否则会显示规范模型的 0。嵌套关键词只需解释机制时，应使用独立的无数值通用文本。
- 规则关键词在描述中用 `[gold]...[/gold]`。额外悬浮说明按描述显式启用，不能因所有攻击都可能消耗弹药就给每张攻击牌附加弹药说明。

### 伤害、时序与预览

- 延时伤害若复制打出时伤害，应在原 `CardPlay` 有效时保存最终攻击方修正值；下回合直接使用快照，不再次套用弹药或新的攻击方修正。
- 向其他敌人传递主目标实际伤害时，参考原版“万向斩”：先记录主目标 `DamageResult`，再以 `TotalDamage + OverkillDamage` 和 `Unpowered | Move` 造成等量伤害，避免二次计算力量、火力等攻击修正。
- 回合结束因虚无消耗而触发的抽牌必须参考原版“黑暗之拥”：在 `AfterCardExhausted(..., causedByEthereal: true)` 只累计次数，等 `AfterSideTurnEnd` 再抽牌，避免新牌被当前回合的弃牌清理移入弃牌堆。
- 卡面预览读取当前临时能力；若牌自身会先创建增伤，再用牌专用预览接口补上即将获得的层数。
- 复用临时力量等原版逻辑但要显示自制来源时，设置 `OriginModel`；自定义图标使用资源覆盖接口。
- 需要逐次消耗人工制品的多层减益必须逐次 `PowerCmd.Apply`，不能先合并总数。
- 必选一张牌并在选择后立即返回时，使用原版“全息影像”式 `CardSelectorPrefs(prompt, 1)`；`(0, 1)` 表示“至多一张”，会额外等待确认。

### 资源与 Godot

- Godot 将 PNG 导入并写入 PCK，不等于卡牌会自动使用它；所有卡牌必须通过 `CardAssetProfile.PortraitPath` 绑定 `res://AK_Exusiai/images/cards/<ClassName>.png`，缺图时才回退到 RitsuLib 占位图。
- `PowerAssetProfile` 当前加载静态 `Texture2D`；GIF 不会自动成为逐帧能力图标。动态效果需要 Godot 场景和专用 UI。
- 能量视觉分为描述小图标、卡牌费用图标和战斗能量计数器场景；旋转/获得能量动画来自场景和 `NEnergyCounter`，不是 GIF。
- 当前原版资源尺寸：遗物小图/轮廓 85×85，大图 256×256；药水小图/轮廓 80×80，原版大图 256×256。均使用透明方形画布；RitsuLib 0.5.14 的药水配置只直接覆盖小图和轮廓，大图需要另行验证或补充覆盖。
- 原版角色 `IconTexture` 为 85×85，且历史记录和顶部面板会直接使用该纹理尺寸；高分辨率源图保留在 `references/`，运行时角色头像由资源脚本缩放到 85×85。
- 本项目遗物使用一张 256×256 主图兼作小图和大图，药水使用一张 80×80 主图；两者各有同尺寸白色轮廓图。替换源图后运行 `& $GodotExe --headless --path . --script .\tools\generate_item_assets.gd`，映射与详细说明见 `tools/README.md`。
- `*.import` 是可再生元数据且被忽略。协作时提交源 PNG/JPG/SVG，并以完整构建中的 `reimport`、`savepack` 记录确认进入 PCK。

## 6. 标准实现与测试流程

1. 对照 CSV 确认基础/升级费用、类型、稀有度、目标、关键词、触发时序和边界；有两种合理解释时先询问。
2. 在当前 `sts2.dll` 找最接近的原版卡、遗物、药水、能力、命令和 Hook。
3. 先做最小机制原型，再扩展同组内容；避免多个任务同时修改共享入口和本地化文件。
4. 添加中英文文本、最终资源和必要来源记录。
5. 先做 C# 构建，再完整导出/部署，最后进游戏测试基础版、升级版和边界情况。
6. 更新 `docs/PROGRESS.md`：结果、验证、下一步和阻塞。

卡牌回归至少覆盖：0/1/大量弹药；单段、多段、AOE、随机目标与击杀中断；快递增减/归零/自动打出；天使费用升降、消耗和重放；抽牌堆、弃牌堆、消耗堆、满手牌；能力叠加、人工制品、战斗结束和存档恢复。

## 7. Git 与素材协作

- `main` 保持可构建、可加载；新机制使用 `card/`、`mechanic/`、`relic/`、`potion/`、`art/`、`fix/`、`docs/` 分支。
- 开工前在 `docs/PROGRESS.md` 认领；共享文件由当期集成负责人统一修改。合并优先使用 PR，并由另一边或其 Codex 只读审阅。
- 保留用户和协作者已有改动；不提交 `bin/`、`obj/`、`.godot/`、日志、部署产物、游戏 DLL/PCK、凭据和本机路径。
- 实现所需的官方/授权源素材及最终资源都要进入 Git，不能只存在某台电脑。来源和用途记入对应 `SOURCES.md`。
- 不提交整套游戏资源、无关批量提取结果或其他 Mod 的自制素材。大文件需要 Git LFS 时先单独启用并验证。

## 8. 参考资料优先级

1. 本仓库已在当前版本构建并测试成功的代码。
2. 当前游戏安装中的 `sts2.dll` 与附带 XML。
3. 锁定版本 RitsuLib 的 README、XML 和 DLL。
4. RitsuLib 官方文档与仓库。
5. 持续更新的社区教程；只能用于定位思路，必须与当前 DLL 交叉验证。
