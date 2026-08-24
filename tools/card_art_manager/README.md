# 卡图管理器

这是一个仅供制作资源使用的 Godot 小工具，不会进入 Mod 的 PCK。它读取卡牌类和简中标题，帮助维护 `card_art_manifest.json`，并将配置导出为运行时卡图。

## 启动

退出正在运行的 Godot 项目后，在仓库根目录执行：

```powershell
.\tools\card_art_manager\run_card_art_manager.ps1
```

启动脚本从未跟踪的 `local.props` 读取 `GodotExe`，不会把本机绝对路径写进仓库。

维护者可以运行 `.\tools\card_art_manager\run_card_art_manager.ps1 -SmokeTest`，无界面验证插画、SVG 图标和先古占位图三条渲染路径；测试预览写入已忽略的 `tmp/card_art_manager_smoke/`。

## 基本流程

1. 左侧默认扫描 `references/official/art`、`references/official/asset` 和 `references/free/art`；也可以添加单张素材或整个文件夹。
2. 右侧选择卡牌和素材类型。
3. 选择左侧素材，将它关联到当前卡牌。
4. 在中间预览区滚轮缩放、左键拖拽。配置在修改后自动保存。
5. 导出当前卡图，或批量导出所有已配置卡牌。

工具栏提供 100%、125%、150%、175% 和 200% 五档界面缩放，字体与控件会一起缩放。左右两条分隔线可以拖动，栏宽会保留到下次启动。素材区可以切换“列表/紧凑”视图；紧凑视图会根据左栏宽度自动排列多列缩略图。文件夹下拉框按素材所在目录分类和筛选。

输出文件为 `AK_Exusiai/images/cards/<CSharpClassName>.png`。默认使用 2× 规格：普通卡 500×380、先古卡 500×702。右侧可以选择 1×、2×、3× 或 4×，始终以官方的 250×190 / 250×351 比例等比缩放；该全局选择保存在卡图清单中。

管理器会从 C# 源码识别 `CardType.Attack`、`CardType.Skill`、`CardType.Power` 和 `CardRarity.Ancient`。预览区默认显示与官方攻击折角、技能平底、能力圆底及先古圆角遮罩相同形状的可视框，方便避开实际会被卡框挡住的位置；它只是参考叠层，不会写入导出的 PNG，可以在右侧关闭。

## 三种素材类型

- **插画裁切**：缩放值 `1.00` 表示图片刚好覆盖整个输出画布；拖拽调整构图。缩放可以低至 `0.05`，小于 `1.00` 时素材保持居中并允许留白。
- **透明图标**：生成渐变、光照、暗角和投影，再将图标置于画面中心；支持 PNG 与 SVG，也可以将小图标缩至 `1.00` 以下。
- **程序化占位**：不需要源素材，使用背景和简单机制图形快速生成测试图。

清单中的项目内路径保持相对路径。添加项目外目录时会保留绝对路径，因此准备提交或交接前，应把需要复现的源素材放入 `references/`，再重新关联。

## 数据与缓存位置

- 可提交的卡图配置和全局输出倍率保存在 `tools/card_art_manager/card_art_manifest.json`。
- 导出的运行时图片保存在 `AK_Exusiai/images/cards/`，它们是项目资源，不是缓存。
- 界面缩放、素材视图、文件夹筛选和分隔栏宽度保存在 Godot 的 `user://card_art_manager_ui.json`。Windows 上通常对应 `%APPDATA%\Godot\app_userdata\AK_Exusiai\card_art_manager_ui.json`。
- 已加载原图和缩略图只缓存在内存中，关闭工具即释放，不另建磁盘缩略图缓存。
- Godot 自身的导入与编辑器缓存位于仓库的 `.godot/`，已被 Git 忽略，可以在关闭 Godot 后删除并让其重建。`references/.gdignore` 会阻止源素材继续进入导入缓存。
- 冒烟测试预览位于已忽略的 `tmp/card_art_manager_smoke/`，可随时删除。
