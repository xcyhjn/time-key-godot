# Turn Lifecycle Gate 0 总结

> 结果：PASS
> 日期：2026-08-02
> 分支：`unity_7.31`

## 工作区与前置成果

- Gate 0 基线为本地领先远端 2 个提交；没有切分支、stash、reset 或回退。
- Remaining Cards Gate D 继承 EditMode `152/152`、PlayMode `38/38`、54 PNG、Windows build 与 Player smoke。
- 简体中文/Silver 已由两个独立提交形成基线，继承 EditMode `161/161`、PlayMode `38/38`、8 PNG、build `210916374` bytes 与 Player smoke。
- Application EditMode `25/25`、公共 Scene PlayMode `15/15` 的 Gate 0 最小冒烟通过；测试写入临时目录，没有刷新历史证据。
- `default_bus_layout.tres`、migration roadmap、5 张历史 targeting PNG、Godot reward/shader、两个 ProjectSettings、当前阶段 Prompt、三个来源不明后继 Prompt 全部保持受保护、未暂存。

## 冻结顺序与身份

```text
PlayerReady
  -> EndTurnRequested / InputLocked
  -> ResolvingTimeline (x then y, dedupe by ActionId)
  -> RunningBuildingBehaviors
  -> ClearingTimeline
  -> ProcessingTurnStartStatuses
  -> 02B4NoOpHook
  -> RefreshingEnemyIntents
  -> PlayerReady / InputUnlocked
```

`CombatSessionPhase` 继续只表示卡牌交互。新 lifecycle phase 不与它混用。ActionId 是卡牌、玩家/敌方 Timeline frame、地图 source/target/range 与详情框之间唯一身份；所有表现消费同一不可变 `TimelineActionPresentationSnapshot`。

## 源语义裁决

- enemy command 为空，输出真实 intent + 显式 `UnsupportedSourceCommand`。
- intent 高优先级先放、同级显式 seed、最多 5；保留 literal shape，包括 Village `011` 的前导空位。
- Tower 创建当回合 `100 -> 50`，下一轮 `50 -> 0`，使用 `Remove` 与原子占用清理。
- Poison 使用全局 snapshot 的 spread/damage/decay 三 pass；新感染本轮隔离。
- 普通敌人 `RemainBroken`；Tower/Radar underling typed `Remove`。

## 交互与 authoring

- 优先级固定为 resolving/disabled > card targeting > scheduling/drag/clear > idle hover > none。
- Scene 在 Play 前保存 EffectFrameHost、TimelineActionLayer、tooltip/overlay anchor 和 Presenter 引用；动态 action/intent frame 必须从保存 Prefab 创建。
- Card layout/style、hand width/spacing/selected reserve、detail/frame style 均进入 Scene/Prefab/Inspector，不继续扩散代码常量。
- B2 Presentation 不写 Domain/Application；主线程独占 Scene/Composition/Controller/Binding/harness。

## 多智能体输出

三份只读报告、五份实现 Prompt 与 `prompt-review-turn-lifecycle.md` 已生成。路径互斥审查通过，当前无用户决策阻塞；立即进入 Gate A。
