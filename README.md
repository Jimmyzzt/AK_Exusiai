# AK_Exusiai

《Slay the Spire 2》能天使角色 Mod。角色原型来自《明日方舟》的能天使；本项目为非官方、非盈利同人二次创作。

## 交接入口

新对话或新协作者按以下顺序阅读：

1. `AGENTS.md`：开发约束、已验证机制经验与构建方式。
2. `docs/PROGRESS.md`：当前完成状态和待测项。
3. `docs/AK_Exusiai-Card_V1.csv`：V1 大修的当前只读卡牌设计；遗物/药水见 `docs/AK_Exusiai-Relic_Potion_V1.csv`，历史版本与平衡说明位于 `docs/archive/`。

## 当前状态

- V1 机制大修已完成设计冻结，集成分支为 `mechanic/v1-overhaul`；第一阶段拆分为弹药/过载与快递/中转两条开发分支。
- 角色、弹药、初始牌组和初始遗物已可用。
- 20 张普通牌已完成游戏内回归。
- 35 张罕见牌、25 张稀有牌和相关衍生牌已完成代码首版，等待集中回归。
- 角色选择小图、4K 背景及现有能力图标已进入 PCK。

## 本地构建

复制 `local.props.template` 为不提交的 `local.props`，填写游戏与 Godot 路径。模板中的 `RitsuLibDeployDir` 会让完整构建自动补齐匹配版本的 RitsuLib 运行时。

```powershell
dotnet build .\AK_Exusiai.csproj
```

只编译 C#：

```powershell
dotnet build .\AK_Exusiai.csproj /p:RunPckExport=false /p:CopyModOnBuild=false
```

正式代码在 `AK_ExusiaiCode/`，运行时资源在 `AK_Exusiai/`，源素材及来源记录在 `references/`。
