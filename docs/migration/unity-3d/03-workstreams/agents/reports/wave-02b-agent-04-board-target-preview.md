# Wave 02B Agent 04：棋盘目标与时间轴预览报告

> 状态：已完成，待主智能体接线
> 负责人：Wave 02B Agent 04
> 最后验证日期：2026-07-31
> 证据来源：冻结集成契约、Agent 02 Domain API、PlayMode NUnit XML、四向与 invalid 实际截图

## 组件/API

- `TargetPreviewPalette` 固定区分 normal、hover、selected、range-valid、timeline-valid、timeline-invalid 六种颜色；selected 与现有 `BoardTileView` 颜色一致。
- `BoardRangePreview.Register(HexCoord, Component)` 接受现有内部 `BoardTileView` 作为 `Component`，无需修改共享文件；`Show(center, relativeOffsets)` 只做 axial 坐标投影/去重，`Clear()` 幂等。
- `BoardRangePreview.ActiveCoordinates` 与 `MissingCoordinates` 可观测；缺失或已销毁地块不创建对象，并通过 `MissingCoordinate` 事件报告。
- 棋盘范围使用 `MaterialPropertyBlock` 保存并恢复每个 Renderer 的原属性块，不修改共享 Material，也不会把测试时已有 selected 属性块清掉。
- `TimelinePlacementPreview.Register(TimelineCell, Graphic)` 注册现有 uGUI cell；`Show(origin, shape, isValid)` 仅使用调用方传入的合法性着色，不复制边界/冲突规则；`Clear()` 恢复每格原色且幂等。

## 测试计数

 scoped PlayMode `4/4 passed`、`0 failed`、`0 skipped`：

1. 六种视觉状态颜色互异。
2. 棋盘范围投影、重复 offset、缺失 `(2,0)`、重复 Show/Clear、属性块恢复和共享材质隔离。
3. `CardPlaySession.PreviewTimeline`/`TimelineGrid.CanPlace` 的 valid 与 conflict invalid 结果直接转发到时间轴颜色，并验证恢复、越界缺失与材质隔离。
4. 0/90/180/270 度相同 `HexCoord` 集合、真实双层高地范围可读、invalid 时间轴渲染与 PNG 输出。

结构化结果：`docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/playmode-results.xml`。

## 四向/invalid 截图

- `board-range-yaw-0.png`
- `board-range-yaw-90.png`
- `board-range-yaw-180.png`
- `board-range-yaw-270.png`
- `timeline-invalid.png`

以上均位于 `docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/`，尺寸 `1280x720`。已逐张实际检查：没有空白、裁切或重叠；三格范围保持一致；双层 `(1,0)` 顶面/侧面高亮清楚；invalid 红格可辨识。哈希见同目录 `manifest.md`。

## 共享 API 请求

无。`BoardTileView` 保持只读且无需改为 public；主智能体可直接把实例作为 `Component` 传给 `Register`。

## 风险

- 本 Agent 未修改共享场景/Controller/harness，因此截图来自独立 PlayMode 渲染 rig；最终原场景接线与完整 UI 遮挡检查仍由主智能体执行。
- 范围预览按 Show 时快照恢复 Renderer 属性块；集成层应先更新 tile 的 selected/hover 基态，再 Show 范围，并在改变基态前 Clear，避免并发写同一属性块产生最后写入者覆盖。
- invalid 时间轴证据使用已注册 cell 的 conflict 状态；完全越界的 cell 没有对应可视格，只通过 `MissingCoordinates` 观测，符合不生成幽灵 cell 的边界。

## 主智能体接线说明

1. composition root 创建一个 `BoardRangePreview`，棋盘生成时逐个调用 `Register(tile.Coordinate, tile)`；目标选中后传入 card 的 Domain `Range`，lighting 即 `(0,0),(1,0),(2,0)`。
2. 创建一个 `TimelinePlacementPreview`，12x3 uGUI 生成时逐格 `Register(new TimelineCell(x,y), cellGraphic)`。
3. 时间轴 hover 先调用 `CardPlaySession.PreviewTimeline(grid, origin)`，再把 `transition.IsPlacementValid` 原样传给 `TimelinePlacementPreview.Show(origin, card.Shape, isValid)`。
4. Cancel、提交成功、换目标和 session 结束时分别调用两个 `Clear()`；镜头只改变 transform，不需要重新 Show，ActiveCoordinates 会保持不变。
