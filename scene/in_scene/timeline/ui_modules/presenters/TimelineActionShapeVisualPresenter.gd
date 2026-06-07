extends RefCounted

const TimelineActionShapeVisualScript = preload("res://scene/in_scene/timeline/TimelineActionShapeVisual.gd")

## TimelineActionShapeVisualPresenter 只负责时间轴行动整体形状视觉层。
## 它不创建行动方块，不连接 hover，不创建敌方意图 Overlay，也不修改 TimelineManager 数据。


func build_config(
	slot_size: float,
	spacing: float,
	outline_color: Color,
	outline_width: float,
	internal_seam_color: Color,
	internal_seam_width: float
) -> Dictionary:
	return {
		"slot_size": slot_size,
		"spacing": spacing,
		"outline_color": outline_color,
		"outline_width": outline_width,
		"internal_seam_color": internal_seam_color,
		"internal_seam_width": internal_seam_width,
	}


func get_local_shape_coords(shape_coords: Array[Vector2i], min_x: int, min_y: int) -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for coord in shape_coords:
		result.append(Vector2i(coord.x - min_x, coord.y - min_y))
	return result


func add_action_shape_visual(
	shape_container: Control,
	local_shape_coords: Array[Vector2i],
	action_color: Color,
	visual_mode: int,
	slot_size: float,
	spacing: float,
	outline_color: Color,
	outline_width: float,
	internal_seam_color: Color,
	internal_seam_width: float
) -> void:
	add_action_shape_visual_from_config(
		shape_container,
		local_shape_coords,
		action_color,
		visual_mode,
		build_config(
			slot_size,
			spacing,
			outline_color,
			outline_width,
			internal_seam_color,
			internal_seam_width
		)
	)


func add_action_shape_visual_from_config(
	shape_container: Control,
	local_shape_coords: Array[Vector2i],
	action_color: Color,
	visual_mode: int,
	config: Dictionary
) -> void:
	if not is_instance_valid(shape_container) or local_shape_coords.is_empty():
		return

	var slot_size: float = config["slot_size"]
	var spacing: float = config["spacing"]
	var outline_color: Color = config["outline_color"]
	var outline_width: float = config["outline_width"]
	var internal_seam_color: Color = config["internal_seam_color"]
	var internal_seam_width: float = config["internal_seam_width"]
	var visual := TimelineActionShapeVisualScript.new() as TimelineActionShapeVisual
	visual.name = "ActionBackplateVisual" if visual_mode == TimelineActionShapeVisual.VisualMode.BACKPLATE else "ActionOutlineVisual"
	visual.size = shape_container.size
	visual.custom_minimum_size = shape_container.size
	visual.mouse_filter = Control.MOUSE_FILTER_IGNORE
	visual.z_index = 20 if visual_mode == TimelineActionShapeVisual.VisualMode.OUTLINE else 0
	visual.setup(
		visual_mode,
		local_shape_coords,
		slot_size,
		spacing,
		action_color,
		outline_color,
		outline_width,
		internal_seam_color,
		internal_seam_width
	)
	shape_container.add_child(visual)
