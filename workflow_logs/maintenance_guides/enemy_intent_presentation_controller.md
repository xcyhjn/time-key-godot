# enemy_intent_presentation_controller.gd 维护说明

日期：2026-06-13

## 当前职责

`scene/in_scene/enermy/enemy_intent_presentation_controller.gd` 是敌人意图表现协调器。它继续负责解析当前 hover 入口、判断交互阶段、协调地图高亮、时间轴高亮、主 tooltip、状态关键词副 tooltip，以及在 hover 退出、阶段切换和意图重判时清理表现。

## 已拆模块

已拆模块位于 `scene/in_scene/enermy/intent_presentation_modules/`：

- `bridges/EnemyIntentPresentationReferenceBridge.gd`：查找 `MainBoard`、`HexMap`、`TimelineManager`、`TimelineUI` 与 `DragShapeController`，并按旧入口顺序连接 hover 与重判信号；不判断交互阶段，不解析意图，不驱动地图/时间轴表现，也不创建或定位 tooltip。
- `rules/EnemyIntentSourceStatusReader.gd`：从敌人意图来源节点读取状态说明行和状态关键词；不创建 tooltip，不写入 `MainBoard`，不定位 UI，也不驱动地图或时间轴表现。
- `presenters/EnemyIntentTooltipTextBuilder.gd`：组装主 tooltip 文本行，包含意图描述、来源状态行和无效原因 BBCode；不写入 `MainBoard.cursor_tooltip`，不创建副 tooltip，也不定位。
- `presenters/EnemyIntentStatusKeywordTooltipPresenter.gd`：创建、定位和销毁状态关键词副 tooltip；不读取敌人状态，不写入主 tooltip 文本，不判断 hover 阶段，也不驱动地图或时间轴表现。
- `presenters/EnemyIntentTooltipPositionHelper.gd`：计算主 tooltip 的屏幕位置和边界 clamp；不读取节点树，不写入 `MainBoard`，不创建或销毁 tooltip。

## 不要继续硬拆

- 不要把地图高亮、时间轴高亮和 tooltip 节点生命周期揉进同一个新模块。
- 不要同批改 `EnemyIntentResolver`、`EnemyIntentData` 字段契约或 `TimelineManager` 数据结构。
- 不要重复拆主 tooltip 文本 builder。
- 不要重复拆状态关键词副 tooltip 的 HBox、Panel 创建、定位或销毁。
- 不要重复拆主 tooltip 坐标与屏幕 clamp helper。
- 不要重复拆来源状态行和状态关键词读取 reader。
- 不要重复拆 `_resolve_references()` 的引用查找 bridge。
- 不要在文本 builder 里读取节点树、创建 `PanelContainer` 或调用 `MainBoard.set_cursor_tooltip_position()`。

## 后续可做

引用查找、来源状态读取和 tooltip 低风险表现面已收口。下一批如果继续这个文件，先重新审查剩余函数，不要为了降行数硬拆 controller；更稳妥的方向是重新评估其他文件的单一小风险面。

## 验证入口

改动后加载 `res://scene/in_scene/in_scene.tscn`，检查地图敌人 hover、时间轴敌人行动 hover、无效意图说明、建筑状态主 tooltip 文本和状态关键词副 tooltip 仍能显示与隐藏。
