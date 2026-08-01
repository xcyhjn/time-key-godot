# Remaining Cards Agent C1 — Clear Domain/Application

> 日期：2026-08-01
> 分支：`unity_7.31`
> 所有权：只修改 C1 Prompt 授权的 Domain/Application/EditMode tests 与本报告；未触碰 Presentation、Controller、Composition、Infrastructure、Scene/Prefab、Editor、共享迁移文档、Godot 源或 Git。

## 1. 结论

Gate C1 的 Clear Domain/Application 契约已实现并通过定向 Unity EditMode 验证。Wind/Tornado 现在走独立 `TimelineClearSession`，不会创建普通 `TimelineAction`、不会占用新格，也不会从 `CardDefinition.Shape` 回退 mask。

冻结行为：

- `TimelineGrid.PreviewClear/TryClear` 只接受 typed `CardEffect` 且要求 `Kind=Clear`、`ClearMask` 非空；普通 effect 直接拒绝。
- Preview 对 mask 每格返回 `OutOfBounds/Empty/Occupied`；命中 action 使用只读 `TimelineClearActionSnapshot`，包含 origin、actor kind、card ID、完整相对 shape 与完整绝对占格。
- Preview 合法性只看整个 mask 是否位于 12x3 边界；占用不影响合法性，合法空清成功。
- Commit 先建立完整 preview 并验证边界，再按 `HashSet<TimelineAction>` identity 去重；命中任一格即删除该 action 的全部占格并从 placed-action 集合移除。
- 玩家与敌人 action 没有 actor 过滤；同一多格 action 被命中多格只返回/移除一次；多个 action 按 mask 扫描顺序稳定返回。
- 越界 Commit 返回失败且零修改；重复 grid clear 在首次移除后成为合法空清。普通 `CanPlace/TryPlace/Resolve` 没有改写语义。
- `TimelineClearSession` 独立维护 Selected/Preview/Committed/Cancelled；重复 Preview 纯读，重复 Commit 返回 `AlreadyCommitted`，Cancel 幂等且不修改 grid。
- Application 新增显式 `CombatInteractionMode.OrdinaryTimeline/TimelineClear`。选择 Clear 时 `RequiredTargetKind=null`，地图选目标与普通 Preview/Commit 返回 `InteractionModeMismatch`。
- `PreviewClear` 把三态 preview 暴露到 command/view；成功 `CommitClear` 立即进入 `Resolved`、清空活动卡牌会话并保留 `LastClearResult`，不要求也不允许普通 Resolve 才算完成。

## 2. 精确文件

新增：

- `Runtime/Domain/TimelineClearModels.cs` 与 `.meta`
- `Runtime/Domain/TimelineClearSession.cs` 与 `.meta`
- `Tests/EditMode/TimelineClearGridTests.cs` 与 `.meta`
- `Tests/EditMode/TimelineClearSessionTests.cs` 与 `.meta`
- 本报告

最小修改：

- `Runtime/Domain/TimelineGrid.cs`
- `Runtime/Application/CombatApplicationModels.cs`
- `Runtime/Application/CombatApplicationSession.cs`
- `Tests/EditMode/Application/CombatApplicationSessionTests.cs`

`TimelineAction.cs` 保持原有 Clear 拒绝保护，无需修改；`TimelineGrid` 的 `_cells` / `_placedActions` 仍为 private，没有暴露可变集合。

## 3. 测试覆盖

新增/更新用例覆盖：

- Wind 2x2、Tornado 12x1 的精确 mask 与 12x3 边界。
- 越界、合法空格、命中占用三态 preview；同 action 多格命中的只读描述去重。
- 空清成功；多格 action 完整移除；同 action 去重；同时移除多个 action。
- 玩家 action、敌人 action 均可清；无 actor/card ID 过滤。
- 越界先验证后零修改；取消与重复取消纯净；重复 preview/commit 确定性。
- ordinary effect 不能调用 clear API，Clear 不能经 `TimelineAction.FromCard` 伪装普通 action。
- 普通重叠拒绝、合法放置、占格计数与原 `TimelineGridTests` 回归。
- Application Clear 无地图目标、显式 mode、专用 Preview/Commit、成功 Commit 即 Resolved、空清、越界、取消、普通/clear 命令互斥。
- 既有 lighting、earthquake、recover、built、poison、trace、初始化 enemy intent、取消/提交/Resolve 回归继续在同一次过滤运行。

## 4. 验证结果

### 纯 C# 编译

使用 Unity 6000.4.10f1 bundled Roslyn、`/langversion:9.0 /warnaserror+`：

- 全部 `Runtime/Domain/**/*.cs` + `Runtime/Application/**/*.cs` + `Runtime/Diagnostics/**/*.cs`：通过，0 warning / 0 error。
- `TimelineClearGridTests`、`TimelineClearSessionTests`、`CombatApplicationSessionTests`：通过，0 warning / 0 error。
- 新增/修改 Domain/Application 文件扫描 `UnityEngine|UnityEditor`：无命中。

### Unity EditMode

唯一 Unity 实例，工程入口 `D:\timekey-unity-731`；过滤：

```text
TimeKey.Tests.EditMode.TimelineClearGridTests;
TimeKey.Tests.EditMode.TimelineClearSessionTests;
TimeKey.Tests.EditMode.Application.CombatApplicationSessionTests;
TimeKey.Tests.EditMode.TimelineGridTests
```

结果 XML：`%TEMP%\remaining-cards-agent-c-editmode-results.xml`

```text
result=Passed total=56 passed=56 failed=0 skipped=0
```

Unity 退出码为 0，运行时约 609 秒；XML 已确认 `total>0` 且 `failed=0`。`git diff --check` 通过。

## 5. 交给 C2 的冻结输入

Presentation 只需消费以下 Application/Domain 结果，不自行扫描 UI cell 或复制清除规则：

- `CombatSessionView.InteractionMode/ClearPreview/LastClearResult`
- `CombatCommandResult.InteractionMode/ClearPreview/ClearResult`
- `TimelineClearPreview.IsInBounds/Cells/HitActions`
- `TimelineClearCellPreview.Coordinate/State/HitAction`
- `TimelineClearResult.Succeeded/RemovedActions/RemovedCellCount`
- `TimelineClearActionSnapshot.Origin/ActorKind/CardId/Shape/OccupiedCells`

Controller/Binding 只允许按 `CombatInteractionMode` 做普通与 clear 的通用路由；Wind/Tornado 不需要也不允许 stable-ID 分支。Clear 成功后从 `ClearResult.RemovedActions[].OccupiedCells` 清掉完整 action 表现；取消/越界只清 preview，不改变现有 action。

## 6. 非目标与交回

未实现 clear Timeline UI、三态颜色/边框/角标、Controller/Binding 路由、Infrastructure Clear registration、Scene/Prefab/Editor harness、视觉证据或共享文档；这些属于主智能体与 C2 的互斥所有权。本切片没有 Tower decay、Poison tick 或敌人生命周期。

当前无用户决策阻塞。上述独占文件所有权现全部交回主智能体。
