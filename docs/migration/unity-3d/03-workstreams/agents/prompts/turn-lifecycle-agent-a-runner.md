# Turn Lifecycle Agent A：Lifecycle Runner 与 Action Snapshot

> 单一目标：只新增 Unity-free lifecycle runner、稳定 ActionId、不可变 action presentation snapshot 与独占 EditMode tests；不得修改任何既有共享文件。

## 必读

- `00-bootstrap/NEXT_STAGE_TURN_LIFECYCLE_PROMPT.md` 的“主 Prompt”
- `agents/reports/turn-lifecycle-source-semantics.md`
- `agents/reports/turn-lifecycle-unity-architecture-audit.md`
- `03-workstreams/integration-contracts.md`
- 当前 `Runtime/Domain/TimelineAction.cs`、`TimelineGrid.cs`、`Runtime/Application/CombatApplicationModels.cs`

## 独占拥有路径

- 新目录 `unity/Assets/_Project/Runtime/Domain/Lifecycle/**` 及 `.meta`
- 新文件 `unity/Assets/_Project/Runtime/Domain/TimelineActionIdentity.cs` 及 `.meta`
- 新文件 `unity/Assets/_Project/Runtime/Application/TimelineActionPresentationSnapshot.cs` 及 `.meta`
- 可选新端口 `unity/Assets/_Project/Runtime/Application/Ports/IActionDisplayCatalog.cs` 及 `.meta`
- 新目录 `unity/Assets/_Project/Tests/EditMode/Lifecycle/**` 及 `.meta`
- 新 snapshot tests，只能放在新目录 `Tests/EditMode/Application/Lifecycle/**` 及 `.meta`
- 独占报告 `agents/reports/turn-lifecycle-agent-a-runner.md`

## 禁止路径

所有既有 `.cs`、asmdef、Composition、Presentation、Scene、Prefab、Editor、Infrastructure、共享文档、既有证据、ProjectSettings 和 Godot 源。尤其不得修改 `TimelineAction.cs`、`TimelineGrid.cs`、`CombatApplicationSession.cs`、`VerticalSliceController.cs` 或 `CombatCompositionRoot.cs`。

## 冻结契约

- lifecycle phase 独立于卡牌交互 phase：`PlayerReady -> EndTurnRequested -> ResolvingTimeline -> RunningBuildingBehaviors -> ClearingTimeline -> ProcessingTurnStartStatuses -> RefreshingEnemyIntents -> PlayerReady`。
- 输入锁由 phase 是否为 `PlayerReady` 派生；同一 cycle 每个 phase 只进入一次，重入返回 typed Busy/failure。
- InitialStart 复用 `ProcessingTurnStartStatuses -> 02B4 no-op hook -> RefreshingEnemyIntents -> PlayerReady`。
- runner 只组织窄端口顺序；Gate A 的 building/status/intent 可由测试 fake/no-op 提供，不实现 Tower、Poison 或 enemy generation。
- `TimelineActionIdentity` 是非空、值相等的稳定 ActionId；不得使用 Unity 对象引用。
- snapshot 至少含 ActionId、actor、source/target ID 与可空 coord、card/effect ID、不可变 display payload、origin、shape、occupied cells、validity/reason、resolve state。
- 所有集合深拷贝；clear/grid 变化后旧 snapshot 不漂移。显示载荷不得包含 Unity `Sprite`/`Color`/`GameObject`。
- 失败前可预检；运行中失败不得重放已完成 action。不要为通用异常回滚发明全状态 transaction。

## 非目标

不接现有 Session/TimelineGrid，不做 Tower/Poison/enemy handler，不做卡牌布局、Prefab、Scene、UI、Controller、trace 扩展或 02B4 玩法。

## 测试与验证

- InitialStart 顺序、完整 EndTurn 顺序、重入 Busy、两个连续 cycle、输入锁、phase 历史、02B4 hook 调用一次且 no-op。
- player/enemy x 后 y 的输入计划、同 ActionId 跨格只执行一次、不同 ActionId 即使 display ID 相同仍独立、重复 ActionId 预检失败。
- snapshot 深拷贝、valid/invalid/unsupported/resolving/resolved typed state/reason。
- fake processor 失败返回结构化结果且不重复已完成 phase/action。
- 运行定向 Unity EditMode filter，解析 XML `total=passed>0, failed=0`；`git diff --check`；静态确认 Domain/Application 无 `UnityEngine`。

## 停止条件

只有必须修改禁止路径、引入 UnityEngine、反向 asmdef 或无法表达冻结 phase/identity 时停止并报告。普通 API 命名与测试修正不是停止理由。

## Git 与协作

你不是仓库唯一工作者。只改拥有路径，适配并行变化，不回退或格式化他人文件。不得切分支、stash、暂存、commit、push 或清理目录；完成后写独占报告并交回所有权。
