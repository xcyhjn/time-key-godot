# ADR 0009：确定性牌区、正式回合资源与互斥战斗结算

> 状态：Accepted
> 日期：2026-08-02

## 背景

Wave 02B3 已冻结单一 lifecycle runner，但 reserved 02B4 hook 仍为 no-op。Godot 的弃手和时间币发生在 Timeline 结算前，抽牌与 Era/phase 推进发生在回合开始状态后；同时 Godot 只自动判定战斗胜利，没有统一的失败谓词或胜负互斥事务。

## 决策

### 1. 三种身份分离

- card stable ID 表示内容类型，例如 `lighting`。
- card-instance identity 表示本场战斗中的一张实体卡，在 deck/hand/discard 移动中不变。
- action identity 表示一次已提交行动，同一卡实例再次打出也必须获得新 action identity。

牌区移动或 CardView 销毁不得改变已提交 action 的 immutable display snapshot。

### 2. 显式 seed 拥有全部牌库随机性

starter deck 从 Godot 的 12 张有序输入建立。战斗牌库使用自己的确定性随机状态；不得读取 Unity frame time、`UnityEngine.Random` 或进程全局随机。抽牌堆顶为数组尾端，hand limit 为 7，正式请求抽 5；空 deck 只在 discard 非空时洗回一次，双空返回 typed exhausted。

### 3. reserved hook 是唯一 02B4 事务入口

EndTurnRequested 时先冻结 lifecycle sequence、Timeline occupied cells、剩余 hand instance IDs 和 action display snapshots。ADR 0008 的 Timeline、building、clear、status 顺序保持不变；reserved hook 按以下顺序恰好执行一次：

```text
discard frozen remaining hand
-> award timecoins from frozen (36 - occupied cells)
-> advance phase, rolling 8 to next Era / phase 1
-> reshuffle discard only when deck is empty
-> draw up to 5 without exceeding hand 7
```

InitialStart 只初始化/洗牌/抽 5，不推进 phase、不发时间币。hook 成功后才刷新 intent；重复 sequence 返回第一次的 typed result，不重复写状态。

### 4. 终局与返回边界是 Domain 事务

战斗 outcome 至少为 Active、Victory、Defeat。第一次终局转换胜出；重复同结果幂等，相反结果返回 typed conflict。终局后输入锁定，不再接受 action/EndTurn，也不重复发 reward entry。

Godot 没有正式失败谓词，因此 Unity 只暴露显式 typed defeat command，不发明玩家 HP 或回合限制。Victory 产生一次最小局内 reward entry。Unity typed return payload 携带 outcome、Era/phase、timecoins、deck stable-ID snapshot 和 battle context；完整 shop/acquire/remove/craft 与 OutScene 仍留在 Godot。

## 后果

Presentation 只能消费 typed snapshot/result，不能推导牌区、资源或胜负。延后到 reserved hook 的写入通过 EndTurn 全程输入锁和 pre-resolution snapshot 保持最终可观察语义，同时避免修改 ADR 0008 或创建第二套 lifecycle。
