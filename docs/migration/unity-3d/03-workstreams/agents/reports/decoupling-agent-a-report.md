# Agent A：Application / Diagnostics 交付报告

> 状态：完成，独占路径已交回主智能体
> 日期：2026-08-01
> 分支：`unity_7.31`
> 验证限制：按 Agent Prompt 未启动 Unity、未截图、未执行 Git 写操作

## 改动文件

Application：

- `unity/Assets/_Project/Runtime/Application/CombatTarget.cs`
- `unity/Assets/_Project/Runtime/Application/CombatApplicationModels.cs`
- `unity/Assets/_Project/Runtime/Application/CombatApplicationSession.cs`
- 对应 `.meta`

Diagnostics：

- `unity/Assets/_Project/Runtime/Diagnostics/CollectingCombatTraceSink.cs`
- 对应 `.meta`

EditMode contract tests：

- `unity/Assets/_Project/Tests/EditMode/Application/CombatApplicationSessionTests.cs`
- `unity/Assets/_Project/Tests/EditMode/Diagnostics/CollectingCombatTraceSinkTests.cs`
- 两个测试目录及文件的 `.meta`

未修改冻结的 `TimeKey.Application.asmdef`、`TimeKey.Diagnostics.asmdef`、`ICardCatalog`、`ICombatTraceSink`、`CombatTraceEntry` 和 `NoOpCombatTraceSink`。

## API 契约

### `CombatApplicationSession`

构造参数：

```csharp
CombatApplicationSession(
    ICardCatalog catalog,
    CombatSliceState state,
    TimelineGrid timeline,
    IReadOnlyList<TimelineAction> initialActions = null,
    ICombatTraceSink traceSink = null)
```

公开命令：

- `SelectCard(string)`
- `SelectTarget(CombatTarget)`
- `PreviewTimeline(TimelineCell)`
- `CommitTimeline()`
- `CancelCard()`
- `ResolveTimeline()`
- 幂等 `Dispose()`

`Cards` 暴露目录只读列表；`Current` 返回不可变 `CombatSessionView`。每个命令返回 `CombatCommandResult`，包含成功/失败、结构化 `CombatCommandFailure`、前后 phase、stable ID、typed target、timeline origin、placement validity 和可选 `ResolutionSnapshot`。

Session 直接复用 `CardPlaySession` 和 `TimelineGrid` 的现有合法性来源，不复制占格或效果规则。构造时对初始敌人行动做全量预检后只放置一次；重复 preview 无副作用，commit 单次变更，resolve 后时间轴按 Domain 契约清空。

### Typed target

`CombatTarget` 区分 `Entity` 与 `Tile`：

- `CombatTarget.ForEntity(entityId, coordinate)`
- `CombatTarget.ForTile(coordinate)`

普通卡目标类型由 effect kind 推导，不依赖 stable ID：Damage/Recover/Poison 为 entity，Elevation/Built 为 tile；混合目标策略、空 effect 和 Clear 返回 `UnsupportedCardTargetPolicy`。Clear 继续保留独立即时会话边界。

现有 Domain `TimelineAction` 强制非空 `TargetId`，因此 tile 命令传入 Domain 时复用当前 `CombatSliceState.TargetId` 作为兼容值；Application 状态与 trace 仍以 typed tile coordinate 为权威，不生成 `hex-q-r` 合成 ID。该兼容值只供现有 Domain 构造器使用，Elevation handler 不消费它。

### Handler 与 Diagnostics

`TimelineGrid.CanPlace/TryPlace` 抛出的 `UnsupportedCardEffectException` 在 preview/commit/resolve 边界转换为 `UnsupportedEffect`，失败发生在玩家 action 占格前并写入 trace；Recover/Built/Poison 不会静默 no-op。handler 注册与实现仍由主智能体拥有的 Domain 路径控制。

`CollectingCombatTraceSink` 按插入顺序暴露只读 `Entries` 并支持幂等 `Clear()`。Session 会吞掉 sink 自身异常，确保 diagnostics 只能观察、不能改变命令结果。冻结 `CombatTraceEntry` 不含单独的 effect before/after 字段，本 Agent 未越权扩展端口。

## 测试覆盖

新增 14 个纯 C# contract tests：

- lighting 选卡、entity target、preview、commit、resolve、伤害和单一敌人意图。
- earthquake typed tile target、七格 effect result、每格逻辑层 `1 -> 3`。
- 未知卡、target-before-card、preview-before-target、错误 target kind、未知 entity、缺失 tile、越界 preview。
- 两卡切换不依赖 Presentation event，且不占用玩家时间轴。
- preview 后并发占格导致 commit 结构化失败且无额外变更。
- cancel 幂等、commit 后 cancel 显式失败、重复 preview/commit/resolve 不重复变更。
- Recover 未注册 handler 显式失败、时间轴无玩家占格并有失败 trace。
- no-op、collecting、throwing 三种 sink 的战斗快照等价。
- collecting sink 顺序、重复 clear 和 null 拒绝。
- dispose 幂等且后续命令失败不变更时间轴。

代理验证使用 Unity 安装内 Roslyn，只编译纯 C# 源码且未启动 Editor：

- static compile：通过。
- 反射执行 Agent A `[Test]`：`14/14` 通过，`0` 失败。
- Application/Diagnostics 源码静态搜索：`UnityEngine`、Infrastructure、Presentation 引用均为 `0`。

正式证据仍由主智能体收回所有权后执行：Unity 6000.4.10f1，EditMode filter `TimeKey.Tests.EditMode.Application;TimeKey.Tests.EditMode.Diagnostics`，XML 写入 `docs/migration/unity-3d/04-verification/evidence/decoupling-r2/agent-a-editmode-results.xml`。

## 主智能体接线点

1. Composition/Controller 构造真实 `ICardCatalog`、`CombatSliceState` 和 `TimelineGrid`，把固定敌人 intent 作为 `initialActions` 传入；原 `PlaceEnemyIntent()` 不得再次放置同一行动。
2. Controller 兼容 facade 将 `SelectTarget(string)` 映射为 `ForEntity`，将 `SelectEarthquakeTarget(HexCoord)` 映射为 `ForTile`；公共 bool/异常/snapshot 语义由 facade 根据 `CombatCommandResult` 保留。
3. Presentation 从 `session.Current` 和命令结果更新手牌、范围、时间轴预览与结算表现，不直接调用 `CardPlaySession`。
4. `ResolveTimeline()` 成功结果携带已结算 stable ID 和 snapshot，即使 `Current.SelectedCard` 在 Resolved phase 已清空。
5. R3 可注入 `NoOpCombatTraceSink.Instance` 或 `CollectingCombatTraceSink`；sink 不是 gameplay 决策输入。

## 风险与保护状态

- Agent 未运行 Unity；正式 asmdef 编译、过滤 EditMode XML 和 Controller 集成测试必须由主智能体完成。
- 未实现剩余卡效果、敌人扩展、Scene/Prefab 或 UI；未增加无真实替换点的接口。
- 未触碰 Controller、Domain、Infrastructure、Presentation、Scene/Prefab、任何 asmdef、harness 或共享进度/架构文档。
- 未 stash、stage、commit、push、切分支、revert 或清理未知文件。
- 用户保护清单未触碰。

Agent A 对 Application、Diagnostics、对应 EditMode tests 和本报告的独占所有权现已交回主智能体。
