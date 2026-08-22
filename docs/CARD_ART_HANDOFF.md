# 卡图工作交接

本页供没有历史聊天的新对话直接开始卡图制作。

## 当前状态

- `AK_Exusiai/images/cards/` 目前没有正式卡图。
- `ExusiaiStrike`、`ExusiaiDefend` 暂时引用铁甲战士原版卡图。
- 其他卡牌主要使用 RitsuLib 占位图。
- 卡牌设计、中文名、英文名和效果以 `docs/AK_Exusiai-Card.csv` 为准；卡图工作不得顺便修改规则或公开 ID。

## 素材位置

- `references/official/art/`：已选用的角色立绘、头像、CG 和标志。
- `references/official/asset/`：维护者新增的官方美术参考批次，供卡图构图选材。
- `references/free/art/`：自制或可用图标源文件。
- `references/official/SOURCES.md`、`references/free/SOURCES.md`：来源与用途记录。

不要覆盖或删除源素材。生成、裁切或合成后的正式卡图放入运行时目录；若使用了新的源素材，同时更新来源记录。

## 命名与接入

正式文件统一使用：

```text
AK_Exusiai/images/cards/<CSharpClassName>.png
```

例如 `ChargingMode.cs` 对应 `AK_Exusiai/images/cards/ChargingMode.png`。图片只包含画面，不包含卡框、费用、标题、规则文字、水印。

卡牌类中通过正式资源路径接入：

```csharp
public override CardAssetProfile AssetProfile => new(
    PortraitPath: $"{Entry.ResPath}/images/cards/ChargingMode.png");
```

已有英文名与 C# 类名可用 `rg` 在 `AK_ExusiaiCode/Cards/` 和本地化 JSON 中核对。不要根据中文名自行新建另一套 ID。

## 推荐批次

1. 初始牌：`ExusiaiStrike`、`ExusiaiDefend`、`ChargingMode`、`LockedAndLoaded`。
2. 20 张普通牌。
3. 35 张罕见牌。
4. 25 张稀有牌与衍生牌。

每批保持独立提交，便于替换和审阅。源图与正式图都要提交；不要提交 Godot 的 `.import`、`.godot/` 或导出的 PCK。

## 验证

1. 完整构建，确认 Godot 日志出现新 PNG 的导入与 `savepack`。
2. 进入卡牌图鉴检查裁切、主体位置、升级版共用图和透明边缘。
3. 在战斗、奖励、商店、选择界面分别检查缩略图。
4. 确认卡图没有改变卡牌效果、ID、本地化或卡池。
5. 更新 `docs/PROGRESS.md`，写明已完成的卡图批次与仍需重做的图片。
