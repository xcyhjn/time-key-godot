# TimelineManager.gd 维护说明

日期：2026-06-13

## 当前职责

`scene/in_scene/timeline/TimelineManager.gd` 是时间轴规则核心。它继续负责 `grid` 占用、行动放置、回合结算、敌方意图生成编排、敌方意图重判、中途移除行动，以及发射 `action_placed`、`action_executed`、`timeline_cleared` 和 `action_hovered_changed` 信号。

## 已拆模块

已拆模块位于 `scene/in_scene/timeline/manager_modules/`：

- `rules/TimelineEnemyIntentPrioritySelector.gd`：敌方意图优先级读取、优先级降序列表、同级候选过滤和同级可放置候选选择。

## 不要继续硬拆

- 不要重复拆优先级读取、优先级排序、同级过滤或同级可放置选择。
- 不要同批修改 `grid` 数据结构、`is_placement_valid()`、`place_action()` 和 `find_random_available_spot()`。
- 不要同批改 `TimelineAction.action_data` 契约、敌方意图目标地块映射和 UI 表现。
- 不要把 `generate_enemy_intents()` 整体搬进模块；它仍负责组合候选、目标、action 创建和最终放置。

## 后续可做

后续如果继续 `TimelineManager.gd`，优先单独评估敌方意图候选收集或目标地块映射。候选收集会触碰敌人协议、`can_generate_intent()` 和 shape 缓存；目标地块映射会触碰 `HexMap.stack_nodes` 契约，两者不要同批处理。

## 验证入口

改动后加载 `res://scene/in_scene/in_scene.tscn`，检查敌方意图生成、时间轴放置、hover 联动、中途失效移除和回合结算。
