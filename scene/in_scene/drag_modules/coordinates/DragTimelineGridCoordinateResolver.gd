extends RefCounted


## DragTimelineGridCoordinateResolver 只负责把时间轴本地鼠标位置换算成网格坐标和边界状态。
## 它不判断卡牌形状是否合法，不更新预览，也不执行放置。


const DEFAULT_GRID_WIDTH: int = 12
const DEFAULT_GRID_HEIGHT: int = 3


func resolve_hover(timeline_ui: Control, fallback_slot_size: float, fallback_spacing: float) -> Dictionary:
	return _resolve(timeline_ui, fallback_slot_size, fallback_spacing, DEFAULT_GRID_WIDTH, DEFAULT_GRID_HEIGHT, true)


func resolve_fixed_bounds(
	timeline_ui: Control,
	fallback_slot_size: float,
	fallback_spacing: float,
	grid_width: int,
	grid_height: int
) -> Dictionary:
	return _resolve(timeline_ui, fallback_slot_size, fallback_spacing, grid_width, grid_height, false)


func _resolve(
	timeline_ui: Control,
	fallback_slot_size: float,
	fallback_spacing: float,
	fallback_grid_width: int,
	fallback_grid_height: int,
	use_timeline_grid_size: bool
) -> Dictionary:
	var local_mouse: Vector2 = Vector2.ZERO
	var has_grid_background: bool = false
	if is_instance_valid(timeline_ui) and _object_has_property(timeline_ui, &"grid_background"):
		var grid_background: Variant = timeline_ui.get("grid_background")
		if is_instance_valid(grid_background) and grid_background.has_method("get_local_mouse_position"):
			has_grid_background = true
			local_mouse = grid_background.get_local_mouse_position()

	var ui_slot_size: float = _get_float_property(timeline_ui, &"slot_size", fallback_slot_size)
	var ui_spacing: float = _get_float_property(timeline_ui, &"spacing", fallback_spacing)
	var cell_size: float = ui_slot_size + ui_spacing
	var grid_x: int = int(local_mouse.x / cell_size)
	var grid_y: int = int(local_mouse.y / cell_size)

	var grid_width: int = fallback_grid_width
	var grid_height: int = fallback_grid_height
	if use_timeline_grid_size:
		grid_width = _get_int_property(timeline_ui, &"grid_width", fallback_grid_width)
		grid_height = _get_int_property(timeline_ui, &"grid_height", fallback_grid_height)

	var is_in_bounds: bool = (
		has_grid_background
		and local_mouse.x >= 0.0
		and local_mouse.y >= 0.0
		and grid_x >= 0
		and grid_x < grid_width
		and grid_y >= 0
		and grid_y < grid_height
	)

	return {
		"local_mouse": local_mouse,
		"grid_pos": Vector2i(grid_x, grid_y),
		"is_in_bounds": is_in_bounds,
		"grid_width": grid_width,
		"grid_height": grid_height,
	}


func _get_float_property(target: Object, property_name: StringName, default_value: float) -> float:
	if target == null:
		return default_value
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return float(target.get(property_name))
	return default_value


func _get_int_property(target: Object, property_name: StringName, default_value: int) -> int:
	if target == null:
		return default_value
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return int(target.get(property_name))
	return default_value


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
