# 能天使社区统计

[主站](https://exusiai.zzt.si/) · [GitHub Pages](https://jimmyzzt.github.io/AK_Exusiai/)

只维护三个文档：本页说明开发和运维，[架构](docs/ARCHITECTURE.md)说明数据库、计算、成本与行业参考，[指标](docs/METRICS.md)定义公式。最新生产验证记入[项目进度](../docs/PROGRESS.md)，不另建一次性报告。

## 确认范围

- 玩家授权后只上传新增的本机能天使玩家局，不补传历史；联机、放弃局也接收。
- 默认单人/标准/前三幕/排除放弃，A0–A10，纳入所有Mod。通过第三幕视为标准范围胜利；旧局缺失证据保持未知。
- 内容限能天使卡牌、角色遗物、先古遗物。自身/RitsuLib不可排除；支持全部Mod单项排除、常用6项内双项排除、标签黑白名单。
- 只公开聚合统计，不公开逐局或玩家标识。原始记录不预设期限，不自动删除、付费升级或补造指标。
- 网页读静态JSON并本地筛选。目前按维护者选择使用手动发布；原GitHub约15分钟Cron配置仍保留，但已发现漏调度。Cloudflare派发补救暂不启用。页面时间是最后成功生成统计的时间，无变化时不变。

## 开发

`mod/`为C#采集，`web/`为网页，`worker/`为上传/Steam/维护/迁移，`shared/`为统计与编码，`scripts/`为构建发布，`tests/`为回归。网页/Node不进入PCK，只显式编译`stat/mod/`。

在本目录运行：

```powershell
npm ci
npm run build
npm run check
npm test
npm run migrate:local
npm run dev
```

本地使用隔离D1；网站预览还需本地静态测试文件，不由浏览器查询本地上传数据库。不要向生产写测试局。Miniflare在受限Windows沙箱内可能无法访问SQLite，应使用正常本地文件权限运行。

卡图按`tools/card_art_manager/card_art_preview.gd`技能牌范围裁切成WebP，内容哈希更新URL；中英名字、费用、升级描述和分类从源码、本地化自动生成。

## 发布

- main的网页/卡图/本地化/相关源码推送触发`stat-pages.yml`，构建、类型检查、测试后将网页和统计一起部署Pages。Worker静态页面共享该发布，并保留部署时本地资源作回退。
- 纯统计文档修改不触发网站构建；文档里的最新验证统一写项目进度，性能报告保留在忽略目录。
- `stat-data.yml`计划每小时8/23/38/53分运行，数据刷新复用兼容的网站artifact。无变化跳过Pages部署；两个工作流共用维护锁。
- **Worker代码不会随Pages自动部署。** 登录的维护者可运行`npm run publish`完成构建/检查/追加迁移/Worker部署，并触发Pages；先把源码推送main。单独`npm run deploy`只部署Worker。
- 首次积压或明确补维护，手动运行`stat-data.yml`并开启`bootstrap`。普通最多40条/20分钟，bootstrap最多200条/45分钟，每日预算仍生效。
- 私有维护由限定仓库、main、复用工作流、有效期的OIDC授权，不接受任意SQL，不保存长期D1密钥。工坊包另行发布。

## 网页未更新时

1. 看Actions有无新的`Refresh community statistics`，区分未触发、失败、成功但无变化。active配置和手动成功都不证明Cron运行。
2. 日志`publication_maintenance_summary`中的processed为实际变更、unchanged为贡献完全相同、pending为仍有积压；`publication_preserved`保留旧版，`publication_generated`才产生新时间。
3. 查询维护状态，避免执行旧全量统计SQL：

```powershell
npx wrangler d1 execute exusiai-stat --remote --config worker/wrangler.jsonc --command "SELECT revision,catalog_revision,day_reads,day_writes,cooldown,(SELECT COUNT(*) FROM stat_dirty) pending FROM stat_state WHERE id=1;"
```

4. 调度遗漏可手动运行并核对发布；重新启用/保存Cron后必须用真实schedule记录验证恢复。仓库已准备Cloudflare定时器调用GitHub的可选触发器，没有授权时禁用，启用方法如下。
5. 对比两站`data/manifest.json`的修订、时间、SHA256，再校验对应JSON。故障保留上一个有效版本；网页刷新不会启动D1重算。

## 维护命令

### 可选Cloudflare触发（当前不启用）

代码固定调用本仓库main的`stat-data.yml`，bootstrap=false，沿用任务锁及预算；每15分钟的Worker Cron同时做Steam同步和可选派发，Steam/D1失败不会阻断派发。未配置`GITHUB_PUBLICATION_TOKEN`时只做Steam，不调用GitHub；没有公开派发接口。

在GitHub创建fine-grained token，仅选择`AK_Exusiai`仓库，Repository permissions只增加**Actions: Read and write**（Metadata读取自动附带），设定有效期。在stat目录执行：

```powershell
npx wrangler secret put GITHUB_PUBLICATION_TOKEN --config worker/wrangler.jsonc
```

只在本机交互提示中输入令牌，不放入聊天、源码、命令参数或文档。该操作创建并部署Worker新版本，先确保本仓库当前代码已部署。它新增的是Cloudflare Worker触发本仓库Actions的授权，需要维护者明确配置；不存在自动复制CLI登录令牌。

随后核对Cron日志`publication_trigger`的dispatched/200（兼容204）与Actions的workflow_dispatch运行，最后验证新manifest；令牌失效的401/403、网络失败只记安全状态，不重放POST。移除此secret可关闭；GitHub原schedule保留作备用，两种触发共用原锁。不保证GitHub执行/发布精确在15分钟内完成。

### 重建和测量

```powershell
# 原始记录重新入队，保留现有常用双项名单
node scripts/rebuild-stat.mjs --remote
# 只有明确重选名单时添加此选项
node scripts/rebuild-stat.mjs --remote --reset-pair-policy
# 仅转换匹配公开版本修订的汇总payload，不覆盖较新单元
node scripts/compact-stat.mjs --remote
# 本地合成测量，报告写入忽略的.publish/
node scripts/benchmark-stat.mjs
```

重建后需有界bootstrap；预测模型变更要明确迁移，不回溯改写冻结期望。生产编码转换先确认完整发布，工具校验哈希、模式及解码向量一致。不提交凭据、原始数据库、构建产物和临时测量文件。

## Mod文案与待验收

中英授权/设置文案在仓库根目录`AK_Exusiai/localization/statistics/zhs.json`、`eng.json`；RitsuLib跟随游戏语言并回退英文。只保留上传开关和网站入口，不提供删除/刷新身份；不要改公开ApplicantId、RequestId或事件名。

v2包含逐幕、升级、选牌和战斗摘要，旧指标缺失显示“—”。最新Mod包仍需维护者发布，并实战验证跨幕存档、额外幕、联机本机归属、断网重试、局终及撤销授权；本轮不重新构建游戏包。
