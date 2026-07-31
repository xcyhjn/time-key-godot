# Wave 01 集成契约

> 状态：已冻结
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：玩法等价契约、目标架构、数据迁移边界

## Domain API

首切片只要求下列语义，不冻结不必要的具体实现：

```text
HexCoord(q, r)
TimelineGrid(width=12, height=3)
TryPlace(TimelineAction) -> success/failure
Resolve(CombatSliceState) -> ResolutionSnapshot
CardDefinition(stableId, numericId, effects, range, shape)
```

`TimelineAction` 至少包含 origin、actor kind、card ID、target ID；结算按列再按行且一个 action 只执行一次。`ResolutionSnapshot` 字段与 `gameplay-parity-contract.md` 完全一致。

## Adapter API

Infrastructure 从 TextAsset/字符串解析卡牌 JSON，输出 Domain 的 `CardDefinition`。解析器必须：

- 接受源文件未映射中文字段。
- 保留 `lighting` 稳定 ID、`damage=100`、range offsets 和单格 shape。
- 对缺失稳定 ID、未知 effect 类型或无效 shape 给出显式错误。
- 不修改 fixture 内容。

## Presentation API

Presentation 创建固定 seed 731 的场景，使用 axial XZ 映射显示至少 7 个六边形地块、一个可选目标和一个敌人意图。UI 暴露选卡、放置、结算、目标 HP、阶段与 12×3 时间轴。对 Domain 的调用通过明确方法完成，不能在按钮事件中重复实现伤害规则。

`VerticalSliceController` 的公共集成面冻结为：

```text
BuildSceneGraph()                         # 幂等，可由 Awake 与 Editor harness 调用
SelectCard(string stableId) -> bool
SelectTarget(string targetId) -> bool
TryPlaceSelected(int column, int row) -> bool
ResolveTimeline() -> ResolutionSnapshot
CurrentTargetHp, EnemyIntentResolved, TimelineSlotCount
```

fixture 固定 `stableId=lighting`、`targetId=target-01`。Controller 通过 `CardJsonAdapter.Parse` 读取 TextAsset，不自建第二套 JSON DTO。`BuildSceneGraph()` 重复调用不能复制 Camera、Canvas、地块或事件监听。

## Wave 02B1 卡牌交互契约

> 状态：待 Agent 实现；语义已冻结

### Domain

```text
TimelineGrid.CanPlace(TimelineAction) -> bool
CardPlaySession(card)
SelectTarget(targetId, HexCoord) -> transition result
PreviewTimeline(TimelineGrid, TimelineCell) -> valid/invalid result
Commit(TimelineGrid) -> success/failure
Cancel() -> cancelled snapshot
```

`CanPlace` 不得改变占用或 action 集合，并与 `TryPlace` 复用同一内部合法性判断。Preview、失败 Commit 和 Cancel 都不能改变 `OccupiedCellCount`。最终 Commit 使用 `TimelineAction.FromCard`，保留 `lighting`、damage 100、单格 shape 与稳定目标 ID。

### Card presentation

```text
CardHandView.Build(CardViewModel)
CardHandView.SetInteractionState(state)
event CardSelected(stableId)
event CardCancelRequested(stableId)
event CardDragChanged(stableId, pointerPosition, phase)
```

卡牌视图只表达交互意图，不 raycast 世界、不解析 JSON、不判断时间轴合法性。原卡牌基准 `125×175`，完整卡面保持纵横比。Selected/Targeting/Scheduling 状态的右键取消优先于轨道镜头；composition root 在这些状态设置 `BoardCamera.InputEnabled=false`，退出后恢复。

### Board and timeline preview

```text
BoardRangePreview.Register(HexCoord, BoardTileView)
BoardRangePreview.Show(center, relativeOffsets)
BoardRangePreview.Clear()
TimelinePlacementPreview.Show(origin, shape, isValid)
TimelinePlacementPreview.Clear()
```

`lighting` 范围固定为 `(0,0),(1,0),(2,0)`。Board preview 只投影坐标并设置表现；Timeline preview 的 `isValid` 必须来自 Domain `CanPlace`，不得复制边界/冲突规则。所有清理幂等，镜头变化不改变坐标集合。

### 主智能体共享接线

现有 `VerticalSliceController` 公共方法必须保持兼容。主智能体在 Agent 交回所有权后独占修改 Controller、`BoardTileView`、共享场景和 Editor harness，把 UI 事件接到现有选卡/目标/放置/结算链。Agent 不得直接改这些共享文件。

## 场景与截图

场景路径固定：`Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`。Editor harness 输出证据到 `docs/migration/unity-3d/04-verification/evidence/unity-slice-01/`，文件名至少包含视口尺寸。PlayMode 测试通过场景控制器的公共交互方法驱动，不依赖屏幕坐标。

## 版本与提交

任何契约变更先由主智能体更新本文件和 ADR，再调整实现。代理不创建独立分支或提交；主智能体精确暂存并形成单一目的检查点。
