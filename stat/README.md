# 能天使社区统计

首版已实现并部署：[主站](https://exusiai.zzt.si/) · [GitHub Pages](https://jimmyzzt.github.io/AK_Exusiai/)。两站共用 Worker API，只公开聚合统计。Mod 上传需玩家在 RitsuLib 中授权，不补传历史存档。

目录：

- `mod/`：C# 上传集成，由 Mod 项目显式编译。
- `web/`：GitHub Pages 与 Worker 共用的网页。
- `worker/`：上传和聚合查询 API、D1 迁移。
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

停止本地开发服务器后，使用已登录的 Cloudflare/GitHub 账号执行 `npm run publish`，从同一份构建发布 Worker 与 `gh-pages`；不会推送源码分支。单独 `npm run deploy` 只更新 Worker。

Mod 已接入入口并通过 C# 构建、提取和上传适配器测试；2026-10-07 临时启用后的游戏启动已确认遥测申请成功注册，测试后原设置恢复。真实对局的授权、上传、离线重试和联机归属仍需游戏内验收。工坊包尚未发布。

遥测申请和统计设置文案统一位于 `AK_Exusiai/localization/statistics/zhs.json`、`eng.json`（相对仓库根目录）。其中 `consent` 为权限说明，`title` 为申请名称。RitsuLib 的 I18N 使用“目录/语言.json”布局，自动跟随游戏语言并回退英文。设置仅有上传授权开关和网站入口；已移除删除数据、身份刷新及相关网页/API。不要修改已有 `ApplicantId`、`RequestId` 或事件名，否则会影响授权/队列关联。

本目录由 `.gdignore` 和导出排除项隔离于 Godot 资源扫描；网页及 Node 依赖不进入游戏包。不要提交凭据、玩家原始记录、数据库导出或构建缓存。
