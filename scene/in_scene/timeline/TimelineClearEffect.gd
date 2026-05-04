class_name TimelineClearEffect
extends RefCounted

## ==========================================
## ★ 时间轴即时清除效果工具
## ==========================================
## 这个脚本只服务于类似 wind.json 的“clear”类卡牌：
## - 卡牌不需要选择地图目标，选中后直接进入时间轴放置状态；
## - 时间轴上的 11,11 等范围只作为即时效果预览，不会真正生成 TimelineAction；
## - 预览时空格显示蓝色，已被占用的重叠格显示绿色，越界时整片可见范围显示红色；
## - 点击确认后立刻清掉预览，并对范围内已存在的行动调用 TimelineManager.remove_action_with_fade()；
## - 被清除的行动使用此前做好的“卸载 shader -> 生成无材质残影 -> 下落淡出”动画。
##
## 之所以单独放在这里，是为了让 clear 这种“即时处理，但借用时间轴选格”的特殊逻辑
## 不侵入普通卡牌的稳定放置流程。DragShapeController 只负责在合适时机调用这里的接口。


const EFFECT_TYPE_CLEAR := "clear"

## clear 预览颜色：保持和普通时间轴预览区分清楚。
const PREVIEW_EMPTY_COLOR := Color.SKY_BLUE
const PREVIEW_OCCUPIED_COLOR := Color(0.24, 0.9, 0.36, 0.86)
const PREVIEW_INVALID_COLOR := Color.INDIAN_RED
const PREVIEW_BORDER_COLOR := Color(0.06, 0.08, 0.1, 0.85)
const PREVIEW_OVERLAY_NAME := "TimelineClearPreviewOverlay"
const PREVIEW_OVERLAY_META := &"timeline_clear_preview_overlay"


## 判断卡牌是否含有 clear 效果。
## 这里只读 card_info.effects，不依赖卡牌的普通 shape 字段。
static func is_clear_card(card: Object) -> bool:
	var card_info := _get_card_info(card)
	if card_info.is_empty():
		return false

	for effect in _get_effects(card_info):
		if typeof(effect) != TYPE_DICTIONARY:
			continue
		if str(effect.get("type", "")).to_lower() == EFFECT_TYPE_CLEAR:
			return true

	return false


## 从 clear 效果里解析时间轴作用形状。
## wind.json 当前写法是 effects: [{ "type": "clear", "value": "11,11" }]，
## 因此这里优先读 value；如果缺失，才回退到普通 shape 字段。
static func get_clear_shape_coords(card: Object) -> Array[Vector2i]:
	var card_info := _get_card_info(card)
	var shape_data: Variant = null

	for effect in _get_effects(card_info):
		if typeof(effect) != TYPE_DICTIONARY:
			continue
		if str(effect.get("type", "")).to_lower() == EFFECT_TYPE_CLEAR:
			shape_data = effect.get("value", null)
			break

	if shape_data == null:
		shape_data = card_info.get("shape", "1")

	return parse_shape_coords(shape_data)


## 解析时间轴矩阵字符串。
## 支持：
## - "11,11"：2x2
## - ["11", "11"]：2x2
## - [Vector2i(0, 0), Vector2i(1, 0)]：已解析坐标
static func parse_shape_coords(shape_data: Variant) -> Array[Vector2i]:
	var result: Array[Vector2i] = []

	if typeof(shape_data) == TYPE_ARRAY:
		var all_vector: bool = not shape_data.is_empty()
		for item in shape_data:
			if typeof(item) != TYPE_VECTOR2I:
				all_vector = false
				break
		if all_vector:
			for item in shape_data:
				result.append(item)
			return _normalize_coords(result)

	var raw_rows: Array[String] = []
	if typeof(shape_data) == TYPE_ARRAY:
		for row in shape_data:
			raw_rows.append(str(row))
	elif typeof(shape_data) == TYPE_STRING:
		var text := (shape_data as String).replace("\n", ",").replace(" ", ",")
		for row in text.split(",", false):
			raw_rows.append(row.strip_edges())
	else:
		raw_rows.append(str(shape_data))

	for y in range(raw_rows.size()):
		var row_text := raw_rows[y]
		for x in range(row_text.length()):
			if row_text[x] == "1":
				result.append(Vector2i(x, y))

	return _normalize_coords(result)


## clear 的合法性只看边界，不看占用。
## 因为它的作用就是清除被占用的时间轴格子，所以重叠时应当合法并显示绿色。
static func is_origin_in_bounds(shape_coords: Array[Vector2i], origin: Vector2i, grid_width: int, grid_height: int) -> bool:
	if shape_coords.is_empty():
		return false

	for offset in shape_coords:
		var pos := origin + offset
		if pos.x < 0 or pos.x >= grid_width or pos.y < 0 or pos.y >= grid_height:
			return false

	return true


## 判断 clear 预览范围内是否至少压到了一个已有时间轴行动。
## DragShapeController 用它给拖拽 shader 写入绿色状态；TimelineClearEffect.update_preview()
## 自己也会逐格使用同一套占用判断，把对应背景格染成绿色。
static func has_overlap(timeline_manager: Object, shape_coords: Array[Vector2i], origin: Vector2i) -> bool:
	for offset in shape_coords:
		if _is_cell_occupied(timeline_manager, origin + offset):
			return true
	return false


## 更新 clear 专用时间轴预览。
## - 越界：整片可见范围红色；
## - 合法：空格蓝色、已占用格绿色；
## - 每次刷新前先调用 clear_grid_preview()，并清空 clear 覆盖层，保证不会留下上一帧蓝/绿/红残留。
static func update_preview(timeline_ui: Control, timeline_manager: Object, shape_coords: Array[Vector2i], origin: Vector2i) -> void:
	if not is_instance_valid(timeline_ui):
		return
	if shape_coords.is_empty():
		clear_preview(timeline_ui)
		return
	if not _object_has_property(timeline_ui, &"grid_cells"):
		_remove_preview_overlay(timeline_ui)
		return

	if timeline_ui.has_method("clear_grid_preview"):
		timeline_ui.clear_grid_preview()

	var grid_width := int(timeline_ui.get("grid_width")) if _object_has_property(timeline_ui, &"grid_width") else 12
	var grid_height := int(timeline_ui.get("grid_height")) if _object_has_property(timeline_ui, &"grid_height") else 3
	var is_valid := is_origin_in_bounds(shape_coords, origin, grid_width, grid_height)
	var grid_cells = timeline_ui.get("grid_cells")
	if not (grid_cells is Dictionary):
		_remove_preview_overlay(timeline_ui)
		return

	var overlay := _get_or_create_preview_overlay(timeline_ui)
	var slot_size := float(timeline_ui.get("slot_size")) if _object_has_property(timeline_ui, &"slot_size") else 40.0
	var spacing := float(timeline_ui.get("spacing")) if _object_has_property(timeline_ui, &"spacing") else 2.0
	var cell_step := slot_size + spacing

	for offset in shape_coords:
		var pos := origin + offset
		var color := PREVIEW_INVALID_COLOR
		if is_valid:
			color = PREVIEW_OCCUPIED_COLOR if _is_cell_occupied(timeline_manager, pos) else PREVIEW_EMPTY_COLOR

		if grid_cells.has(pos):
			var cell = grid_cells[pos]
			if cell is Panel:
				_apply_cell_color(cell, color)

		if overlay != null and pos.x >= 0 and pos.x < grid_width and pos.y >= 0 and pos.y < grid_height:
			var preview_block := Panel.new()
			preview_block.name = "ClearPreviewCell_%s_%s" % [pos.x, pos.y]
			preview_block.mouse_filter = Control.MOUSE_FILTER_IGNORE
			preview_block.position = Vector2(pos.x * cell_step, pos.y * cell_step)
			preview_block.size = Vector2(slot_size, slot_size)
			preview_block.custom_minimum_size = preview_block.size
			_apply_cell_color(preview_block, color)
			overlay.add_child(preview_block)


## 清理 clear 专用预览。
## DragShapeController 在鼠标离开时间轴、确认施放、取消拖拽时都会调用它，确保蓝/绿/红示意格不残留。
static func clear_preview(timeline_ui: Control) -> void:
	if not is_instance_valid(timeline_ui):
		return
	_remove_preview_overlay(timeline_ui)
	if timeline_ui.has_method("clear_grid_preview"):
		timeline_ui.clear_grid_preview()


## 执行 clear 即时效果。
## 这里不会创建新的 TimelineAction，只会把范围内已有 action 找出来并移除。
## 多格 action 只要任意一格被 clear 命中，就整体移除；否则会留下“残缺行动”，破坏时间轴一致性。
static func execute(origin: Vector2i, shape_coords: Array[Vector2i], timeline_manager: Object, timeline_ui: Control) -> Array:
	var removed_actions: Array = []
	if not is_instance_valid(timeline_manager):
		return removed_actions
	if not _object_has_property(timeline_manager, &"grid"):
		return removed_actions

	var grid = timeline_manager.get("grid")
	if not (grid is Dictionary):
		return removed_actions

	var processed := {}
	for offset in shape_coords:
		var pos := origin + offset
		if not grid.has(pos):
			continue

		var action = grid[pos]
		if not is_instance_valid(action):
			continue

		var action_id: int = action.get_instance_id()
		if processed.has(action_id):
			continue
		processed[action_id] = true
		removed_actions.append(action)

	for action in removed_actions:
		if timeline_manager.has_method("remove_action_with_fade"):
			timeline_manager.remove_action_with_fade(action, timeline_ui, "clear_card")
		else:
			_erase_action_without_animation(timeline_manager, action)

	return removed_actions


static func _get_card_info(card: Object) -> Dictionary:
	if card == null:
		return {}
	if not _object_has_property(card, &"card_info"):
		return {}

	var value = card.get("card_info")
	return value if typeof(value) == TYPE_DICTIONARY else {}


static func _get_effects(card_info: Dictionary) -> Array:
	var effects = card_info.get("effects", [])
	return effects if typeof(effects) == TYPE_ARRAY else []


static func _normalize_coords(coords: Array[Vector2i]) -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	if coords.is_empty():
		result.append(Vector2i.ZERO)
		return result

	var min_x := coords[0].x
	var min_y := coords[0].y
	for coord in coords:
		min_x = min(min_x, coord.x)
		min_y = min(min_y, coord.y)

	for coord in coords:
		var normalized := Vector2i(coord.x - min_x, coord.y - min_y)
		if not result.has(normalized):
			result.append(normalized)

	return result


static func _is_cell_occupied(timeline_manager: Object, pos: Vector2i) -> bool:
	if not is_instance_valid(timeline_manager):
		return false
	if not _object_has_property(timeline_manager, &"grid"):
		return false

	var grid = timeline_manager.get("grid")
	return grid is Dictionary and grid.has(pos)


static func _apply_cell_color(cell: Panel, color: Color) -> void:
	var style = cell.get_theme_stylebox("panel")
	var style_copy := StyleBoxFlat.new()
	if style != null:
		style_copy = style.duplicate() as StyleBoxFlat
		if style_copy == null:
			style_copy = StyleBoxFlat.new()
	style_copy.bg_color = color
	style_copy.set_border_width_all(2)
	style_copy.border_color = PREVIEW_BORDER_COLOR
	cell.add_theme_stylebox_override("panel", style_copy)
	cell.scale = Vector2.ONE


static func _get_or_create_preview_overlay(timeline_ui: Control) -> Control:
	var parent := _get_preview_parent(timeline_ui)
	if parent == null:
		return null

	var overlay: Control = null
	for child in parent.get_children():
		if not _is_preview_overlay(child):
			continue

		if overlay == null and child is Control and not child.is_queued_for_deletion():
			overlay = child
		else:
			# 旧实现每帧 queue_free() 后立刻新建同名节点，Godot 可能把重复节点改名，
			# 导致后续 get_node_or_null() 只能删掉其中一个。这里主动收束成唯一覆盖层。
			parent.remove_child(child)
			child.queue_free()

	if overlay == null:
		overlay = Control.new()
		overlay.name = PREVIEW_OVERLAY_NAME
		overlay.set_meta(PREVIEW_OVERLAY_META, true)
		overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
		overlay.z_index = 500
		parent.add_child(overlay)

	# 模仿 timeline_ui.update_grid_preview() 的刷新方式：
	# 每帧先把上一帧示意格从树上移走，再用当前鼠标位置重新绘制。
	_clear_overlay_blocks(overlay)
	parent.move_child(overlay, parent.get_child_count() - 1)
	return overlay


static func _remove_preview_overlay(timeline_ui: Control) -> void:
	var parent := _get_preview_parent(timeline_ui)
	if parent == null:
		return

	for child in parent.get_children():
		if not _is_preview_overlay(child):
			continue
		# remove_child() 会立刻让残留预览从画面消失；
		# queue_free() 只负责稍后释放资源，避免“鼠标移开但还显示一帧/多帧”的观感。
		parent.remove_child(child)
		child.queue_free()


static func _clear_overlay_blocks(overlay: Control) -> void:
	if not is_instance_valid(overlay):
		return

	for child in overlay.get_children():
		overlay.remove_child(child)
		child.queue_free()


static func _is_preview_overlay(node: Node) -> bool:
	if node == null:
		return false
	if node.has_meta(PREVIEW_OVERLAY_META):
		return true
	# 兼容已经生成在场景里的旧残留节点；重复命名时 Godot 可能给节点名追加前后缀。
	return str(node.name).contains(PREVIEW_OVERLAY_NAME)


static func _get_preview_parent(timeline_ui: Control) -> Control:
	if not is_instance_valid(timeline_ui):
		return null

	var shape_layer = timeline_ui.get_node_or_null("ShapeLayer")
	if shape_layer is Control:
		return shape_layer
	return timeline_ui


static func _erase_action_without_animation(timeline_manager: Object, action: Object) -> void:
	if not is_instance_valid(timeline_manager):
		return
	if not _object_has_property(timeline_manager, &"grid"):
		return

	var grid = timeline_manager.get("grid")
	if not (grid is Dictionary):
		return

	if action.has_method("get_absolute_coords"):
		for coord in action.get_absolute_coords():
			if grid.get(coord) == action:
				grid.erase(coord)


static func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
