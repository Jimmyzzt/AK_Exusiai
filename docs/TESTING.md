# AK_Exusiai 测试清单

## 构建与启动

1. 确认游戏为 `v0.111.0`，游戏 `mods/STS2-RitsuLib` 的清单与 DLL 均为 `0.5.14`。
2. 退出游戏后执行 `dotnet build .\AK_Exusiai.csproj`。
3. 启动游戏并检查日志：RitsuLib 与 `AK_Exusiai` 的补丁应全部成功；自动注册应为 0 项失败。39 项是普通卡阶段的旧基线，不再用于本分支。

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
AK_EXUSIAI_CARD_ARMED_ESCORT
AK_EXUSIAI_CARD_DELAYED_BLAST
AK_EXUSIAI_CARD_MARKSMANSHIP
AK_EXUSIAI_CARD_WELCOME_TO_LATERANO
AK_EXUSIAI_CARD_CROSS_OF_DEVOTION
AK_EXUSIAI_CARD_VIOLENT_DELIVERY
AK_EXUSIAI_CARD_FREE_DELIVERY
AK_EXUSIAI_CARD_RUN
AK_EXUSIAI_CARD_THE_LORDS_PROTECTION
AK_EXUSIAI_CARD_JUNGLE_STYLE
AK_EXUSIAI_CARD_TEXAS_CALL
AK_EXUSIAI_CARD_QUICK_MAG
AK_EXUSIAI_CARD_LOOKING_BACK
AK_EXUSIAI_CARD_FIELD_MODIFICATION
AK_EXUSIAI_CARD_READY_FOR_ACTION
AK_EXUSIAI_CARD_APPLE_PIE
AK_EXUSIAI_CARD_TEXAS
AK_EXUSIAI_CARD_HOLY_CITY_PURGE
AK_EXUSIAI_CARD_FULLY_PREPARED
AK_EXUSIAI_CARD_HOLY_CITY_RADIANCE
AK_EXUSIAI_CARD_LASER_CANNON
AK_EXUSIAI_CARD_GUARANTEED_SUCCESS
AK_EXUSIAI_CARD_PARTY_TIME
AK_EXUSIAI_CARD_OVERLOADING_MODE
AK_EXUSIAI_CARD_FULL_SALVO
AK_EXUSIAI_CARD_DESSERT_TIME
AK_EXUSIAI_CARD_STEADFAST_HEART
AK_EXUSIAI_CARD_PARADISE_LOST
AK_EXUSIAI_CARD_AIM_FOR_THE_WEAK_POINT
AK_EXUSIAI_CARD_OUTSTANDING_GRADUATE
AK_EXUSIAI_CARD_SHOOTOHOLIC
AK_EXUSIAI_CARD_ANGELS_HEART
AK_EXUSIAI_CARD_EMBRACE_NEW_LIFE
AK_EXUSIAI_CARD_LOAN
AK_EXUSIAI_CARD_CHILDHOOD
AK_EXUSIAI_CARD_GUARDIAN_GUN
AK_EXUSIAI_CARD_EXPLOSIVE_AMMO
AK_EXUSIAI_CARD_THE_LORDS_FORGIVENESS
AK_EXUSIAI_CARD_NECKLACE_OF_THE_PRESENCE
AK_EXUSIAI_CARD_LOGISTICS_OUTSOURCE
AK_EXUSIAI_CARD_ANGEL_FORM
AK_EXUSIAI_CARD_CHILLED_DESSERT
AK_EXUSIAI_CARD_PAGANINI_CUSTOM
AK_EXUSIAI_CARD_HOLY_CITY_GUIDANCE
AK_EXUSIAI_CARD_HOLY_CITY_SHELTER
AK_EXUSIAI_CARD_HOLY_CITY_ETERNITY
AK_EXUSIAI_CARD_HOLY_CITY_ICE_CREAM
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
card AK_EXUSIAI_CARD_ARMED_ESCORT
card AK_EXUSIAI_CARD_DELAYED_BLAST
card AK_EXUSIAI_CARD_FIELD_MODIFICATION
card AK_EXUSIAI_CARD_CROSS_OF_DEVOTION
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
- 天意！随机选择一名存活敌人并造成 8（升级后 11）伤害；有弹药时按普通攻击规则消耗 1 发并获得增伤，不显示弹药悬浮说明。
- 德克萨斯与圣城净化像原版灵魂、君王之剑一样注册在 `TokenCardPool`：显示无色牌框，但不进入能天使奖励、普通无色奖励或商店；德克萨斯不扣弹，有弹药时每段伤害享受增伤，0 弹药时不享受。
- 穿透打击只有实际扣弹时才改为命中所有敌人；原目标不会被重复命中。
- 穿甲弹、精准打击与德克萨斯的描述不显示原始 `{VulnerablePower...}`、`{WeakPower...}` 或其他未解析占位符；穿甲弹与精准打击先造成伤害，再分别施加易伤与虚弱，0 弹药时不施加。
- 所有格挡描述中的“格挡”均标黄；穿透打击、穿甲弹和精准打击条件句中的“弹药”均标黄。
- 只有描述提到弹药的牌显示弹药悬浮说明：穿透打击、穿甲弹、精准打击、射击技巧、德克萨斯和全弹发射应显示；天意！、延时爆炸、拉特兰欢迎你、虔心十字、暴力运输等不应显示。
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

## 稀有牌首版优先回归

- 准备充足！：普通 10 张手牌上限以及被效果改变后的手牌上限，都只在恰好满手时可打出；基础/升级伤害为 10×5/12×5。
- 激光炮？、过载模式、全弹发射！：分别测试 0、1、上限和大量弹药；每发弹药都强化每一段。前两者最多消耗 5 发，全弹发射消耗全部现有弹药。再叠加临时改装与帕格尼尼定制验证总增伤。
- 开火成瘾症：测试 0–6 发弹药、攻击牌不足 5 张、单体与群体攻击混合、自动牌自身为多弹攻击；所有自动攻击均获得同一份预付增伤且不二次耗弹。
- 优秀毕业生：选择带被动能力且当前意图为攻击的敌人；被动在随后敌方回合不触发，意图仍执行，攻击伤害仅乘 90%，敌方回合结束后恢复。
- 爆弹：分别用单体、群体、多段攻击测试；原攻击照常命中，额外群伤按每段弹药增伤触发，并计入临时改装与帕格尼尼定制。德克萨斯等完全未耗弹的免费攻击不触发；开火成瘾症预付过弹药的自动攻击触发。
- 守护铳、显圣吊坠：用普通单发与多弹药攻击测试累计阈值、余数、一次消耗多发时的抽牌和格挡。
- 天使之心：确认手牌与抽牌堆各获得五种未升级天使牌；基础版只覆盖当前回合，升级版覆盖当前与下回合，期间新生成的天使牌也为 0 费。
- 拥抱新生：测试 X=0、X=2 和升级版 X=0；允许重复随机，生成牌未升级并带迅捷 1。
- 天使形态：测试原有牌、后续生成牌、快递自动打出的牌、普通消耗牌、直接被其他效果消耗的牌和能力牌。只有打出后消耗的非能力天使牌额外结算一次，第二次不递归且只留下一个实体消耗记录。
- 圣城指引、圣城净化、圣城庇护、圣城永恒、圣城冰淇淋：确认均为不可进入奖励/商店的无色衍生牌；“天使”独占首行；升级数值、保留/消耗、能量图标、生命不低于 1 与跨回合时序正确。
- 童年、失乐园：先随机费用再应用童年减费；天使最低费用锁定仍有效。失乐园先获得能量和抽牌，后续抽牌才受本回合混乱影响。
- “主”的宽恕：只改变原本不可打出的诅咒；固定为 1 费、可打出并消耗，不影响原本可打出的诅咒。
- 使命必达！、物流外包、冰镇甜品！、帕格尼尼定制：回归快递选择、自动打出、升级费用/层数，以及多余能量与回合开始弹药；确认固定快递牌不再于启动时触发规范模型异常。
- 其余低耦合牌：圣城威光、派对时间！、甜品时间！、坚定之心、瞄准弱点！、借贷分别核对基础/升级数值与生成牌落点。

## 本轮优先重测卡牌

`德克萨斯`、`圣城净化`、`穿透打击`、`精准打击`、`穿甲弹`、`延时爆炸`、`临时改装`、`拉特兰欢迎你`、`射击技巧`、`虔心十字`、`暴力运输`、`并联弹匣`、`准备万全！`、`苹果派！`。
