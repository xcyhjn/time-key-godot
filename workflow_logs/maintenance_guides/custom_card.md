# custom_card.gd 维护说明

日期：2026-06-12

## 当前职责

`scene/card/custom_card.gd` 是卡牌节点的 composition root。它继续负责适配 card-framework 父类状态、保存/恢复卡牌视觉状态、处理选中/取消选中、tooltip 请求、卡牌数据写回、回手牌布局和出牌交接。

## 已拆模块

已拆模块位于 `scene/card/custom_card_modules/`：

- `bridges/CustomCardNodeBridge.gd`：MainBoard、CardManager、玩家手牌、当前手牌容器、DragShapeController 查找。
- `bridges/CustomCardTooltipBridge.gd`：tooltip MainBoard 查找与显隐转发。
- `bridges/CustomCardMapConditionalEffectBridge.gd`：地图条件效果刷新转发。
- `rules/CustomCardDescriptionParser.gd`：描述 BBCode 与关键词解析。
- `rules/CustomCardTimelineShapeParser.gd`：时间轴 shape 解析。
- `rules/CustomCardEffectRangeParser.gd`：六边形效果范围解析。
- `presenters/CustomCardSelectedVisualPresenter.gd`：选中状态下倾斜和阴影跟随。
- `presenters/CustomCardHoverShaderPresenter.gd`：hover shader 参数写入。

## 不要继续硬拆

- 不要硬拆 `_enter_state()`、`toggle_selection()`、`force_deselect()`、`return_to_hand()`、`apply_stat_modifier()` 或 `play_card()`。
- 不要同批统一 EffectProcessor、敌人意图解析和 CustomCard 运行时范围解析。
- 不要改变 `get_parsed_description()` 写回 `active_keywords` 的旧协议。

## 后续可做

当前剩余低风险边界基本清空。只有在新增明确卡牌表现或规则字段时，再评估放入既有 bridge/rule/presenter。

## 验证入口

改动后加载 `res://scene/card/custom_card.tscn` 和 `res://scene/in_scene/in_scene.tscn`，手动检查 hover tooltip、选中/取消、拖拽交接和出牌。
