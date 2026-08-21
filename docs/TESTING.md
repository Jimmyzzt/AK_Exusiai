# AK_Exusiai 测试清单

## 构建与启动

1. 确认游戏为 `v0.111.0`，游戏 `mods/STS2-RitsuLib` 的清单与 DLL 均为 `0.5.14`。
2. 退出游戏后执行 `dotnet build .\AK_Exusiai.csproj`。
3. 启动游戏并检查日志：RitsuLib 补丁应全部成功；`AK_Exusiai` 自动注册应为 39 项成功、0 项失败。

## 公开 ID

```text
AK_EXUSIAI_CHARACTER_EXUSIAI
AK_EXUSIAI_CARD_EXUSIAI_STRIKE
AK_EXUSIAI_CARD_EXUSIAI_DEFEND
AK_EXUSIAI_CARD_CHARGING_MODE
AK_EXUSIAI_CARD_LOCKED_AND_LOADED
AK_EXUSIAI_CARD_TRAVEL_LIGHT
AK_EXUSIAI_CARD_PIERCING_STRIKE
AK_EXUSIAI_CARD_ARMOR_PIERCING_ROUND
AK_EXUSIAI_CARD_PRECISION_STRIKE
AK_EXUSIAI_CARD_FIRE_ESCORT
AK_EXUSIAI_CARD_DELAYED_BLAST
AK_EXUSIAI_CARD_SHOOTING_TECHNIQUE
AK_EXUSIAI_CARD_WELCOME_TO_LATERANO
AK_EXUSIAI_CARD_DEVOUT_CROSS
AK_EXUSIAI_CARD_VIOLENT_DELIVERY
AK_EXUSIAI_CARD_FREE_DELIVERY
AK_EXUSIAI_CARD_RUN
AK_EXUSIAI_CARD_THE_LORDS_PROTECTION
AK_EXUSIAI_CARD_JUNGLE_STYLE
AK_EXUSIAI_CARD_TEXAS_CALL
AK_EXUSIAI_CARD_QUICK_MAGAZINE
AK_EXUSIAI_CARD_LOOKING_BACK
AK_EXUSIAI_CARD_TEMPORARY_MODIFICATION
AK_EXUSIAI_CARD_READY_FOR_ACTION
AK_EXUSIAI_CARD_APPLE_PIE
AK_EXUSIAI_CARD_TEXAS
AK_EXUSIAI_CARD_HOLY_CITY_PURIFICATION
AK_EXUSIAI_RELIC_EXUSIAI_BADGE
AK_EXUSIAI_SECONDARY_RESOURCE_AMMO
```

## 控制台测试

进入能天使战斗后可使用：

```text
sresource get AK_EXUSIAI_SECONDARY_RESOURCE_AMMO
sresource set AK_EXUSIAI_SECONDARY_RESOURCE_AMMO 0
sresource set AK_EXUSIAI_SECONDARY_RESOURCE_AMMO 1
sresource set AK_EXUSIAI_SECONDARY_RESOURCE_AMMO 99
card AK_EXUSIAI_CARD_LOCKED_AND_LOADED
card AK_EXUSIAI_CARD_CHARGING_MODE
card AK_EXUSIAI_CARD_EXUSIAI_STRIKE
card AK_EXUSIAI_CARD_TRAVEL_LIGHT
card AK_EXUSIAI_CARD_FIRE_ESCORT
card AK_EXUSIAI_CARD_DELAYED_BLAST
card AK_EXUSIAI_CARD_TEMPORARY_MODIFICATION
card AK_EXUSIAI_CARD_DEVOUT_CROSS
card AK_EXUSIAI_CARD_TEXAS_CALL
upgrade 0
```

至少确认：

- 角色选择界面显示能天使、77 生命、99 金币、背景 CG 和初始遗物。
- 开战后证章把弹药从 0 增加到 4，且弹药计数器可见。
- 0 弹药时打击造成 6，冲锋模式造成 3×3，攻击牌仍可打出。
- 1 弹药时打击消耗 1 发并造成 8；冲锋模式消耗 1 发并造成 5×3，而不是消耗 3 发。
- 打击和防御分别显示战士的原版打击、原版防御卡图；顶部角色头像显示 `Exusiai_icon.png`。
- 子弹上膛获得 3（升级后 5）发弹药；数值可以超过常见计数但不溢出。
- 升级打击为 9、防御为 8、冲锋模式为 4×3。
- Replay、自动打出或复制形成的每个实际出牌实例分别消耗弹药；一张牌内部的多条攻击命令仍只消耗 1 发。
- 战斗结束并进入下一场战斗前，上一场剩余弹药不会保留；新战斗只获得证章提供的 4 发。
- 轻装上阵先获得 1 发弹药再造成伤害；0 弹药起手也享受本次 +2 增伤，且不扣弹。
- 德克萨斯与圣城净化显示无色牌框且不进入能天使普通牌奖励池；德克萨斯不扣弹，有弹药时每段伤害享受增伤，0 弹药时不享受。
- 穿透打击只有实际扣弹时才改为命中所有敌人；原目标不会被重复命中。
- 穿甲弹、精准打击与德克萨斯的描述不显示原始 `{VulnerablePower...}`、`{WeakPower...}` 或其他未解析占位符；穿甲弹与精准打击先造成伤害，再分别施加易伤与虚弱，0 弹药时不施加。
- 拉特兰欢迎你的卡面伤害随弹药及临时改装实时变化，多段次数仍正确显示为 3（升级后 4）。
- 延时爆炸在打出时冻结攻击方修正后的单段伤害：无弹药时两次均为 8（升级后 11）；有弹药和基础临时改装时两次均为 12。下回合的弹药、临时改装或其他攻击方修正不再改变第二次伤害。
- 延时爆炸挂起图标显示冻结后的实际伤害数值，文本不再额外说明“不消耗弹药”。
- 延时爆炸挂起期间保存并重新载入战斗，第二次伤害仍会在下回合正常触发。
- 临时改装可叠加；本回合每段弹药增伤额外增加 2（升级后 3），回合结束移除。
- 射击技巧和并联弹匣产生的“下回合弹药”使用 `AmmoNextTurnPower.svg` 新图标，小图标和详情图标均清晰。
- 火力护送、免费派送可跳过选择；目标牌获得保留、消耗和快递层数。多个来源的快递层数相加。
- 手牌中的快递在每个玩家回合结束时减少 1；归零后立即免费自动打出，单体目标由游戏随机选择；不可打出的牌按原版自动打出规则进入结果牌堆。
- 暴力运输在没有快递牌时仍正常造成伤害；选择快递 1 的牌后应立即触发自动打出。
- 虔心十字与德克萨斯！始终生成未升级的衍生牌。
- 准备万全的 1 点能量显示为 1 个能量图标；苹果派的 4（升级后 5）点能量显示为“数字+能量图标”。
- 圣城净化允许选择 0 张牌，基础版至多 1 张、升级版至多 2 张；无论选择几张都获得 3（升级后 4）发弹药；其 0 费不能被混乱或重构提高。
- 后续有原始费用大于 0 的天使牌时：若本场战斗费用从 3 降至 1，之后不能回升超过 1；再次降至 0 后，本场战斗保持 0。保存并读取战斗后最低费用记录不丢失。
- 击杀全部敌人后应正常进入奖励界面，并能从 20 张普通牌中生成 3 个不同候选。

当前自动验证只覆盖编译、PCK 导出、Mod 初始化、注册和本地化合并；上述交互与伤害数值仍需在可视游戏内逐项确认。

## 本轮优先重测卡牌

`德克萨斯`、`圣城净化`、`精准打击`、`穿甲弹`、`延时爆炸`、`临时改装`、`拉特兰欢迎你`、`射击技巧`、`并联弹匣`、`准备万全！`、`苹果派！`。
