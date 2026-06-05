class_name TimelineGridCellInteractionPresenter
extends RefCounted

## TimelineGridCellInteractionPresenter 只负责 TimelineUI 空背景格子的坐标换算和 hover 样式。
## 它不发射业务信号，不处理拖拽预览，也不读取 TimelineManager 数据。


func get_grid_pos(cell_index: int, grid_width: int) -> Vector2i:
	if grid_width <= 0:
		return Vector2i.ZERO
	return Vector2i(cell_index % grid_width, cell_index / grid_width)


func apply_cell_color(grid_cells: Dictionary, grid_pos: Vector2i, color: Color) -> void:
	var cell = grid_cells.get(grid_pos)
	if not (cell is Panel):
		return

	var style = cell.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
	if not style:
		style = StyleBoxFlat.new()
	style.bg_color = color
	style.set_border_width_all(0)
	cell.add_theme_stylebox_override("panel", style)
