# 来源与范围

本包将原作资料、已有素材副本与新制作素材分开保存。`free`是仓库既有分类名称，不代表本包素材取得任意用途授权。明日方舟游戏图像、动画和CG的权利归鹰角及相关权利方；PRTS页面中的网站自有内容遵循其页面许可。没有为本包整体另行指定许可证。

## PRTS与原始资料

| 内容 | 公开来源 | 本地位置 |
|---|---|---|
| 活动入口 | https://prts.wiki/w/众生行记 | 关卡范围与命名依据 |
| MT-1至MT-10 | https://prts.wiki/w/MT-1 至 https://prts.wiki/w/MT-10 | `01_originals/maps/` |
| MT-S-5普通/突袭 | https://prts.wiki/w/MT-S-5 | `01_originals/maps/MT-S-5/` |
| 圣徒身份、动作与机制 | https://prts.wiki/w/“圣徒” | `01_originals/enemies/enemy_1567_pope_“圣徒”/` |
| 未变化地图预览 | `https://torappu.prts.wiki/assets/map_preview/<stageId>.png` | 各关`map_preview.png` |
| 变化后地图 | PRTS上述关卡页的梦醒/揭示①至④图片 | 各Boss关目录的编号PNG |
| 敌人骨骼、图集与纹理 | `https://torappu.prts.wiki/assets/enemy_spine/<enemyId>/<enemyId>.skel/.atlas/.png` | 各敌人目录 |
| 关卡与名称结构化镜像 | https://github.com/Kengxxiao/ArknightsGameData | 关卡配置及`00_index`中的选定资料 |
| 既有活动CG | 仓库`references/official/asset/Avg_61_*.png`，PRTS剧情资源可作对照：https://prts.wiki/w/剧情角色一览 | `01_originals/event_cg/` |
| 能天使站位参考 | 仓库`references/official/art/Exusiai_default_default.png` | `05_preview/exusiai_scale_reference.png` |

每张已下载图片和每套骨骼的精确URL、字节数与SHA-256见`00_index/downloads.json`。全包最终文件哈希见`00_index/files.sha256`。游戏资料JSON是结构化保存的选择内容，不声称其排版与服务器原始字节一致。

动作解析使用项目既有流程采用的 [SpineSkeletonDataConverter v3.8](https://github.com/wang606/SpineSkeletonDataConverter/releases/tag/v3.8)，只把原始骨骼解析为同版本JSON以列出完整动画，不修改原始`.skel`。工具可执行文件没有放入素材包。原始动画名称和时长可能与PRTS查看器经过补丁、裁剪、连播后的展示不同，不能直接把目录时长当作塔2的伤害触发点。

## 视频检索记录与验证边界

检索使用“明日方舟 MT-10”“明日方舟 MT-S-5”“明日方舟 MT-S-5突袭”，并细化过“转阶段”“圣徒”“梦醒”。以下是公开参考入口：

- [MT-10低配版攻略，BV1MUVPzVE41](https://www.bilibili.com/video/BV1MUVPzVE41/)
- [圣徒双阶段展示，BV1eGV5zmEJo](https://www.bilibili.com/video/BV1eGV5zmEJo/)
- [MT-S-5普通低配，BV1vhGx6cEkL](https://www.bilibili.com/video/BV1vhGx6cEkL/)，页面作者为冰海孤魂。
- [MT-S-5突袭低配，BV1xSEdzwEau](https://www.bilibili.com/video/BV1xSEdzwEau/)
- [MT-S-5突袭展示，BV13dE1z7EUZ](https://www.bilibili.com/video/BV13dE1z7EUZ/)

已完成搜索与部分视频页面信息读取；B站接口返回412，浏览器播放器与截图访问反复超时，未完成可靠的逐帧观看和秒级测量。没有转载这些视频、没有把检索摘要写成视频实测记录。场景新增区域和顺序由PRTS实际地图截图与关卡配置交叉确认；转场时间、石板显现方式和碎屑是为塔2重新设计的方案。

## 新制作内容

`02_created`的背景与圣徒重绘使用内置`image_gen`，提示词和输入素材角色见`00_index/generation_prompts.json`。原始生成图片位于`source_renders`，未覆盖PRTS原图。标准化只涉及分辨率、透明边距和转场分层；`layers/`与最终阶段图由同一分层流程产生。

UI纹理及UI合成预览由`05_preview/scene_renderer.js`的同一绘制逻辑生成。MP4使用临时取得并校验PyPI轮包哈希的`imageio-ffmpeg`编码器导出；编码器没有进入本包或游戏。

## 还未进行的游戏验证

本包没有注册怪物、遭遇或第四幕，没有选择游戏Hook，没有完成游戏内缩放、动画事件、交互和多人同步测试。实际接入须进一步验证模型转换、Boss阶段时序、背景资源预载与释放、宽屏裁切、保存/读档和多人阶段一致性。
