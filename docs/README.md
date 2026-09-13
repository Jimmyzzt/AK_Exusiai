# 文档目录

玩家简介、安装与反馈入口见 [仓库首页](../README.md)。本目录将当前操作说明与历史设计分开维护。

## 当前文档

| 文档 | 内容 |
| --- | --- |
| [开发说明](DEVELOPMENT.md) | 环境搭建、构建、资源导出与贡献流程 |
| [当前进度](PROGRESS.md) | 已实现内容、验证证据、发布前待办 |
| [游戏与框架基线](FRAMEWORK_AUDIT.md) | 游戏 API、RitsuLib 与 Mod 清单的版本依据 |
| [控制台测试命令](EXUSIAI_TEST_COMMANDS.md) | 专用测试战斗、弹药、天使与物流测试 |
| [工坊发布准备](WORKSHOP_RELEASE.md) | 宣传素材清单、共同作者、上传及更新流程 |
| [工坊中文介绍草稿](workshop/description.zh-CN.md) | 面向玩家的中文介绍 |
| [工坊英文介绍草稿](workshop/description.en.md) | 对应英文介绍 |
| [素材与致谢](CREDITS.md) | 现有来源记录、署名及许可边界 |
| [协作开发指南](../AGENTS.md) | 自动化开发和代码维护约定 |

## 设计与制作

- 当前只读设计源：[卡牌 V1.2](AK_Exusiai-Card_V1.2.csv)、[遗物与药水 V1.2](AK_Exusiai-Relic_Potion_V1.2.csv)。CSV 的最新基础/升级行是实现依据；发现规则歧义先确认，勿让历史说明覆盖新表。
- [制作工具总览](../tools/README.md)：图标生成、Spine 和制作工具入口。
- [卡图管理器](../tools/card_art_manager/README.md)：构图、源素材、卡图导出。
- [卡牌特效与音效管理器](../tools/card_effect_manager/README.md)：效果配置、原版效果复用和游戏内试播。

## 历史资料

- [V1.2 平衡调整](archive/changelog_v1.md)：文件名沿用历史命名，正文记录 V1.2 调整。
- [V0.1 平衡调整](archive/V0.1_BALANCE_CHANGES.md)。
- [截至 2026-09-10 的开发记录](archive/PROGRESS_2026-09-10.md)：保留构建、测试和旧设计讨论的上下文。
- [特效管理器最初方案](archive/CARD_EFFECT_MANAGER_PLAN.md)：用于解释设计由来，不作为当前使用手册。
- [旧版设计表](archive/)：保留比对依据，不再用于当前生成或同步。

更新规则：玩家看到的变化写进更新记录；当前验证和未完成工作写进进度页；长期开发规则写进 AGENTS。README 不再承载历史分支与任务流水账。
