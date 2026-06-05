class_name DragTimelineGridMouseFilterController
extends RefCounted


## DragTimelineGridMouseFilterController 只负责时间轴网格单元格的鼠标过滤状态。
## 它不展开或收起时间轴，不处理 hover，也不判断卡牌放置结果。


func disable_grid_cells(timeline_ui: Control) -> void:
	_set_grid_cells_mouse_filter(timeline_ui, Control.MOUSE_FILTER_IGNORE)


func restore_grid_cells(timeline_ui: Control) -> void:
	_set_grid_cells_mouse_filter(timeline_ui, Control.MOUSE_FILTER_PASS)


func _set_grid_cells_mouse_filter(timeline_ui: Control, mouse_filter: int) -> void:
	if not is_instance_valid(timeline_ui) or not _object_has_property(timeline_ui, &"grid_cells"):
		return

	var grid_cells: Variant = timeline_ui.grid_cells
	if not (grid_cells is Dictionary):
		return

	for cell: Variant in grid_cells.values():
		if cell is Control:
			cell.mouse_filter = mouse_filter


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
