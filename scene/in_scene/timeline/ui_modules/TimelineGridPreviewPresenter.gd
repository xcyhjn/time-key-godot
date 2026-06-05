class_name TimelineGridPreviewPresenter
extends RefCounted

## TimelineGridPreviewPresenter 只负责 TimelineUI 空背景格子的拖拽预览样式。
## 它不修改 TimelineManager 数据，不创建行动块，也不处理卡牌放置规则。


func update_grid_preview(
	grid_cells: Dictionary,
	timeline_manager: TimelineManager,
	shape_coords: Array[Vector2i],
	origin_pos: Vector2i,
	is_valid: bool,
	grid_width: int,
	grid_height: int,
	default_color: Color,
	create_tween_callback: Callable
) -> void:
	clear_grid_preview(grid_cells, default_color)
	if shape_coords.is_empty():
		return

	var covered_cells: Array[Vector2i] = []
	var all_in_bounds = true
	for coord in shape_coords:
		var cell_pos = Vector2i(origin_pos.x + coord.x, origin_pos.y + coord.y)
		covered_cells.append(cell_pos)
		if not _is_in_bounds(cell_pos, grid_width, grid_height):
			all_in_bounds = false

	var overlaps_enemy_intent = _has_enemy_intent_overlap(timeline_manager, covered_cells)
	var is_preview_valid = is_valid and all_in_bounds

	for cell_pos in covered_cells:
		if not _is_in_bounds(cell_pos, grid_width, grid_height):
			continue
		if not grid_cells.has(cell_pos):
			continue

		var cell = grid_cells[cell_pos] as Panel
		if not cell:
			continue

		_apply_preview_style(cell, is_preview_valid, overlaps_enemy_intent)
		_play_preview_scale(cell, create_tween_callback)


func clear_grid_preview(grid_cells: Dictionary, default_color: Color) -> void:
	for cell_pos in grid_cells.keys():
		var cell = grid_cells[cell_pos] as Panel
		if not cell:
			continue

		var style = _get_panel_style(cell)
		style.bg_color = default_color
		style.set_border_width_all(0)
		cell.add_theme_stylebox_override("panel", style)
		cell.scale = Vector2.ONE


func _is_in_bounds(cell_pos: Vector2i, grid_width: int, grid_height: int) -> bool:
	return cell_pos.x >= 0 and cell_pos.x < grid_width and cell_pos.y >= 0 and cell_pos.y < grid_height


func _has_enemy_intent_overlap(timeline_manager: TimelineManager, covered_cells: Array[Vector2i]) -> bool:
	if not timeline_manager:
		return false

	for cell_pos in covered_cells:
		if not timeline_manager.grid.has(cell_pos):
			continue
		var action = timeline_manager.grid[cell_pos] as TimelineAction
		if action and action.type == TimelineAction.Type.ENEMY:
			return true
	return false


func _apply_preview_style(cell: Panel, is_preview_valid: bool, overlaps_enemy_intent: bool) -> void:
	var style = _get_panel_style(cell)
	if is_preview_valid:
		style.bg_color = Color.SKY_BLUE
		style.set_border_width_all(0)
	elif overlaps_enemy_intent:
		style.bg_color = Color.TRANSPARENT
		style.set_border_width_all(2)
		style.border_color = Color.INDIAN_RED
	else:
		style.bg_color = Color.INDIAN_RED
		style.set_border_width_all(0)
	cell.add_theme_stylebox_override("panel", style)


func _play_preview_scale(cell: Panel, create_tween_callback: Callable) -> void:
	if not create_tween_callback.is_valid():
		return
	var tw = create_tween_callback.call()
	if not (tw is Tween):
		return
	cell.scale = Vector2(0.9, 0.9)
	tw.tween_property(cell, "scale", Vector2.ONE, 0.15)


func _get_panel_style(cell: Panel) -> StyleBoxFlat:
	var style = cell.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
	if not style:
		style = StyleBoxFlat.new()
	return style
