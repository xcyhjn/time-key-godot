class_name DragPlacementQueryService
extends RefCounted


## DragPlacementQueryService 只负责回答当前拖拽形状在指定时间轴格子是否可用。
## 它不执行放置，不创建 TimelineAction，也不修改时间轴或卡牌状态。


const TimelineClearEffectUtil = preload("res://scene/in_scene/timeline/TimelineClearEffect.gd")


func is_placement_valid(
	timeline_ui: Control,
	timeline_manager: Node,
	shape_coords: Array[Vector2i],
	grid_pos: Vector2i,
	is_timeline_clear_mode: bool
) -> bool:
	if shape_coords.is_empty():
		return false

	if is_timeline_clear_mode:
		var grid_width: int = _get_int_property(timeline_ui, &"grid_width", 12)
		var grid_height: int = _get_int_property(timeline_ui, &"grid_height", 3)
		return TimelineClearEffectUtil.is_origin_in_bounds(shape_coords, grid_pos, grid_width, grid_height)

	if not is_instance_valid(timeline_manager):
		return false

	if timeline_manager.has_method("is_placement_valid"):
		return timeline_manager.is_placement_valid(shape_coords, grid_pos)
	return false


func _get_int_property(target: Object, property_name: StringName, default_value: int) -> int:
	if target == null:
		return default_value
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return int(target.get(property_name))
	return default_value
