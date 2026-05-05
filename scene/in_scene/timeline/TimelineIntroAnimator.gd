# 功能: 时间轴入场动画控制器，统一处理背景格子涟漪出现与意图方块生成出现。
# 核心逻辑: play_grid_intro() 按左下角距离分层播放格子缩放淡入；play_action_intro() 让生成的意图由暗到亮、从下方上升到固定格位。
extends Node
class_name TimelineIntroAnimator

signal grid_intro_finished
signal action_intro_finished(action: TimelineAction, action_container: Control)

const META_ORIGINAL_MOUSE_FILTER := &"timeline_intro_original_mouse_filter"

@export_group("总开关")
## 是否启用时间轴背景格子的入场动画。
@export var grid_intro_enabled: bool = true
## 是否启用敌人/玩家行动方块的入场动画。
@export var action_intro_enabled: bool = true
## 在真正播放入场前，是否先把背景格子压到透明小尺寸。
@export var prepare_grid_hidden_until_play: bool = true
## 格子入场时是否临时隐藏行动层，避免已有意图抢在格子前显示。
@export var hide_action_layer_during_grid_intro: bool = true
## 是否只播放一次背景格子入场。单局内后续回合只播放意图生成动画。
@export var play_grid_intro_once: bool = true
## 是否对敌人意图播放生成动画。
@export var animate_enemy_actions: bool = true
## 是否对玩家卡牌行动也播放同一套生成动画。默认关闭，保留原玩家放置手感。
@export var animate_player_actions: bool = false

@export_group("格子涟漪")
## 涟漪源点，使用归一化网格坐标。Vector2(0, 1) 表示左下角。
@export var grid_intro_origin_normalized: Vector2 = Vector2(0.0, 1.0)
## 单个格子的缩放淡入时间。
@export var grid_intro_cell_duration: float = 0.26
## 每相差一格距离额外延迟的时间，控制涟漪层间隔。
@export var grid_intro_wave_delay: float = 0.055
## 格子出现前的缩放。
@export var grid_intro_start_scale: Vector2 = Vector2(0.18, 0.18)
## 格子出现前的颜色。通常只调 alpha，保持原本格子样式颜色。
@export var grid_intro_start_modulate: Color = Color(1.0, 1.0, 1.0, 0.0)
## 格子最终颜色。
@export var grid_intro_final_modulate: Color = Color.WHITE
## 格子入场补间曲线类型。
@export var grid_intro_trans_type: Tween.TransitionType = Tween.TRANS_BACK
## 格子入场缓动方向。
@export var grid_intro_ease_type: Tween.EaseType = Tween.EASE_OUT

@export_group("意图生成")
## 意图涟漪源点，默认也从左下角开始，让意图和背景格子的方向统一。
@export var action_intro_origin_normalized: Vector2 = Vector2(0.0, 1.0)
## 单个意图容器的入场时间。
@export var action_intro_duration: float = 0.34
## 多个意图同时生成时，每相差一格距离额外延迟的时间。
@export var action_intro_wave_delay: float = 0.035
## 意图开始时相对最终位置的偏移。y 为正表示从下方上升。
@export var action_intro_start_offset: Vector2 = Vector2(0.0, 18.0)
## 意图开始时的缩放。
@export var action_intro_start_scale: Vector2 = Vector2(0.9, 0.9)
## 意图开始时的颜色。较暗且透明，形成“倒放卸载”的出现感。
@export var action_intro_start_modulate: Color = Color(0.22, 0.16, 0.16, 0.0)
## 意图最终颜色。
@export var action_intro_final_modulate: Color = Color.WHITE
## 意图入场补间曲线类型。
@export var action_intro_trans_type: Tween.TransitionType = Tween.TRANS_CUBIC
## 意图入场缓动方向。
@export var action_intro_ease_type: Tween.EaseType = Tween.EASE_OUT
## 意图入场期间是否临时禁用自身鼠标交互，防止动画途中触发 hover 联动。
@export var disable_action_input_during_intro: bool = true

var timeline_ui: Control = null
var grid_background: Control = null
var shape_layer: Control = null
var _active_action_tweens: Dictionary = {}


func setup(p_timeline_ui: Control, p_grid_background: Control, p_shape_layer: Control) -> void:
	timeline_ui = p_timeline_ui
	grid_background = p_grid_background
	shape_layer = p_shape_layer


## 入场前预处理背景格子。
## 注意这里不调用 hide()，因为 GridContainer 可能会忽略隐藏子节点并重新排版。
func prepare_grid_for_intro(grid_cells: Dictionary) -> void:
	if not grid_intro_enabled or not prepare_grid_hidden_until_play:
		return

	for cell in grid_cells.values():
		if cell is Control:
			_prepare_control_for_reveal(cell as Control, grid_intro_start_scale, grid_intro_start_modulate)
			_set_control_input_enabled(cell as Control, false)


## 播放背景格子涟漪入场，并在结束后恢复格子交互。
func play_grid_intro(grid_cells: Dictionary, grid_width: int, grid_height: int) -> void:
	if not grid_intro_enabled:
		grid_intro_finished.emit()
		return

	var entries := _collect_grid_entries(grid_cells, grid_width, grid_height)
	if entries.is_empty():
		grid_intro_finished.emit()
		return

	var action_layer_was_visible := true
	if hide_action_layer_during_grid_intro and is_instance_valid(shape_layer):
		action_layer_was_visible = shape_layer.visible
		shape_layer.visible = false

	var duration := maxf(grid_intro_cell_duration, 0.001)
	var tween := create_tween().set_parallel(true)
	for entry in entries:
		var cell := entry["cell"] as Control
		var delay := float(entry["delay"])
		_prepare_control_for_reveal(cell, grid_intro_start_scale, grid_intro_start_modulate)
		_set_control_input_enabled(cell, false)

		var scale_tweener := tween.tween_property(cell, "scale", Vector2.ONE, duration)
		scale_tweener.set_delay(delay)
		scale_tweener.set_trans(grid_intro_trans_type)
		scale_tweener.set_ease(grid_intro_ease_type)

		var fade_tweener := tween.tween_property(cell, "modulate", grid_intro_final_modulate, duration)
		fade_tweener.set_delay(delay)
		fade_tweener.set_trans(Tween.TRANS_SINE)
		fade_tweener.set_ease(Tween.EASE_OUT)

	await tween.finished

	for entry in entries:
		var cell := entry["cell"] as Control
		if is_instance_valid(cell):
			cell.scale = Vector2.ONE
			cell.modulate = grid_intro_final_modulate
			_restore_control_input(cell)

	if hide_action_layer_during_grid_intro and is_instance_valid(shape_layer):
		shape_layer.visible = action_layer_was_visible

	grid_intro_finished.emit()


## 判断某个 TimelineAction 是否应该使用意图入场动画。
func should_animate_action(action: TimelineAction) -> bool:
	if not action_intro_enabled or action == null:
		return false
	if action.type == TimelineAction.Type.ENEMY:
		return animate_enemy_actions
	if action.type == TimelineAction.Type.PLAYER:
		return animate_player_actions
	return false


## 播放单个行动容器的生成动画。
## action_container 是 timeline_ui.gd 为一个 TimelineAction 创建的整体容器，动画只动容器，避免逐格 tween 过多。
func play_action_intro(
	action_container: Control,
	action: TimelineAction,
	grid_width: int,
	grid_height: int,
	force_restart: bool = false
) -> void:
	if not is_instance_valid(action_container) or not should_animate_action(action):
		return

	var container_id := action_container.get_instance_id()
	if _active_action_tweens.has(container_id):
		var old_tween := _active_action_tweens[container_id] as Tween
		if old_tween != null and old_tween.is_valid():
			if not force_restart:
				return
			old_tween.kill()
		_active_action_tweens.erase(container_id)

	var target_position := action_container.position
	var target_scale := Vector2.ONE
	var delay := _get_action_intro_delay(action, grid_width, grid_height)
	var duration := maxf(action_intro_duration, 0.001)

	action_container.visible = true
	action_container.pivot_offset = _get_control_pivot(action_container)
	action_container.position = target_position + action_intro_start_offset
	action_container.scale = action_intro_start_scale
	action_container.modulate = action_intro_start_modulate

	if disable_action_input_during_intro:
		_set_tree_input_enabled(action_container, false)

	var tween := create_tween().bind_node(action_container).set_parallel(true)
	_active_action_tweens[container_id] = tween
	var position_tweener := tween.tween_property(action_container, "position", target_position, duration)
	position_tweener.set_delay(delay)
	position_tweener.set_trans(action_intro_trans_type)
	position_tweener.set_ease(action_intro_ease_type)

	var scale_tweener := tween.tween_property(action_container, "scale", target_scale, duration)
	scale_tweener.set_delay(delay)
	scale_tweener.set_trans(action_intro_trans_type)
	scale_tweener.set_ease(action_intro_ease_type)

	var fade_tweener := tween.tween_property(action_container, "modulate", action_intro_final_modulate, duration)
	fade_tweener.set_delay(delay)
	fade_tweener.set_trans(Tween.TRANS_SINE)
	fade_tweener.set_ease(Tween.EASE_OUT)
	tween.finished.connect(_finish_action_intro.bind(container_id, action_container, action, target_position))


func stop_action_intros() -> void:
	for tween in _active_action_tweens.values():
		if tween is Tween and tween.is_valid():
			tween.kill()
	_active_action_tweens.clear()


func _finish_action_intro(container_id: int, action_container: Control, action: TimelineAction, target_position: Vector2) -> void:
	_active_action_tweens.erase(container_id)
	if not is_instance_valid(action_container):
		return

	action_container.position = target_position
	action_container.scale = Vector2.ONE
	action_container.modulate = action_intro_final_modulate
	if disable_action_input_during_intro:
		_restore_tree_input(action_container)
	action_intro_finished.emit(action, action_container)


func _collect_grid_entries(grid_cells: Dictionary, grid_width: int, grid_height: int) -> Array[Dictionary]:
	var entries: Array[Dictionary] = []
	for pos in grid_cells.keys():
		if typeof(pos) != TYPE_VECTOR2I:
			continue

		var cell = grid_cells[pos]
		if not (cell is Control):
			continue

		var grid_pos: Vector2i = pos
		entries.append({
			"cell": cell,
			"delay": _get_grid_intro_delay(grid_pos, grid_width, grid_height)
		})

	return entries


func _get_grid_intro_delay(grid_pos: Vector2i, grid_width: int, grid_height: int) -> float:
	var origin := _normalized_origin_to_grid_point(grid_intro_origin_normalized, grid_width, grid_height)
	var grid_point := Vector2(float(grid_pos.x), float(grid_pos.y))
	var distance := origin.distance_to(grid_point)
	return distance * maxf(grid_intro_wave_delay, 0.0)


func _get_action_intro_delay(action: TimelineAction, grid_width: int, grid_height: int) -> float:
	if action == null:
		return 0.0

	var action_center := Vector2(float(action.origin_grid_pos.x), float(action.origin_grid_pos.y))
	if not action.shape_coords.is_empty():
		var sum := Vector2.ZERO
		for offset in action.shape_coords:
			var cell_pos := action.origin_grid_pos + offset
			sum += Vector2(float(cell_pos.x), float(cell_pos.y))
		action_center = sum / float(action.shape_coords.size())

	var origin := _normalized_origin_to_grid_point(action_intro_origin_normalized, grid_width, grid_height)
	return origin.distance_to(action_center) * maxf(action_intro_wave_delay, 0.0)


func _normalized_origin_to_grid_point(origin_normalized: Vector2, grid_width: int, grid_height: int) -> Vector2:
	var clamped_origin := Vector2(
		clampf(origin_normalized.x, 0.0, 1.0),
		clampf(origin_normalized.y, 0.0, 1.0)
	)
	return Vector2(
		maxi(grid_width - 1, 0) * clamped_origin.x,
		maxi(grid_height - 1, 0) * clamped_origin.y
	)


func _prepare_control_for_reveal(control: Control, start_scale: Vector2, start_modulate: Color) -> void:
	if not is_instance_valid(control):
		return
	control.visible = true
	control.pivot_offset = _get_control_pivot(control)
	control.scale = start_scale
	control.modulate = start_modulate


func _get_control_pivot(control: Control) -> Vector2:
	if not is_instance_valid(control):
		return Vector2.ZERO
	if control.size.x > 0.0 and control.size.y > 0.0:
		return control.size * 0.5
	return control.custom_minimum_size * 0.5


func _set_tree_input_enabled(node: Node, enabled: bool) -> void:
	if node is Control:
		_set_control_input_enabled(node as Control, enabled)
	for child in node.get_children():
		_set_tree_input_enabled(child, enabled)


func _restore_tree_input(node: Node) -> void:
	if node is Control:
		_restore_control_input(node as Control)
	for child in node.get_children():
		_restore_tree_input(child)


func _set_control_input_enabled(control: Control, enabled: bool) -> void:
	if not is_instance_valid(control):
		return
	if not control.has_meta(META_ORIGINAL_MOUSE_FILTER):
		control.set_meta(META_ORIGINAL_MOUSE_FILTER, int(control.mouse_filter))
	control.mouse_filter = int(control.get_meta(META_ORIGINAL_MOUSE_FILTER)) if enabled else Control.MOUSE_FILTER_IGNORE


func _restore_control_input(control: Control) -> void:
	if not is_instance_valid(control):
		return
	if control.has_meta(META_ORIGINAL_MOUSE_FILTER):
		control.mouse_filter = int(control.get_meta(META_ORIGINAL_MOUSE_FILTER))
		control.remove_meta(META_ORIGINAL_MOUSE_FILTER)
