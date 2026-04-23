class_name EnemyIntentPresentationController
extends Node

# ==========================================
# 脚本名称: enemy_intent_presentation_controller.gd
# 功能概述:
# - 这是“敌人意图显示系统”的表现协调器。
# - 它负责把 EnemyIntentResolver 产出的 EnemyIntentData 同时驱动到：
#   1. 地图侧 (敌人本体高亮、目标地块高亮、tooltip)
#   2. 时间轴侧 (整组意图格子放大、脉冲、灰态)
#
# 数据来源:
# - 来自地图 hover：HexMap._on_stack_hover() 调用 handle_map_stack_hover()
# - 来自时间轴 hover：TimelineManager.action_hovered_changed 信号
# - 来自敌人本体与时间轴动作之间的绑定关系：
#   - source_node
#   - timeline_action
#
# 本文件主要处理什么:
# - 根据当前交互阶段（闲置 / 地块选择 / 时间占位拖拽）决定哪些入口允许触发意图显示
# - 将地图侧与时间轴侧的表现绑定到同一份 EnemyIntentData
# - 在 hover 进入与离开时，统一控制 shader / tooltip / 时间轴动画
#
# 本文件不做什么:
# - 不直接负责敌人 AI
# - 不直接生成时间轴意图
# - 不解析 effect_range 细节（交给 EnemyIntentResolver）
#
# 主要函数职责:
# - handle_map_stack_hover():
#   处理地图敌人地块 hover
# - handle_timeline_action_hover():
#   处理时间轴敌人意图 hover
# - show_intent_preview():
#   将同一份意图同时展示到地图和时间轴
# - clear_intent_preview():
#   清除当前正在显示的敌人意图
#
# 数据传递到哪里:
# - 传给 HexMap.show_enemy_intent_preview()
# - 传给 TimelineUI.show_enemy_intent_preview()
# - 传给主界面的 cursor_tooltip
# ==========================================

@export_group("Tooltip Position")
## 敌人意图 tooltip 相对于“施法者地块”的偏移量。
## 约定：tooltip 永远出现在敌人右侧，因此 X 通常为正值。
@export var tooltip_offset: Vector2 = Vector2(120.0, -30.0)
## tooltip 距离屏幕边缘的安全留白。
## 仅用于防止 tooltip 出现在屏幕外，不用于避让建筑。
@export var tooltip_screen_margin: Vector2 = Vector2(16.0, 16.0)

@export_group("Map Intent Colors")
## 默认情况下，施法者本体使用的高亮颜色（白色系）。
@export var source_valid_color: Color = Color(0.82, 0.82, 0.82, 1.0)
## 当意图覆盖自身时，施法者本体使用的红色高亮。
@export var source_self_included_color: Color = Color(0.95, 0.25, 0.25, 1.0)
## 当前意图无效时，施法者本体使用的灰色高亮。
@export var source_invalid_color: Color = Color(0.4, 0.4, 0.4, 1.0)
## 目标地块在意图有效时的波纹颜色。
@export var target_valid_color: Color = Color(0.95, 0.2, 0.2, 1.0)
## 目标地块在意图无效时的灰色波纹颜色。
@export var target_invalid_color: Color = Color(0.45, 0.45, 0.45, 1.0)

@export_group("Timeline Intent Colors")
## 时间轴上的敌人意图格子在有效时的脉冲颜色。
@export var timeline_valid_color: Color = Color(1.0, 0.25, 0.25, 1.0)
## 时间轴上的敌人意图格子在无效时的脉冲颜色。
@export var timeline_invalid_color: Color = Color(0.55, 0.55, 0.55, 1.0)

## HexMap 引用。用于地图地块高亮、地图意图重判、tooltip 锚点回查。
var hex_map: battle = null
## TimelineManager 引用。用于查找敌人 action、时间轴重判与 hover 信号连接。
var timeline_manager: TimelineManager = null
## TimelineUI 引用。用于驱动时间轴整组格子的放大与 shader 脉冲。
var timeline_ui: Control = null
## DragShapeController 引用。用于判断当前是否正处于时间占位拖拽阶段。
var drag_shape_controller: Node = null
## MainBoard 引用。用于访问全局 cursor_tooltip 和 CardManager。
var main_board: Control = null

## 当前正在展示的敌人意图数据。
var current_intent_data: EnemyIntentData = null
## 当前 hover 入口来源：
## - "map": 由地图敌人地块触发
## - "timeline": 由时间轴敌人意图触发
var current_hover_origin: String = ""
## 上一帧记录的系统阶段，用于侦测阶段切换并及时清掉不该残留的意图预览。
var last_phase: String = ""


func _ready() -> void:
	set_process(true)
	call_deferred("_resolve_references")


func _resolve_references() -> void:
	# 统一在这里收集依赖节点，避免各个函数中重复 find_child / get_node_or_null。
	main_board = get_tree().get_first_node_in_group("MainBoard")
	if not is_instance_valid(main_board):
		return

	hex_map = main_board.get_node_or_null("../../map/HexMap")
	timeline_manager = main_board.get_node_or_null("../TimelineSystem/TimelineManager")
	timeline_ui = main_board.get_node_or_null("../TimelineUI")
	drag_shape_controller = get_tree().get_first_node_in_group("DragShapeController")

	if is_instance_valid(timeline_manager):
		if not timeline_manager.action_hovered_changed.is_connected(handle_timeline_action_hover):
			timeline_manager.action_hovered_changed.connect(handle_timeline_action_hover)

	if is_instance_valid(hex_map) and hex_map.has_signal("enemy_roster_changed"):
		if not hex_map.enemy_roster_changed.is_connected(revalidate_enemy_intents):
			hex_map.enemy_roster_changed.connect(revalidate_enemy_intents)
	if is_instance_valid(hex_map) and hex_map.has_signal("tile_topology_changed"):
		if not hex_map.tile_topology_changed.is_connected(revalidate_enemy_intents):
			hex_map.tile_topology_changed.connect(revalidate_enemy_intents)


func handle_map_stack_hover(stack: Area2D, is_entered: bool) -> void:
	# 地图侧 hover 入口：
	# - 闲置阶段允许
	# - 地块选择阶段允许
	# - 时间占位拖拽阶段不允许
	if not _is_map_hover_allowed():
		if current_hover_origin == "map":
			clear_intent_preview()
		return
	if not is_instance_valid(stack):
		return

	var source_node = stack.get_meta("occupant") if stack.has_meta("occupant") else null
	if not _can_source_provide_intent(source_node):
		if is_entered == false and current_hover_origin == "map":
			clear_intent_preview()
		return

	if is_entered:
		var intent_data = EnemyIntentResolver.resolve_enemy_intent(source_node, hex_map, _find_action_by_source(source_node))
		show_intent_preview(intent_data, "map")
	elif current_hover_origin == "map":
		clear_intent_preview()


func handle_timeline_action_hover(action: TimelineAction, is_hovering: bool) -> void:
	# 时间轴侧 hover 入口：
	# - 闲置阶段允许
	# - 时间占位拖拽阶段允许
	# - 地块选择阶段不允许
	if not is_instance_valid(action) or action.type != TimelineAction.Type.ENEMY:
		return
	if not _is_timeline_hover_allowed():
		if current_hover_origin == "timeline":
			clear_intent_preview()
		return

	if is_hovering:
		var intent_data = EnemyIntentResolver.resolve_enemy_intent(action.source_node, hex_map, action)
		show_intent_preview(intent_data, "timeline")
	elif current_hover_origin == "timeline":
		clear_intent_preview()


func show_intent_preview(intent_data: EnemyIntentData, origin: String) -> void:
	# 统一展示入口：
	# - 先清除旧意图
	# - 再把同一份 EnemyIntentData 同步推送到地图、时间轴和 tooltip
	if intent_data == null:
		return

	clear_intent_preview(false)
	current_intent_data = intent_data
	current_hover_origin = origin

	var resolved_source_color = source_valid_color
	if not intent_data.is_valid:
		resolved_source_color = source_invalid_color
	elif intent_data.includes_self:
		resolved_source_color = source_self_included_color

	var resolved_target_color = target_valid_color if intent_data.is_valid else target_invalid_color
	var resolved_timeline_color = timeline_valid_color if intent_data.is_valid else timeline_invalid_color

	if is_instance_valid(hex_map) and hex_map.has_method("show_enemy_intent_preview"):
		hex_map.show_enemy_intent_preview(intent_data, resolved_source_color, resolved_target_color)

	if is_instance_valid(timeline_ui) and intent_data.timeline_action != null and timeline_ui.has_method("show_enemy_intent_preview"):
		timeline_ui.show_enemy_intent_preview(intent_data.timeline_action, intent_data.is_valid, resolved_timeline_color)

	_show_intent_tooltip(intent_data)


func clear_intent_preview(restore_card_hover: bool = true) -> void:
	# 统一清理入口：
	# - 地图 overlay
	# - 时间轴 overlay
	# - tooltip
	# - 当前状态缓存
	if is_instance_valid(hex_map) and hex_map.has_method("clear_enemy_intent_preview"):
		hex_map.clear_enemy_intent_preview(restore_card_hover)

	if is_instance_valid(timeline_ui) and timeline_ui.has_method("clear_enemy_intent_preview"):
		timeline_ui.clear_enemy_intent_preview()

	if is_instance_valid(main_board) and is_instance_valid(main_board.get("cursor_tooltip")):
		main_board.cursor_tooltip.hide()

	current_intent_data = null
	current_hover_origin = ""


func revalidate_enemy_intents() -> void:
	if not is_instance_valid(timeline_manager) or not is_instance_valid(hex_map):
		return

	if timeline_manager.has_method("revalidate_enemy_intents"):
		timeline_manager.revalidate_enemy_intents(hex_map, timeline_ui)

	if current_intent_data != null and is_instance_valid(current_intent_data.source_node):
		var action = _find_action_by_source(current_intent_data.source_node)
		var refreshed = EnemyIntentResolver.resolve_enemy_intent(current_intent_data.source_node, hex_map, action)
		show_intent_preview(refreshed, current_hover_origin if current_hover_origin != "" else "map")


func _show_intent_tooltip(intent_data: EnemyIntentData) -> void:
	if not is_instance_valid(main_board) or not is_instance_valid(main_board.get("cursor_tooltip")):
		return

	var tooltip_lines = [intent_data.description]
	if not intent_data.is_valid:
		tooltip_lines.append("[color=#ff5555]%s[/color]" % (intent_data.invalid_reason if intent_data.invalid_reason != "" else "无可用目标"))

	main_board.cursor_tooltip.bbcode_enabled = true
	main_board.cursor_tooltip.clear()
	main_board.cursor_tooltip.append_text("\n".join(tooltip_lines))
	main_board.cursor_tooltip.show()
	call_deferred("_position_intent_tooltip", intent_data.source_coord)


func _position_intent_tooltip(source_coord: Vector2i) -> void:
	if not is_instance_valid(main_board) or not is_instance_valid(main_board.get("cursor_tooltip")):
		return

	var source_stack = _find_stack_for_coord(source_coord)
	var tooltip_pos = source_stack.global_position + tooltip_offset if is_instance_valid(source_stack) else get_viewport().get_visible_rect().size * 0.5

	var panel_size = Vector2(220.0, 80.0)
	if main_board.get("cursor_tooltip_panel") != null and is_instance_valid(main_board.cursor_tooltip_panel):
		panel_size = main_board.cursor_tooltip_panel.size
	else:
		panel_size = main_board.cursor_tooltip.get_combined_minimum_size()

	var screen_size = get_viewport().get_visible_rect().size
	tooltip_pos.x = clampf(tooltip_pos.x, tooltip_screen_margin.x, screen_size.x - panel_size.x - tooltip_screen_margin.x)
	tooltip_pos.y = clampf(tooltip_pos.y, tooltip_screen_margin.y, screen_size.y - panel_size.y - tooltip_screen_margin.y)

	if main_board.has_method("set_cursor_tooltip_position"):
		main_board.set_cursor_tooltip_position(tooltip_pos)


func _find_action_by_source(source_node: Node) -> TimelineAction:
	if not is_instance_valid(timeline_manager):
		return null

	var seen: Dictionary = {}
	for action in timeline_manager.grid.values():
		if action == null:
			continue
		var action_id = action.get_instance_id()
		if seen.has(action_id):
			continue
		seen[action_id] = true
		if action.type == TimelineAction.Type.ENEMY and action.source_node == source_node:
			return action

	return null


func _find_stack_for_coord(coord: Vector2i) -> Area2D:
	if not is_instance_valid(hex_map) or not hex_map.stack_nodes.has(coord):
		return null
	return hex_map.stack_nodes[coord]


func _is_map_hover_allowed() -> bool:
	var phase = _get_phase()
	return phase == "idle" or phase == "target_select"


func _is_timeline_hover_allowed() -> bool:
	var phase = _get_phase()
	return phase == "idle" or phase == "timeline_drag"


func _get_phase() -> String:
	# 当前阶段判定优先级：
	# 1. 若主战斗流程不在 COMBAT，则统一视为 blocked（完全禁止敌人意图显示）
	# 2. 只要 DragShapeController 正在拖拽，就视为 timeline_drag
	# 3. 否则，如果卡牌已选中，则视为 target_select
	# 4. 最后才是 idle
	if is_instance_valid(main_board) and main_board.get("current_battle_state") != null:
		if main_board.current_battle_state != main_board.BattleFlowState.COMBAT:
			return "blocked"

	if is_instance_valid(drag_shape_controller) and drag_shape_controller.get("is_dragging") == true:
		return "timeline_drag"

	if is_instance_valid(main_board) and main_board.get("manager_instance") != null:
		var cm = main_board.manager_instance
		if cm.get("current_selected_card") != null:
			return "target_select"

	return "idle"


func _process(_delta: float) -> void:
	var phase = _get_phase()
	var phase_changed = phase != last_phase
	last_phase = phase

	if current_intent_data != null:
		if current_hover_origin == "map" and not _is_map_hover_allowed():
			clear_intent_preview()
		elif current_hover_origin == "timeline" and not _is_timeline_hover_allowed():
			clear_intent_preview()
		return

	if phase_changed:
		_try_restore_preview_from_current_hover()


func _try_restore_preview_from_current_hover() -> void:
	# 当阶段切换后，如果鼠标实际上仍停留在敌人地块或敌人时间轴意图上，
	# 则自动恢复对应的敌人意图预览，避免玩家需要“再晃一下鼠标”。
	if _is_map_hover_allowed():
		var hovered_stack = _get_current_hovered_stack()
		if is_instance_valid(hovered_stack):
			handle_map_stack_hover(hovered_stack, true)
			if current_intent_data != null:
				return

	if _is_timeline_hover_allowed() and is_instance_valid(timeline_manager) and timeline_manager.hovered_action != null:
		handle_timeline_action_hover(timeline_manager.hovered_action, true)


func _get_current_hovered_stack() -> Area2D:
	if not is_instance_valid(hex_map):
		return null
	if hex_map.get("hovered_stacks") == null:
		return null

	var stacks = hex_map.hovered_stacks
	if not (stacks is Array):
		return null

	var front_stack: Area2D = null
	var max_y = -INF
	for stack in stacks:
		if is_instance_valid(stack) and stack.global_position.y > max_y:
			max_y = stack.global_position.y
			front_stack = stack
	return front_stack


func _can_source_provide_intent(source_node: Node) -> bool:
	if not is_instance_valid(source_node):
		return false
	if not source_node.has_method("is_intent_preview_enabled"):
		return false
	return source_node.is_intent_preview_enabled()
