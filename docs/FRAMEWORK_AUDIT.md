# 游戏与框架核查（2026-08-21）

## 结论

AK_Exusiai 应直接以游戏 public beta `v0.111.0` 和 `STS2.RitsuLib` `0.5.14` 建立新项目。两位开发者分别配置自己的游戏和 Godot 安装路径，不在仓库中共享绝对路径。

## 基线核查记录

- 游戏 `release_info.json`：
  - version/branch：`v0.111.0`；
  - commit：`41cef1ea`；
  - 构建时间：2026-08-14。
- `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- NuGet 包源在 2026-08-21 返回 `STS2.RitsuLib` 最新版本 `0.5.14`。
- 0.5.14 NuGet 包：
  - repository commit：`8fca891d65de050b1848b9dc4e1fcc449dacf253`；
  - 包内生成清单目标：`0.111.0`；
  - GodotSharp/Godot.SourceGenerators：4.5.1；
  - 目标框架：`net9.0`。

## 开发要求

- 当前 DLL 反编译优先于教程；
- 动态变量、本地化、资源路径和注册分层的做法；
- 普通机制优先使用原版命令/Hook，Harmony 只用于没有窄范围替代的情况；
- C# 只编译与完整 PCK/部署测试分离；
- 对异步 patch 校验目标数量和执行顺序。

- 新项目 PackageReference 固定为 0.5.14；
- Mod 清单 `min_game_version` 使用 `0.111.0`；
- 安装与编译包匹配的 RitsuLib 0.5.14 运行时后再做进游戏验证；
- RitsuLib 当前 README 推荐 `CreateContentPack`、生命周期事件和诊断能力，应优先核对 0.5.14 文档，而不是机械复制旧项目的自动注册模式；
- 官方游戏从 0.107.1 起发布 `STS2.dll` XML 文档，并修复 Steam Mod 下 `ReflectionHelper.ModTypes` 的问题；开发时应把各自游戏安装附带的文档列为直接参考；
- 社区教程仓库当前明确支持 `public-beta`，但仍声明 API 频繁变化，因此只能作为次级参考。

## 下一次升级检查

当游戏或 RitsuLib 再次更新时：

1. 记录 `release_info.json`、`sts2.dll` 哈希和文件时间。
2. 查询 NuGet 的当前稳定版和对应游戏 API；不要只看 GitHub 页面缓存。
3. 确保 NuGet 编译包、游戏分支和 `mods/STS2-RitsuLib` 运行时三者对应。
4. 在独立分支修改版本，不直接更新 `main`。
5. 先做 0 警告/0 错误编译，再跑加载、注册、资源、本地化和每个自定义 patch 的烟雾测试。
6. 对弹药伤害修正、快递自动打出、天使耗能保护、额外回合和批量自动出牌做完整回归。

## 公开参考

- 游戏 v0.111.0 补丁说明：https://store.steampowered.com/news/app/2868840/view/671751488532383386
- RitsuLib 仓库：https://github.com/BAKAOLC/STS2-RitsuLib
- RitsuLib NuGet：https://www.nuget.org/packages/STS2.RitsuLib
- RitsuLib 文档：https://sts2-ritsulib.ritsukage.com/
- 社区教程：https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials
