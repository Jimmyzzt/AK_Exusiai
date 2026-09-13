# 游戏与框架基线

核对日期：2026-09-13。本页记录项目采用和最近验证的组合，不将线上最新版本自动视为兼容。

| 项目 | 当前依据 |
| --- | --- |
| 游戏 API | 最近验证基线为 public beta `v0.111.0`；清单 `min_game_version` 为 `0.111.0` |
| Godot | 项目 SDK 与导入环境为 4.5.1 Mono |
| .NET | `net9.0`，C# 13 |
| RitsuLib 编译包 | `AK_Exusiai.csproj` 固定 `STS2.RitsuLib` 0.5.20 |
| RitsuLib 最低运行时依赖 | `AK_Exusiai.json` 声明 `STS2-RitsuLib` 0.5.20 |
| Mod 版本线 | Git 已有 `v1.1`、`v1.2` 标签；当前 `main` 还包含 V1.2 后续修复 |
| 待统一元数据 | `AK_Exusiai.json` 仍显示 `1.1.0`，正式发布包需统一为计划发布的 V1.2 版本号 |

旧文档中的 RitsuLib 0.5.17/0.5.18 属于历史基线。当前 csproj 没有自动部署 RitsuLib 的构建目标；仅设置 `RitsuLibDeployDir` 不会安装运行时。

## 现有验证证据

2026-09-11 的 `19f7aed` 修复完成过完整构建、PCK 导出和本机部署，0 警告、0 错误；部署 DLL 与本地构建 DLL 哈希一致。此前的主菜单、资源和部分实战测试记录保留在 [开发归档](archive/PROGRESS_2026-09-10.md)。

2026-09-13 只整理文档，未更新依赖、运行游戏或重新验证最新 Steam 分支。游戏清单最低版本、Steam 分支范围和实际验证结果需分别记录。

## 升级或发布时

1. 记录游戏界面版本、Steam 分支与 `sts2.dll` 哈希。
2. 确认计划使用的 RitsuLib 支持该 API，统一编译包和实际游戏运行时。
3. 编译并完整导出，核对自动注册、资源、本地化和补丁目标是否成功。
4. 回归弹药、天使/X 费、遗物物流、生成牌预览、Replay、额外回合与多人选择/回滚。
5. 在发布记录中写明实测版本；工坊版本范围按实测设置，不把 `min_game_version` 当成跨全部后续版本的保证。

参考：[RitsuLib 文档](https://sts2-ritsulib.ritsukage.com/)、[框架源码与发行版本](https://github.com/BAKAOLC/STS2-RitsuLib)、[社区教程](https://tutorials.sts2modding.com/)。具体 API 仍以当前本机程序集为准。
