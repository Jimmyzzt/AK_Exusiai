# 能天使 · Exusiai

将《明日方舟》的能天使带入《Slay the Spire 2》：装填弹药、倾泻火力，借助天使的祝福与企鹅物流的支援攀登高塔。

本项目为非官方、非盈利同人角色 Mod，包含简体中文与英文本地化。目前处于 V1.2 版本线，正在准备 Steam 创意工坊发布。

![能天使角色选择背景](AK_Exusiai/images/character/appearance/Exusiai.png)

## 玩法特色

- **弹药与过载**：攻击消耗弹药获得额外伤害；把弹药积攒到上限，进入过载，打出一轮猛烈的连续攻击。
- **天使**：赋予卡牌保留与首次免费打出的机会，搭配圣城衍生牌组织连招。
- **快递与中转**：暂时寄出遗物换取收益，再通过中转调度临时遗物，在眼前的爆发与后续战斗之间取舍。
- **干扰与沉默**：压制敌人的攻击与被动能力，为队伍创造输出空间。
- **企鹅物流支援**：大帝、德克萨斯、可颂等伙伴以物流卡登场；另有面向多人合作的卡牌。

## 包含内容

按当前源码统计，共 **104 种卡牌、9 件角色遗物、3 种角色药水**。卡牌包括 91 种角色卡池卡牌（含初始与先古牌）、11 种衍生牌和 2 种诅咒；并非 104 张都会进入普通卡牌奖励。

角色提供能天使与新约能天使两组外观，共六套造型，配有战斗、休息、商店动画，以及自定义光环苹果能量指示器、转场和音效。两组外观使用同一套玩法。

具体效果以游戏内卡牌为准。版本调整见 [V1.2 更新记录](docs/archive/changelog_v1.md)，设计表及开发资料见 [文档目录](docs/README.md)。

## 安装与版本

当前已验证的开发基线为 **游戏 public beta v0.111.0、RitsuLib 0.5.20、Windows**。这不是对所有后续游戏版本、正式分支或其他平台的兼容承诺；发布时会注明对应游戏版本。

工坊页面上线后将在这里补充订阅链接。前置依赖为 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)。

如果使用维护者提供的手动安装包，将 `AK_Exusiai.dll`、`AK_Exusiai.pck`、`AK_Exusiai.json` 放在游戏的 `mods/AK_Exusiai/` 目录，并安装匹配的 RitsuLib。GitHub 的 **Download ZIP 是源码**，不能直接作为游戏安装包。

多人游戏时，各方应使用相同的游戏版本、Mod 版本及依赖，并核对内容 Mod 的加载顺序。多人相关问题仍欢迎反馈。

## 反馈问题

请通过 [GitHub Issues](https://github.com/Jimmyzzt/AK_Exusiai/issues) 提交反馈，附上：游戏版本、Mod 与 RitsuLib 版本、单人或多人、涉及卡牌及升级状态、复现步骤，以及截图或日志。

游戏内控制台输入 `open logs` 可打开日志目录。优先提供发生问题那次运行的 `godot.log`；如果后来又启动过游戏，需同时查看带日期的轮转日志。多人不同步时，请附上对应的 `ritsulib_state_divergence_*.zip`，尽量收集双方的记录。公开日志前可遮去不愿公开的用户名或本机路径。

## 开发与贡献

源码使用 C#、.NET 9 和 Godot 4.5.1 Mono，基于 RitsuLib。你可以阅读实现、提交问题或发起 Pull Request；较大的机制调整建议先讨论设计。

准备好本机游戏与 Godot 后，复制 [local.props.template](local.props.template) 为 `local.props` 并填写路径。完整构建会编译、导出 PCK 并部署到本机游戏目录：

```powershell
dotnet build .\AK_Exusiai.csproj
```

完整构建前请退出游戏。首次搭建环境还需配置 Spine 编辑器扩展与 RitsuLib 运行时，步骤见 [开发说明](docs/DEVELOPMENT.md)。卡图和卡牌特效编辑器见 [制作工具](tools/README.md)。

## 素材与致谢

感谢 Mega Crit、《明日方舟》的创作者、RitsuLib 和社区工具的维护者，以及参与测试和反馈的玩家。

角色、美术、音效与第三方工具各有其来源和权利归属，详见 [素材与致谢](docs/CREDITS.md)。仓库目前未设置统一代码许可证；素材也不因源码公开而自动获得新的使用许可。
