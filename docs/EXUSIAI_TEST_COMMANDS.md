# 能天使控制台测试命令

所有命令都以 `exusiai` 为统一入口，并且只在战斗中生效。输入 `exusiai help` 可在控制台内查看简明帮助；命令与参数支持 Tab 补全。

## 物流牌整手测试

```text
exusiai logic
exusiai logic upgraded
```

- 将当前能量准确设置为 100。
- 将当前手牌移出战斗，并换成全部 8 张物流衍生牌。
- 不带参数时生成基础版；使用 `upgraded` 时全部升级。
- `exusiai logistics` 是 `exusiai logic` 的等价别名。

## 查看手牌索引与状态

```text
exusiai hand
```

输出当前能量、弹药，以及每张手牌的从 0 开始的索引、公开 ID、基础重放、快递、天使和升级状态。后续针对单张手牌的命令都使用这里显示的索引。

## 添加重放

```text
exusiai replay 0 10
```

给第 0 张手牌增加 10 层基础重放。该命令是“增加”而不是“设置”；为避免误输入导致一张牌执行过多次，单张牌的基础重放总数上限为 100。

## 设置弹药

```text
exusiai ammo 22
```

将当前弹药准确设置为指定值，可用范围为 0–30。Tab 补全包含多段攻击与火力阈值测试常用的 `0/5/10/16/22/30`。

## 添加快递

```text
exusiai delivery 0 7
```

给第 0 张手牌增加 7 层快递，可用范围为 1–99。该命令走正式的快递添加流程，因此适合测试卡牌角标和相关联动。

## 推荐组合

```text
exusiai logic upgraded
exusiai hand
exusiai delivery 0 7
exusiai replay 0 10
exusiai ammo 22
```

这组命令会建立升级物流牌整手，再为其中一张牌添加快递和重放，并准备 22 发弹药。需要测试别的组合时，可先用 `exusiai hand` 重新确认手牌索引。
