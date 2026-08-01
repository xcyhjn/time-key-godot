# Turn Lifecycle Unity 架构审查

> 状态：Gate 0 只读审查完成
> 日期：2026-08-02
> 写入范围：仅本报告；审查智能体未运行 Unity、未修改工作区

## 可继承扩展面

- Domain 与 Application 保持 Unity-free；asmdef 依赖方向可继续继承。
- `TimelineGrid` 已实现 x 后 y 顺序和同一对象跨格去重。
- Clear 保持独立 session、typed mask、空清与整 action 移除，不应并回 ordinary action。
- occupant 已有 runtime ID + `HexCoord` 双索引，能支持稳定重判。
- Application 命令已有结构化失败，trace sink 异常不改变玩法结果。
- 当前 `CombatSessionPhase` 表达卡牌交互，不是回合 phase；必须新增独立 lifecycle phase 类型。

## 当前缺口

- `TimelineAction` 缺稳定 ActionId、source identity/coord、typed validity 和 resolve state；preview 与 commit 会创建不同 action。
- `TimelineGrid.Resolve()` 结算后立即清空，无法观察 `ResolvingTimeline -> RunningBuildingBehaviors -> ClearingTimeline`，也无法为每个 action 发布中间状态。
- `ResolutionSnapshot` 只有 origin、字符串 kind 和 card ID，不能支撑统一表现快照。
- enemy action 只写 `EnemyIntentResolved=true`，没有 occupant 重判和显式 unsupported result。
- occupant 缺确定性全局快照、typed death policy、remove/status 生命周期结果；卡牌 `OccupantEffectResult` 不应被挪作所有生命周期结果。
- `CombatApplicationSession` 是一次性 resolve，没有唯一回合入口、全局输入锁、cycle sequence 或两个连续 lifecycle。
- Composition 同时向 Session/Controller 注入硬编码 damage-0 enemy marker，形成双重事实来源。
- Controller/Timeline Presenter 另行推导 label/color 并逐格渲染，绕开 Application snapshot。

## Gate A 最小类型

- `Domain/Lifecycle/TurnLifecycleModels.cs`：phase、request kind、failure、transition、result、sequence。
- `Domain/Lifecycle/TurnLifecyclePorts.cs`：Timeline、building、status、02B4 hook、intent refresh 的窄端口及 no-op。
- `Domain/Lifecycle/TurnLifecycleRunner.cs`：唯一入口、单向 phase、非重入锁存与输入锁派生。
- `Domain/TimelineActionIdentity.cs`：稳定只读 ActionId 值对象。
- `Application/TimelineActionPresentationSnapshot.cs`：不可变显示、身份、合法性和结算态快照。

ActionId 使用 lifecycle sequence + 确定性 ordinal，不使用 card/intent ID 或对象引用。输入锁由 `Phase != PlayerReady` 派生，不维护第二个易失配布尔值。

## 主线程共享修改面

`TimelineAction.cs`、`CardPlaySession.cs`、`TimelineGrid.cs`、`TimelineClearModels.cs`、`ResolutionSnapshot.cs`、`CombatApplicationModels.cs`、`CombatApplicationSession.cs`、trace port、Composition、Controller、Binding、Scene、asmdef、Editor harness 均由主线程独占。Gate A 实现智能体只新增 lifecycle/snapshot 文件与独占测试。

## 失败语义

Gate A 保证所有可预检失败在 mutation 前退出；运行中重复请求返回 Busy；已完成 action 不因失败重放；phase 最终可观察且输入不会误解锁。现有状态没有通用事务 clone，因此“任意 handler 抛异常后完整回滚全部既有 mutation”不是最小 Gate A 承诺，具体 processor 必须先计算 typed result 再提交。

## 必测

覆盖 initial start 复用尾段、严格 phase 顺序、重入 Busy、连续两个 cycle、锁定期命令拒绝、x/y 混排、ActionId 跨格去重/重复拒绝、clear 后不复活、building 后 clear/status、02B4 no-op、结构化失败、快照深拷贝、typed resolve state、trace 中立和 Unity-free 静态检查。

硬阻塞：无。
