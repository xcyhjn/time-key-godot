class_name DragPlacementTargetResolver
extends RefCounted


## DragPlacementTargetResolver 只负责计算卡牌放置动画的目标左上角位置。
## 它不锁定交互，不播放动画，也不执行时间轴放置或卡牌归属变更。


func resolve_card_top_left(
	timeline_ui: Control,
	card: Control,
	grid_pos: Vector2i,
	float_offset: Vector2,
	fallback_position: Vector2,
	target_card_scale: Vector2 = Vector2(0.9, 0.9)
) -> Vector2:
	var grid_cell_center: Vector2 = fallback_position
	if is_instance_valid(timeline_ui) and _object_has_property(timeline_ui, &"grid_cells"):
		var grid_cells = timeline_ui.get("grid_cells")
		if typeof(grid_cells) == TYPE_DICTIONARY and grid_cells.has(grid_pos):
			var target_cell = grid_cells[grid_pos]
			if target_cell is Control and is_instance_valid(target_cell):
				var actual_cell_size: Vector2 = target_cell.size * timeline_ui.scale
				grid_cell_center = target_cell.global_position + (actual_cell_size / 2.0) + float_offset

	var card_top_left: Vector2 = grid_cell_center
	if is_instance_valid(card) and card.has_method("get_size"):
		var card_size: Vector2 = card.get_size()
		card_top_left -= card_size * target_card_scale / 2.0

	return card_top_left


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
