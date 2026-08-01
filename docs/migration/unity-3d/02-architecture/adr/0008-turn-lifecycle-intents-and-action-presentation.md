# ADR 0008：统一回合生命周期、敌方意图与行动表现身份

> 状态：Accepted
> 日期：2026-08-02

## 背景

Remaining Cards 已提供普通卡牌、Clear、occupant 和 before/after 结果，但没有统一的回合推进入口。固定 enemy marker、Tower 自损、Poison tick 和 UI 清空若分别由 Controller、动画或 Presenter 驱动，会产生重复结算、失效意图仍执行、同一多格 action 重复显示，以及状态和场景对象不同步。

## 决策

### 1. Application 只有一个阶段编排入口

`CombatTurnLifecycleCoordinator` 使用一个 `TurnLifecycleRunner`，阶段固定为：

```text
PlayerReady
-> EndTurnRequested / InputLocked
-> ResolvingTimeline
-> RunningBuildingBehaviors
-> ClearingTimeline
-> ProcessingTurnStartStatuses
-> 02B4 hook（当前 no-op）
-> RefreshingEnemyIntents
-> PlayerReady / InputUnlocked
```

初始开局只执行状态、02B4 no-op hook 和意图生成。每个 phase 每个 sequence 只前进一次；重复输入和重复表现刷新不能再次执行 action、Tower decay 或 Poison tick。

### 2. Action identity 是跨层追踪键

玩家 action 与 enemy intent 都有稳定的 action identity、actor、source/target、shape、effect display payload 和 occupied cells。Timeline 仍按 x 后 y 排序并按 identity 去重。同一 action 的多格只对应一个保存 Prefab 实例；Clear、失效、source removal 和 lifecycle clearing 均按 identity 移除整组表现。

卡牌、时间轴 action frame、地图 source/target/range 和详情框消费同一只读 snapshot。Presentation 只维护 `identity -> View` 映射，不重新计算目标、范围、效果或合法性。

### 3. MIG-002 保持显式而可观察

敌人意图以显式 seed、priority、shape 和最多五个候选确定性生成，并在执行前重判 source、target、shape 与 effect。当前 Godot 权威 command 为空时，结果为 `UnsupportedSourceCommand` 的成功 no-effect：trace、中文详情框和 action frame 都可见，但不伪造伤害 0 的攻击。失效意图完整移除，直到下一次刷新阶段才重新生成。

### 4. Tower、Poison 与死亡共享 occupant transaction

building phase 从已结算后的稳定 occupant 快照执行。新建 Tower 因此在创建当回合从 HP100 变为 HP50，下一周期 HP50 变为 0 并按 `Remove` 策略清 registry、tile slot、状态和 View。一般敌人采用冻结的 `RemainBroken`，Tower/Radar underling 采用 `Remove`。

Poison 在回合开始使用同一全图快照完成传播、快照伤害和衰减三个 pass。新感染不在同一周期受伤；死亡与 Tower 使用相同的 typed removal result。Presenter 按 runtime ID 和 lifecycle sequence/phase 幂等消费结果。

### 5. 保存资产承载外观

`CardEffectFrame.prefab` 和 `TimelineActionFrame.prefab` 是动态详情/行动实例的保存来源；稳定 action layer、effect host 和 Inspector 引用在 Play 前存在于 Scene。玩家 frame 使用 teal 编码，enemy frame 使用 amber stripe/徽标冗余，不能只依赖颜色。Card/Tower/Poison 与所有玩家可见文字使用 Silver；`TextMesh` 必须同时引用 Silver Font 和对应字体材质。

## 后果

回合顺序、敌方意图、建筑与状态现在可以从不可变结果完整追踪到 Scene 表现，UI 不再决定规则执行。新增 building/status/intent handler 必须登记到现有阶段，不得新建自己的计时器；02B4 只能接入保留 hook。代价是 snapshot/result 类型增加，但它消除了目标重算和跨层第二份真相。
