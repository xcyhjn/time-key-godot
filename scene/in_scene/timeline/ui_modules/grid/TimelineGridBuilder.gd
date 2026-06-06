extends RefCounted

## TimelineGridBuilder 只负责创建 TimelineUI 的空背景格子。
## 它不处理 hover 状态，不更新拖拽预览，也不创建时间轴行动方块。


func build_background_grid(
	grid_background: GridContainer,
	grid_width: int,
	grid_height: int,
	slot_size: float,
	spacing: float,
	default_color: Color,
	gui_input_callback: Callable,
	mouse_entered_callback: Callable,
	mouse_exited_callback: Callable
) -> Dictionary:
	var grid_cells: Dictionary = {}
	if not is_instance_valid(grid_background):
		return grid_cells

	grid_background.columns = grid_width
	grid_background.add_theme_constant_override("h_separation", spacing)
	grid_background.add_theme_constant_override("v_separation", spacing)

	for child in grid_background.get_children():
		child.queue_free()

	for i in range(grid_width * grid_height):
		var slot := Panel.new()
		slot.custom_minimum_size = Vector2(slot_size, slot_size)
		slot.add_theme_stylebox_override("panel", _create_default_style(default_color))
		slot.mouse_filter = Control.MOUSE_FILTER_PASS

		if gui_input_callback.is_valid():
			slot.gui_input.connect(gui_input_callback.bind(i))
		if mouse_entered_callback.is_valid():
			slot.mouse_entered.connect(mouse_entered_callback.bind(i))
		if mouse_exited_callback.is_valid():
			slot.mouse_exited.connect(mouse_exited_callback.bind(i))

		grid_background.add_child(slot)

		var grid_pos := Vector2i(i % grid_width, i / grid_width)
		grid_cells[grid_pos] = slot

	return grid_cells


func _create_default_style(default_color: Color) -> StyleBoxFlat:
	var default_style := StyleBoxFlat.new()
	default_style.bg_color = default_color
	default_style.set_border_width_all(0)
	return default_style
