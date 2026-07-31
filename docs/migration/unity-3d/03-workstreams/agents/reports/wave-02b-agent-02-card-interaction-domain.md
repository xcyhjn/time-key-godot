# Wave 02B Agent 02：卡牌交互 Domain 报告

> 状态：已完成，待主智能体审查并冻结 API
> 负责人：Wave 02B Agent 02
> 最后验证日期：2026-07-31
> 证据来源：玩法等价契约、集成契约、Godot `CustomCard`/`DragPlacementQueryService`、Unity EditMode 测试

## 交付范围

- `TimelineGrid.CanPlace(TimelineAction)` 是无副作用查询；与 `TryPlace` 共用 `IsPlacementValid`，没有第二套边界或冲突算法。
- `CardPlaySession` 建立 `Idle -> TargetSelected -> TimelinePreview -> Committed` 状态路径，并提供不可撤销的 `Cancelled` 终态。
- Session 只保存 `CardDefinition`、稳定目标字符串 ID、`HexCoord` 和 `TimelineCell`，没有 `UnityEngine` 引用。
- Preview 只创建临时 `TimelineAction.FromCard` 并调用 `CanPlace`；Commit 才调用 `TryPlace`。预览后若网格被其他行动占用，Commit 会显式失败且不会新增占格。
- Cancel 幂等；Commit 成功后不允许 Cancel 撤销占格。

## 实际 API

```text
TimelineGrid.CanPlace(TimelineAction) -> bool

CardPlaySession(CardDefinition)
  Card, State, TargetId, TargetCoord?, TimelineOrigin?
  SelectTarget(string targetId, HexCoord targetCoord) -> CardPlayTransition
  PreviewTimeline(TimelineGrid grid, TimelineCell origin) -> CardPlayTransition
  Commit(TimelineGrid grid) -> CardPlayTransition
  Cancel() -> CardPlayTransition

CardPlayTransition
  Succeeded, State, Failure, IsPlacementValid
```

状态：`Idle`、`TargetSelected`、`TimelinePreview`、`Committed`、`Cancelled`。

显式失败：`InvalidTarget`、`MissingTarget`、`PreviewRequired`、`InvalidTimelinePlacement`、`AlreadyCommitted`、`Cancelled`。空 `TimelineGrid` 属于编程参数错误，沿用现有 Domain 风格抛 `ArgumentNullException`；非法交互顺序不抛异常。

## 测试证据

新增 `CardPlaySessionTests` 共 12 个实际 NUnit case：

1. 合法选目标、合法预览、确认放置与 `lighting` 伤害 100 结算。
2. 未选目标时预览返回 `MissingTarget`。
3. 四个越界原点均预览失败且不占格。
4. 冲突预览失败且不改变既有占格。
5. 重复预览无副作用，并以最后一次原点为准。
6. 合法预览后网格发生冲突时，Commit 失败且不增加占格。
7. 重复 Commit 返回 `AlreadyCommitted` 且只占一格。
8. Cancel 幂等，Cancel 后 Commit 返回 `Cancelled` 且不占格。
9. `CanPlace` 重复查询无副作用，并与 `TryPlace` 对合法、重复实例、冲突和越界给出一致结果。

先运行红灯门禁，新增测试因 `CardPlaySession` 尚不存在而按预期编译失败；实现后运行完整 Unity EditMode：`31/31 passed`、`0 failed`、`0 skipped`。原基线 `19/19` 保留，新增 `12` 个 case 全通过。`git diff --check` 通过。

## 失败项与兼容风险

- 本子任务是纯 Domain，无可视表现，因此没有截图验收；视觉与输入互斥由主智能体及后续 Presentation Agent 验证。
- 没有运行 PlayMode、场景 harness 或 Player build；它们超出本 Agent 所有权，主智能体集成后执行。
- `TimelinePreview` 同时表达合法和非法预览，Presentation 必须读取 `CardPlayTransition.IsPlacementValid`，不能仅凭 State 判定颜色或是否可确认。
- Session 是一次出牌会话；Cancel/Commit 后若要重新选同一卡，应新建 Session，不应复用终态实例。

## 主智能体接线建议

- 选中 `lighting` 时创建 `CardPlaySession(cardDefinition)`；世界格点击调用 `SelectTarget(targetId, hexCoord)`。
- 时间轴 hover 调用 `PreviewTimeline(grid, cell)`，把返回的 `IsPlacementValid` 直接传给 `TimelinePlacementPreview.Show`；禁止 Presentation 自算越界或冲突。
- 确认按钮只调用 `Commit(grid)`。仅 `Succeeded=true` 时更新弃牌/UI，并继续现有结算链。
- 右键或 Escape 调用 `Cancel()`；收到 `Cancelled` 状态后清理棋盘/时间轴预览并恢复相机输入。
- 主智能体冻结 API 后再启动依赖本 Domain 的 Agent 03/04；若需要重命名，优先只改调用名，不改变本报告记录的状态与副作用语义。
