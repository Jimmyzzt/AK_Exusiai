# 工坊发布准备

核对日期：2026-09-13。本文是 AK_Exusiai 的准备清单和操作说明，本轮尚未创建工坊条目或更改仓库可见性。介绍只展示 Mod 内容，不列维护者与合作者的分工。

## 先准备哪些物料

建议首发采用“一张封面、六张截图、中英文介绍”。下列像素尺寸是制作建议，不是 Steam 商店胶囊图的硬性规格；这里发布的是工坊 Mod。

| 物料 | 建议规格 | 内容与当前状态 |
| --- | --- | --- |
| 工坊主封面 | 方形 1024×1024 工作图，导出 PNG 小于 1 MB，建议控制在 900 KB 内 | 已有 `references/free/preview/Exusiai.psd` 与 PNG。当前 PNG 为 1,026,510 字节，正式上传前仍建议压到 900 KB 内 |
| 截图 1：角色选择 | 1920×1080，或保留原生 16:10 比例 | 已收集能天使与新约能天使角色选择图 |
| 截图 2：战斗与弹药 | 同上 | 小人、手牌、弹药与苹果能量计数器同时可见。待截取 |
| 截图 3：过载爆发 | 同上 | 展示多段攻击与过载状态，避免被大量弹窗遮挡。待截取 |
| 截图 4：天使卡牌 | 同上 | 展示保留、生成牌与一组能读清的卡牌。待截取 |
| 截图 5：企鹅物流 | 同上 | 快递/中转遗物栏与物流卡，体现这套玩法的差异。待截取 |
| 截图 6：外观与合作 | 同上 | 已有外观图；真实多人合作画面仍可作为后续补充 |
| 中文与英文介绍 | 短开场＋特色＋内容＋依赖/版本＋反馈 | 草稿已写入 [中文](workshop/description.zh-CN.md) / [英文](workshop/description.en.md)，发布前按实测兼容版本更新 |
| 简短更新公告 | 首发摘要；后续按变动项目记录 | 首发介绍内容即可；详细 V1.2 调整可参考 [历史更新记录](archive/changelog_v1.md) |
| 作者邀请信息 | Steam 好友关系＋主页链接 | [待邀请的共同作者](https://steamcommunity.com/profiles/76561199134042642/) |
| 选做：横幅/短视频 | 横幅 1920×640；视频 20–40 秒 | 依次展示外观、装填/爆发、物流；不影响首次上传准备 |

主预览图 `image.png` 必须小于 1 MB；官方模板对 `previews/` 内附加图片也要求小于 1 MB。截图保留高清母版，上传副本可压缩成 JPG。主图不要只把 JPG 改名为 PNG。[官方模板说明](https://github.com/megacrit/sts2-mod-uploader/blob/main/template/README.md)

工坊封面工作文件与七张实机截图已收集到 `references/free/preview/`。其中截图是高清母版，若通过官方上传器的 `previews/` 管理附图，需要另行导出小于 1 MB 的上传副本。`docs/workshop/description.zh-CN.jpeg` 是中文介绍的预览图，不作为工坊主封面。

介绍的开场建议：**“将《明日方舟》的能天使带入《Slay the Spire 2》：装填弹药、倾泻火力，借助天使的祝福与企鹅物流的支援攀登高塔。”** 后接内容数量和四组机制即可，完整卡牌效果由游戏内百科承担。

## 上传方式

### 官方上传器

社区教程可作为起点，但当前官方源码实际读取 `workshop.json`、`image.png`、`content/`，更新说明字段为 `changeNote`；教程中的 `workspace.json`、`image.jpg`、`changeNotes` 不应直接套用。下载哪个版本，就核对那个版本生成的模板。[教程](https://tutorials.sts2modding.com/docs/11-upload-workshop/)、[实际读取代码](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/UploadCommand.cs)

把上传工作区放在仓库的被忽略目录，例如：

```text
release/workshop/AK_Exusiai/
  workshop.json
  image.png
  content/
    AK_Exusiai.dll
    AK_Exusiai.pck
    AK_Exusiai.json
  previews/          # 可选；由上传器管理附加预览图时才创建
  mod_id.txt         # 首次成功上传后生成，更新时保留
```

从官方上传器生成的模板开始填写。以下是首次隐藏上传的配置参考，标签以当时工坊分类为准：

```json
{
  "title": "[明日方舟] 能天使 / Exusiai",
  "description": "能天使角色 Mod：弹药、过载、天使与企鹅物流。Exusiai character mod featuring Ammo, Overload, Angel cards and Penguin Logistics.",
  "visibility": "private",
  "changeNote": "首次上传，等待订阅验证。",
  "tags": ["Characters", "Cards", "Relics", "Simplified Chinese", "English"],
  "dependencies": [3747602295]
}
```

`dependencies` 是数字型工坊条目 ID 数组；RitsuLib 对应 **3747602295**。它与 Mod 清单中的字符串 ID `STS2-RitsuLib` 不同，工坊依赖不会替代清单版本检查。[字段类型](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/ModConfig.cs)、[RitsuLib 条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)

在存放 `ModUploader.exe` 的目录运行，`-w` 指向上述工作区：

```powershell
.\ModUploader.exe upload -w '完整的上传工作区路径'
```

Steam 客户端应已登录准备用来持有条目的账号。上传完成后保留 `mod_id.txt`，后续用同一 ID 更新；只上传 `content/` 中三个成品，不上传源码 ZIP、游戏 DLL 或编辑器扩展。失败时检查 `mod-uploader.log`。[官方用法](https://github.com/megacrit/sts2-mod-uploader/blob/main/README.md)

### 游戏内 Workshop Uploader

[OLC 的 Workshop Uploader](https://steamcommunity.com/sharedfiles/filedetails/?id=3748678645) 提供可视化编辑标题、语言、依赖、预览图和可见性的界面，也支持 Markdown 转 Steam BBCode。本项目已经依赖 RitsuLib，若希望日常用界面维护页面，这条路线比较方便；它是作者使用的工具，不是玩家游玩能天使必须订阅的依赖。本轮只调查，未安装或调用上传。[工具作者说明](https://github.com/BAKAOLC/STS2-WorkshopUploaderMod)

当前两份介绍草稿是 Markdown。使用该工具时可转换为 BBCode；使用官方 CLI 或直接在工坊编辑时，需转换为 Steam 格式，不能把 Markdown 的 `##` 和 `**` 当成工坊一定支持的格式。

## 共同作者怎么添加

是的，先创建工坊条目，再由上传账号在条目页面右侧 **Owner Controls → Add/remove Contributors（添加/移除贡献者）** 邀请好友。让合作者接受邀请，确认“Created by”中显示对方后即可。可先对隐藏条目操作，无需为了署名立刻公开。[Valve 的共同作者操作说明](https://www.dota2.com/frostivus2017agreement)

上面的 Valve 页面是其他作品的历史活动说明，这里只参考 Steam 的邀请流程，不套用其比赛、分成或素材规则。具体按钮与通知位置以当前工坊页面为准。

**署名权限与上传维护权限分别确认。** 查到的社区反馈在不同游戏中并不一致；当前 STS2 官方上传器也没有共同作者权限设置项。因此不能保证添加后对方就能编辑介绍或上传 DLL。建议由固定账号持有首发条目，在隐藏阶段让合作者实际检查可用操作；双方仍可通过 GitHub 协作源码。无需共享 Steam 密码。[相关社区反馈](https://steamcommunity.com/discussions/forum/10/618456760263925033/)

## 后续更新时容易踩的坑

- 在网页编辑好标题和介绍后，上传配置中的旧文本可能再次覆盖它们。决定由哪一处维护；网页维护时，在后续上传配置中省略对应字段，而非留空字符串。[教程的更新经验](https://tutorials.sts2modding.com/docs/11-upload-workshop/)
- 官方当前实现中，省略 `dependencies` 保留原依赖，写 `[]` 则会清空；存在 `previews/` 时会按文件名同步并移除缺少的旧图，整个目录不存在才保持附图不变。主图每次仍会提交。[更新实现](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/UploadCommand.cs)
- **版本范围是例外**：官方当前代码无条件调用版本范围设置，省略 `minBranch/maxBranch` 会传空范围；不能以为在网页填好的范围一定会保留。首次及每次更新后都核对所支持的游戏分支。[同一实现中的范围设置](https://github.com/megacrit/sts2-mod-uploader/blob/main/src/UploadCommand.cs)
- 从实际订阅端验收，而不只从作者电脑启动。其他游戏的官方工坊指南也推荐上传后自己订阅或请朋友测试；本项目尤其要排除本机手动部署副本掩盖缺依赖的问题。[官方工坊实践示例](https://steamcdn-a.akamaihd.net/steam/apps/362890/manuals/bms_workshop_guide.pdf)

## 本项目的发布顺序

1. 审阅介绍、封面与实机截图；补齐 [素材记录](archive/CREDITS.md) 中尚缺的来源；共同决定源码许可范围。
2. 统一版本：当前清单仍为 `1.1.0`，而 Git 已有 `v1.2`；确定本次公开包的版本号，并包含标签之后的修复，不重新移动既有标签。
3. 在目标游戏分支完成完整构建与部署验证，记录游戏/Mod/RitsuLib 版本；选定三个成品放入 `content/`。
4. 创建隐藏条目，补齐依赖、介绍与预览，完成 Steam Workshop 协议确认和共同作者邀请。未接受协议可能导致条目仍不可见。[Steamworks 说明](https://partner.steamgames.com/doc/features/workshop/implementation)
5. 改为合适的测试可见性，例如仅好友。请合作者从工坊订阅，检查全新安装、退出重启、单人战斗、百科、外观与多人对局。临时移开手动安装副本时先备份并确认路径。
6. 测试通过后公开，补上 README 的工坊链接；记录正式条目 ID。之后只更新这个条目，不为每个补丁创建新条目。

当前还缺：成品封面与截图、发布包版本统一、实际支持分支复测、部分素材来源，以及共同作者邀请。中文/英文介绍草稿已就位，工坊发布和仓库公开仍由正式发布步骤执行。
