# enemy_intent_presentation_controller.gd 维护说明

日期：2026-06-13

## 当前职责

`scene/in_scene/enermy/enemy_intent_presentation_controller.gd` 是敌人意图表现协调器。它继续负责解析当前 hover 入口、判断交互阶段、协调地图高亮、时间轴高亮、主 tooltip、状态关键词副 tooltip，以及在 hover 退出、阶段切换和意图重判时清理表现。

## 已拆模块

已拆模块位于 `scene/in_scene/enermy/intent_presentation_modules/`：

- `presenters/EnemyIntentTooltipTextBuilder.gd`：组装主 tooltip 文本行，包含意图描述、来源状态行和无效原因 BBCode；不写入 `MainBoard.cursor_tooltip`，不创建副 tooltip，也不定位。

## 不要继续硬拆

- 不要把地图高亮、时间轴高亮和 tooltip 节点生命周期揉进同一个新模块。
- 不要同批改 `EnemyIntentResolver`、`EnemyIntentData` 字段契约或 `TimelineManager` 数据结构。
- 不要重复拆主 tooltip 文本 builder。
- 不要在文本 builder 里读取节点树、创建 `PanelContainer` 或调用 `MainBoard.set_cursor_tooltip_position()`。

## 后续可做

下一批如果继续这个文件，优先在一个风险面内处理：

1. 状态关键词副 tooltip presenter：只接管 HBox、关键词 Panel 创建和销毁，保持 host 选择与定位语义不变。
2. 主 tooltip 定位 helper：只拆 `_position_intent_tooltip()` 的坐标与屏幕 clamp，不碰文本和副 tooltip 创建。
3. 引用查找 bridge：只拆 `_resolve_references()`，但必须保持 MainBoard、HexMap、TimelineManager 和 hover 信号连接顺序。

## 验证入口

改动后加载 `res://scene/in_scene/in_scene.tscn`，检查地图敌人 hover、时间轴敌人行动 hover、无效意图说明、建筑状态主 tooltip 文本和状态关键词副 tooltip 仍能显示与隐藏。
