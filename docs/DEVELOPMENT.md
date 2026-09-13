# 开发说明

本项目是《Slay the Spire 2》的能天使角色 Mod，源码与资源分别位于 `AK_ExusiaiCode/` 和 `AK_Exusiai/`。当前游戏与依赖依据见 [框架基线](FRAMEWORK_AUDIT.md)，实现约定见 [AGENTS](../AGENTS.md)。

## 搭建环境

1. 安装并运行过一次 Steam 版游戏；使用与目标 API 匹配的游戏分支。当前验证基线为 Windows、public beta 0.111.0。
2. 安装 .NET 9 SDK 与 Godot 4.5.1 **Mono/.NET** 版。
3. 将仓库的 `local.props.template` 复制为 `local.props`，填写 `Sts2Dir`、`Sts2DataDir` 和 `GodotExe`。该文件不提交，其他共享文件使用相对路径。
4. 单独安装与项目相符的 RitsuLib 运行时。NuGet 会还原编译包，**不会自动把它安装到游戏**；模板中的 `RitsuLibDeployDir` 为旧配置，当前构建文件没有消费它。
5. 完整 Godot 导入需要适用于 Godot 4.5.1 的 Windows Spine 编辑器扩展。按 [Spine 资源说明](../tools/README.md#spine-character-assets) 放入被忽略的 `SpineGodotExtension4.5.1/`。游戏自带 Spine 运行时，编辑器扩展不随 Mod 发布。

游戏 DLL 从本机安装读取，仓库不提供游戏 DLL、PCK、FMOD bank 或完整游戏资源。仅检查 C# 时不需要运行 Godot 资源导出。

## 编译与部署

从仓库根目录运行，仅编译 C#：

```powershell
dotnet build .\AK_Exusiai.csproj /p:RunPckExport=false /p:CopyModOnBuild=false
```

完整构建前退出游戏，避免 DLL 被锁定：

```powershell
dotnet build .\AK_Exusiai.csproj
```

完整构建会把 Mod DLL、JSON 复制到 `$(Sts2Dir)/mods/AK_Exusiai/`，并直接将 PCK 导出到该目录。只有 `CopyModOnBuild=false` **不会**禁止 PCK 写入游戏目录；想完全不部署时，应同时禁用两个属性。

如需为发布单独准备输出目录，可覆盖构建目标：

```powershell
dotnet build .\AK_Exusiai.csproj -c Release /p:ModOutputDir=release/AK_Exusiai/ /p:ModPckPath=release/AK_Exusiai/AK_Exusiai.pck
```

`release/` 已被 Git 忽略。`ModPckPath` 指定 PCK 文件，`ModOutputDir` 指定 DLL/JSON 目录；显式同时设置可避免输出位置不一致。之后只将这三个成品文件放入工坊上传工作区的 `content/`。

不要复制整个 `.godot/mono/temp/bin/` 作为发布包：编译引用中包含本机游戏程序集和工具依赖。

## 验证

- 玩法或 C# 变更：编译无错误，完整导出包含变更资源，部署文件与该次构建输出一致；记录实际完成的游戏内测试。
- 本地化变更：检查中英文 JSON、基础与升级版、动态伤害/格挡、生成牌预览与关键词说明，并完整导出 PCK。
- 纯文档变更：检查链接、现状与命令是否对应实际代码，以及 `git diff --check`；无需启动游戏或重新部署。

实战回归重点为弹药不足、多段/AOE/击杀中断、过载、天使费用与 X 费、快递/中转恢复、人工制品、存档和多人同步。专用命令见 [测试说明](EXUSIAI_TEST_COMMANDS.md)。控制台夹具会修改当前战斗，使用专门的测试对局。

保留“编译通过”“主菜单加载通过”“实战通过”的区别。遇到不同步，收集对应运行日志和 `ritsulib_state_divergence_*.zip`；仅有一次无报错构建不足以证明多人同步正确。

## 资源与工具

| 目录 | 用途 |
| --- | --- |
| `AK_ExusiaiCode/` | 卡牌、能力、角色、机制、补丁和开发命令 |
| `AK_Exusiai/` | 随 PCK 发布的本地化、图像、场景、音效与正式特效配置 |
| `references/` | 选定原始素材与来源记录，不直接从此加载运行时资源 |
| `tools/` | 卡图/特效管理器、图标生成和资源验证脚本 |
| `docs/` | 当前说明、设计表和历史归档 |

替换能力图标后，使用 Python 3 与 Pillow 运行 `tools/generate_power_icons.py`，生成 64×64/256×256 两套资源，再让 Godot 重新导入。卡图与音效制作流程见 [工具总览](../tools/README.md)。

当前导出预设排除了源码、文档、参考素材、制作工具和 Spine 编辑器扩展；自定义音效的 `SOURCES.md` 会被包含。发布时仍需核对实际 PCK 内容。

## 提交约定

保留工作区中其他人的改动，按此次任务范围暂存文件。公开 ID、类名与保存字段变更需考虑已有存档，不能仅为统一英文拼写而重命名。设计调整同步基础/升级版、中英文和更新记录。

不提交本机路径、构建产物、凭据、日志、缓存或整套游戏资源。新增源素材补充对应 `SOURCES.md`；仓库公开与为源码选择许可证是两件事，当前没有统一代码许可证。

工坊上传与源码提交分开操作，发布流程见 [工坊准备](WORKSHOP_RELEASE.md)。
