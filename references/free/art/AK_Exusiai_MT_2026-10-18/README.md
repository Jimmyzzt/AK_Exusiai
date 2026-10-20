# 众生行记：怪物与战场素材包

整理日期：2026-10-18。供 AK_Exusiai 新怪组及后续幕层设计使用，尚未接入游戏，也未改变现有 Mod 注册、依赖、存档或玩法。

直接打开 [index.html](index.html) 查看背景、Boss UI、逐阶段转场和动作目录。浏览器限制本地画布导出时，独立 PNG 和下面的 MP4 可直接使用。

## 已交付

- MT-1 至 MT-10 的关卡配置、地图原图，以及关卡与敌人的对应表。普通关卡中共16种独立敌人；重复出现的敌人共用一套素材。
- 另收录 MT-S-5 的7种补充敌人，总计23套完整原始 Spine 三件套、150个动作。动作名称、原始时长和事件见 [全部动作CSV](03_animation_catalog/all_actions.csv)，每个敌人目录也有中文名称、出现关卡、`actions.json` 和 `动作清单.txt`。
- MT-1 至 MT-9：9张横向战斗背景，标准1920×1080。每关独立制作，构图依据见 [DESIGN.md](DESIGN.md)。
- MT-10：梦中、梦醒两张固定镜头背景，以及一次转场的透明图层与MP4。
- MT-S-5：梦中、揭示①、揭示②、揭示③、揭示④五张固定镜头背景，以及四段独立转场。预览默认到揭示③结束；勾选“额外阶段”才可进入揭示④。
- “圣徒”：地面重绘和飞行姿态概念各一张透明PNG，完整战斗UI预览，以及独立血条框、意图底板、阶段标记。生命值显示`? / ?`，不提前确定玩法数值。
- 活动相关的40张既有CG副本；战前事件主选及两张备选图独立保存。

## 入口

| 内容 | 文件 |
|---|---|
| 本地交互预览 | [index.html](index.html) |
| Boss战UI | [battle_ui_concept.png](02_created/boss/battle_ui_concept.png) |
| 九关背景总览 | [created_backgrounds_contact.jpg](00_index/created_backgrounds_contact.jpg) |
| Boss场景总览 | [created_boss_states_contact.jpg](00_index/created_boss_states_contact.jpg) |
| MT-10 梦中→梦醒 | [MT-10_transition_1.mp4](05_preview/exports/MT-10_transition_1.mp4) |
| MT-S-5 揭示① | [MT-S-5_transition_1.mp4](05_preview/exports/MT-S-5_transition_1.mp4) |
| MT-S-5 揭示② | [MT-S-5_transition_2.mp4](05_preview/exports/MT-S-5_transition_2.mp4) |
| MT-S-5 揭示③ | [MT-S-5_transition_3.mp4](05_preview/exports/MT-S-5_transition_3.mp4) |
| MT-S-5 条件揭示④ | [MT-S-5_transition_4_conditional.mp4](05_preview/exports/MT-S-5_transition_4_conditional.mp4) |
| 战前事件主选 | [primary_approach_to_sanctuary.png](04_event_background/primary_approach_to_sanctuary.png) |
| 关卡与敌人对应 | [关卡_敌人对应.csv](00_index/关卡_敌人对应.csv) |
| 来源、权属与验证边界 | [SOURCES.md](SOURCES.md) |
| 素材完整性检查 | [validation.json](00_index/validation.json) |
| 浏览器预览验证 | [browser_validation.json](00_index/browser_validation.json) |

## 目录

```text
00_index/               目录、对应表、下载哈希、原始揭示坐标、生成提示词及验证记录
01_originals/           原作地图、原始关卡配置、敌人原始动画、活动CG
02_created/backgrounds/ MT-1至MT-9的标准战斗背景
02_created/MT-10/       梦中/梦醒背景及透明揭示图层
02_created/MT-S-5/      梦中/揭示①至④背景及透明揭示图层
02_created/boss/        圣徒重绘、UI预览与独立UI元素
02_created/source_renders/ 未进行尺寸规范化的生成原图
03_animation_catalog/   全动作目录及以同版本解析的Spine JSON
04_event_background/    战前事件主选与备选原CG
05_preview/             离线预览代码和导出的MP4/PNG
tools/                  图像规范化、分层、渲染与验证脚本
```

原始动作是 Spine 3.8.x 数据，不应直接当作本游戏 Spine 4.2 的运行时文件加载。后续接入时转换副本，并验证动作事件、图集尺寸、边界和死亡/重生状态机；原始文件应保持不变。预览里的飞行重绘是姿态概念，不代替原始骨骼第二形态动画。

“入梦砖”及其三个编号变体作为关卡机制实体单列。PRTS常规敌人骨骼路径未提供它们的独立模型；没有把404响应、其他敌人的模型或静态图伪装成已取得的动作。

转场的区域与顺序依据PRTS各地图状态及原始`rect_1`至`rect_4`配置。公开视频已检索并读取页面，但视频播放/逐帧访问未稳定完成；本包的3.2秒转场、石板落位和边缘描光是为塔2重新设计的美术时序，不是测得的原作逐帧复刻。详情及公开参考视频见来源页。

## 离线使用与复现

整份文件夹或ZIP解压后可移到其他位置，打开根目录的`index.html`即可浏览；无需游戏、Godot或额外运行库。敌人页面用于检索原始动作清单，不是Spine实时播放器。五段MP4展示场景转场，不包含原作视频或怪物骨骼动画的重编码。

目录内的`.gitattributes`仅对本素材包关闭Git换行转换，确保原始文件和`files.sha256`在不同平台拉取后仍逐字节一致；不改变仓库其他源码的换行约定。

图像规范化和完整性检查脚本需要Python与Pillow；原图及站位参考均已在包内。`tools/render_bundle.cjs`需要Node.js、`@napi-rs/canvas`及中文字体；设置`MT_FFMPEG`为本机FFmpeg可执行文件路径才会重新导出MP4。运行`tools/finalize_bundle.py`生成总览、重新验证并更新哈希；可传`--zip-output`指定包外的全新ZIP路径，脚本不会覆盖已有压缩包。
