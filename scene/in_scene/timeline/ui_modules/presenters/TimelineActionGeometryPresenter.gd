extends RefCounted

## TimelineActionGeometryPresenter 只负责计算并应用时间轴行动容器的几何信息。
## 它不创建行动方块，不连接 hover，不播放动画，也不修改 TimelineManager 数据。


func get_shape_bounds(shape_coords: Array[Vector2i]) -> Dictionary:
	var result := {
		"min_x": 0,
		"max_x": 0,
		"min_y": 0,
		"max_y": 0,
	}
	if shape_coords.is_empty():
		return result

	result["min_x"] = shape_coords[0].x
	result["max_x"] = shape_coords[0].x
	result["min_y"] = shape_coords[0].y
	result["max_y"] = shape_coords[0].y
	for offset in shape_coords:
		result["min_x"] = min(result["min_x"], offset.x)
		result["max_x"] = max(result["max_x"], offset.x)
		result["min_y"] = min(result["min_y"], offset.y)
		result["max_y"] = max(result["max_y"], offset.y)
	return result


func apply_shape_container_geometry(
	shape_container: Control,
	origin_grid_pos: Vector2i,
	shape_bounds: Dictionary,
	slot_size: float,
	spacing: float
) -> void:
	var min_x: int = shape_bounds["min_x"]
	var max_x: int = shape_bounds["max_x"]
	var min_y: int = shape_bounds["min_y"]
	var max_y: int = shape_bounds["max_y"]
	var cell_span: float = slot_size + spacing
	var width: float = (max_x - min_x + 1) * slot_size + max(0, max_x - min_x) * spacing
	var height: float = (max_y - min_y + 1) * slot_size + max(0, max_y - min_y) * spacing

	shape_container.size = Vector2(width, height)
	shape_container.pivot_offset = shape_container.size * 0.5
	shape_container.position = Vector2(
		(origin_grid_pos.x + min_x) * cell_span,
		(origin_grid_pos.y + min_y) * cell_span
	)


func get_block_local_position(
	target_grid_pos: Vector2i,
	origin_grid_pos: Vector2i,
	shape_bounds: Dictionary,
	slot_size: float,
	spacing: float
) -> Vector2:
	var min_x: int = shape_bounds["min_x"]
	var min_y: int = shape_bounds["min_y"]
	var cell_span: float = slot_size + spacing
	return Vector2(
		(target_grid_pos.x - (origin_grid_pos.x + min_x)) * cell_span,
		(target_grid_pos.y - (origin_grid_pos.y + min_y)) * cell_span
	)
