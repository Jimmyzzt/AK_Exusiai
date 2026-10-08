# 能天使社区统计

网站：[主站](https://exusiai.zzt.si/) · [GitHub Pages](https://jimmyzzt.github.io/AK_Exusiai/)。本轮改为两站展示 GitHub Pages 发布的聚合 JSON，浏览器本地筛选；生产迁移状态见[实施报告](docs/STATIC_PUBLICATION.md)。Mod 上传需玩家在 RitsuLib 中授权，不补传历史存档。

目录：

- `mod/`：C# 上传集成，由 Mod 项目显式编译。
- `web/`：GitHub Pages 与 Worker 共用的网页。
- `worker/`：上传、Steam同步、受控汇总维护与导出、D1迁移。
- `docs/`：统计方法、隐私和部署文档。
- `scripts/`：生成内容目录、构建网页和双站发布。
- `tests/`：Worker/D1 与 Mod 数据提取测试。

开发与发布说明见 [实施说明](docs/IMPLEMENTATION.md)，确认范围见 [PLAN.md](PLAN.md)。在本目录运行：

```powershell
npm ci
npm run build
npm run check
npm test
npm run migrate:local
npm run dev
```

静态发布、迁移、OIDC与重建操作见 [实施报告](docs/STATIC_PUBLICATION.md)。首次上线需执行有界 bootstrap。停止本地开发服务器后，使用已登录的 Cloudflare/GitHub 账号执行 `npm run publish`，发布 Worker 并触发 main 的 GitHub Pages 工作流；请先提交推送相应源码。卡图、本地化和网页推送后自动更新 Pages，Worker 静态页面共享这一发布，无需 Cloudflare CI 密钥。单独 `npm run deploy` 只更新 Worker。

统计 v2 已扩展新局的逐幕、升级版、选牌和战斗摘要；旧局保留，缺失指标显示“—”。最新采集包仍需在游戏中验证跨幕存档、联机和局终上传，并由维护者发布工坊。算法和参考站差异见 [指标方法](docs/METRICS.md)。

遥测申请和统计设置文案统一位于 `AK_Exusiai/localization/statistics/zhs.json`、`eng.json`（相对仓库根目录）。其中 `consent` 为权限说明，`title` 为申请名称。RitsuLib 的 I18N 使用“目录/语言.json”布局，自动跟随游戏语言并回退英文。设置仅有上传授权开关和网站入口；已移除删除数据、身份刷新及相关网页/API。不要修改已有 `ApplicantId`、`RequestId` 或事件名，否则会影响授权/队列关联。

本目录由 `.gdignore` 和导出排除项隔离于 Godot 资源扫描；网页及 Node 依赖不进入游戏包。不要提交凭据、玩家原始记录、数据库导出或构建缓存。
