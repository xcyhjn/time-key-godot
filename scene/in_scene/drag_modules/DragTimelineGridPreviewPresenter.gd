class_name DragTimelineGridPreviewPresenter
extends RefCounted


## DragTimelineGridPreviewPresenter 只负责把拖拽形状预览转发给时间轴 UI。
## 它不计算鼠标坐标、不判断放置是否合法，也不创建 TimelineAction。


const TimelineClearEffectUtil = preload("res://scene/in_scene/timeline/TimelineClearEffect.gd")


func update_preview(
	timeline_ui: Control,
	timeline_manager: Node,
	shape_coords: Array[Vector2i],
	grid_pos: Vector2i,
	is_valid: bool,
	is_timeline_clear_mode: bool
) -> void:
	if not is_instance_valid(timeline_ui) or shape_coords.is_empty():
		return

	if is_timeline_clear_mode:
		TimelineClearEffectUtil.update_preview(timeline_ui, timeline_manager, shape_coords, grid_pos)
		return

	if timeline_ui.has_method("update_grid_preview"):
		timeline_ui.update_grid_preview(shape_coords, grid_pos, is_valid)


func clear_preview(timeline_ui: Control, is_timeline_clear_mode: bool) -> void:
	if not is_instance_valid(timeline_ui):
		return

	if is_timeline_clear_mode:
		TimelineClearEffectUtil.clear_preview(timeline_ui)
	elif timeline_ui.has_method("clear_grid_preview"):
		timeline_ui.clear_grid_preview()
