# AK_Exusiai 测试清单

## 构建与启动

1. 确认游戏为 `v0.111.0`，游戏 `mods/STS2-RitsuLib` 的清单与 DLL 均为 `0.5.14`。
2. 退出游戏后执行 `dotnet build .\AK_Exusiai.csproj`。
3. 启动游戏并检查日志：RitsuLib 补丁应全部成功；`AK_Exusiai` 自动注册应为 11 项成功、0 项失败。

## 公开 ID

```text
AK_EXUSIAI_CHARACTER_EXUSIAI
AK_EXUSIAI_CARD_EXUSIAI_STRIKE
AK_EXUSIAI_CARD_EXUSIAI_DEFEND
AK_EXUSIAI_CARD_CHARGING_MODE
AK_EXUSIAI_CARD_LOCKED_AND_LOADED
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
upgrade 0
```

至少确认：

- 角色选择界面显示能天使、77 生命、99 金币、背景 CG 和初始遗物。
- 开战后证章把弹药从 0 增加到 4，且弹药计数器可见。
- 0 弹药时打击造成 6，冲锋模式造成 3×3，攻击牌仍可打出。
- 1 弹药时打击消耗 1 发并造成 8；冲锋模式消耗 1 发并造成 5×3，而不是消耗 3 发。
- 子弹上膛获得 3（升级后 5）发弹药；数值可以超过常见计数但不溢出。
- 升级打击为 9、防御为 8、冲锋模式为 4×3。
- Replay、自动打出或复制形成的每个实际出牌实例分别消耗弹药；一张牌内部的多条攻击命令仍只消耗 1 发。
- 战斗结束并进入下一场战斗前，上一场剩余弹药不会保留；新战斗只获得证章提供的 4 发。

当前自动验证只覆盖编译、PCK 导出、Mod 初始化、注册和本地化合并；上述交互与伤害数值仍需在可视游戏内逐项确认。
