# Turn Lifecycle Agent A：Runner 与 Action Snapshot 交付报告

> 状态：实现完成，等待主智能体集成
> 日期：2026-08-02
> 所有权：仅新增 Agent A Prompt 授予的 runtime、test、`.meta` 与本报告

## 交付内容

- 新增 Unity-free `TimelineActionIdentity` 值对象；非空、Ordinal 值相等，并提供 `cycle:{sequence}/action:{ordinal}` 确定性工厂。
- 新增独立于 `CombatSessionPhase` 的 `TurnLifecyclePhase`、request、context、typed failure/result 与 immutable phase history。
- 新增 `TimelineActionPlan`：在任何 phase mutation 前校验 ActionId、边界、格子重复和 action 重叠，并按首个实际占格严格执行 x 后 y 排序。
- 新增单入口 `TurnLifecycleRunner.Run(TurnLifecycleRequest)`：支持 InitialStart 与 EndTurn，共用 status、02B4 no-op hook、intent refresh 尾段。
- 新增六个窄端口：timeline action、building、timeline clearing、turn-start status、next-turn hook、enemy intent refresh；Gate A 默认实现全部为 no-op。
- 新增 immutable `TimelineActionPresentationSnapshot` 与 display payload；包含 ActionId、actor、priority、source/target ID 与 coord、card/effect ID、origin、shape、occupied cells、effect range、typed validity/reason 和 resolve state。

## 生命周期语义

EndTurn 固定顺序：

```text
PlayerReady
  -> EndTurnRequested
  -> ResolvingTimeline
  -> RunningBuildingBehaviors
  -> ClearingTimeline
  -> ProcessingTurnStartStatuses
  -> 02B4 no-op hook
  -> RefreshingEnemyIntents
  -> PlayerReady
```

InitialStart 直接复用 `ProcessingTurnStartStatuses -> hook -> RefreshingEnemyIntents -> PlayerReady`。输入锁只由 `CurrentPhase != PlayerReady` 派生。

运行中重入返回 `Busy`，不改变 phase、sequence 或执行次数。typed processor failure 保留当前故障 phase、保持输入锁并锁存 runner；后续请求返回 `Faulted`，因此已经完成的 action 不会被重放。本 Gate 不实现通用异常事务或回滚。

## 测试覆盖

新增 20 个纯 C# NUnit case，覆盖：

- InitialStart 尾段顺序、完整 EndTurn phase history、输入锁和 02B4 hook 单次调用。
- 运行中重入 Busy、连续两个 cycle、sequence 递增和每阶段每 cycle 一次。
- player/enemy 同 grid 的 x 后 y 顺序、多格 action 单次执行、相同 display ID 的不同 ActionId 保持独立。
- 重复 ActionId 在 processor 调用前返回 typed plan failure。
- processor 结构化失败、已完成 ActionId 记录、故障后拒绝重放。
- ActionId 非空、Ordinal 值相等与 sequence/ordinal 工厂。
- snapshot 集合深拷贝、只读暴露，以及 valid/invalid/unsupported/resolving/resolved typed 状态组合。

## 非 Unity 验证

- Unity 自带 Roslyn 4.3.1 编译完整 `Runtime/Domain/**/*.cs`：通过。
- 同一编译器引用 Domain 后编译完整 `Runtime/Application/**/*.cs`：通过。
- 编译本 Agent 新增 NUnit tests：通过。
- PowerShell 反射执行上述纯测试：`passed=20, failed=0`。
- runtime 新文件静态扫描无 `UnityEngine`、`UnityEditor`、`GameObject`、`Sprite`、`Color`、`MonoBehaviour` 或 `Resources`。

根据主智能体下发的明确禁令，本 Agent 未启动 Unity，因此没有运行 Unity EditMode filter，也没有生成 XML。主智能体接线后仍需运行 Prompt 指定的定向及全量 Unity tests。

## 集成交接

- 未修改任何既有 `.cs`、asmdef、Scene、Prefab、ProjectSettings 或共享文档。
- 未接 `TimelineAction`、`TimelineGrid`、`CombatApplicationSession`、Composition 或 Presentation；这些共享接线继续由主智能体独占。
- 主智能体需要让 preview、commit、resolve、clear 使用同一 `TimelineActionIdentity`，将现有 Timeline 内容投影为 `TimelineActionPlan`，并把 Application 发布数据投影为新的 presentation snapshot。
- 当前无用户决策阻塞。

## Git

未切分支、stash、暂存、commit 或 push；所有新增文件保持未跟踪状态，等待主智能体精确审查和暂存。
