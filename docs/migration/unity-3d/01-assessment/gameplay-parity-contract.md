# 玩法等价契约

> 状态：首切片契约已冻结
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Godot 实跑、README、Timeline/Drag/Effect 核心代码

## 不可漂移的规则

1. 卡牌稳定 ID 使用字符串 basename/`name`；数字 `id` 仅作内容字段和排序。
2. 时间轴固定 12 列×3 行，坐标原点为左上，`x` 向右，`y` 向下。
3. 结算按 `x=0..11`，同列按 `y=0..2`；一个多格 action 只执行一次。
4. 卡牌交互语义为选卡→选世界目标→选择/旋转时间轴形状→确认。
5. 形状只有字符 `1` 占格，裁剪到最小包围盒；空形状普通卡回退单格。
6. 玩家、敌人意图、建筑行动的顺序以当前实跑/代码为基线，不按描述性文档擅自修正。
7. 所有随机行为必须可注入 seed。
8. 3D 改变呈现，不改变伤害值、胜利条件、卡牌 ID 和结算顺序。

## 首切片 fixture

```text
Card: lighting
Effect: damage 100（首切片缩放目标 HP 到 10 时使用同一伤害语义，结果为 0）
Range offsets: (0,0),(1,0),(2,0)
Shape: one occupied slot
Timeline: 12×3
Target: one enemy unit/tile
Enemy intent: one visible occupied slot/action record
Seed: fixed constant 731
```

## 可允许差异

- 2D 像素地块改为 3D 白盒 mesh、灯光和阴影。
- 动画时长、颜色和过渡材质可不同，但状态变化必须可观察。
- uGUI 控件布局可重排以适应 1280×720，但信息内容和输入结果不变。
- 首切片敌人意图可以使用固定 fixture，不复刻当前随机候选选择器。

## 明确未实现

- 完整 7 张卡、clear/建造/毒/治疗/高度全套命令。
- 完整敌方建筑行为、胜负、奖励、局外地图、教程、旧档导入。

## 对照快照字段

每次结算至少输出：

```json
{
  "turn": 1,
  "phase": "resolved",
  "timeline": [{"origin":[0,0],"kind":"player","cardId":"lighting"}],
  "targetHpBefore": 10,
  "targetHpAfter": 0,
  "enemyIntentResolved": true,
  "seed": 731
}
```
