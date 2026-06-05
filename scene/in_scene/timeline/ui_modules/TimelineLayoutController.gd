class_name TimelineLayoutController
extends RefCounted

## TimelineLayoutController 只负责 TimelineUI 的顶部锚点布局和背景网格对齐。
## 它不创建行动块，不处理展开动画，也不读取 TimelineManager 数据。


func apply_anchor_layout(
	owner: Control,
	grid_background: Control,
	grid_width: int,
	grid_height: int,
	slot_size: float,
	spacing: float,
	margin_top_preset: float,
	top_reserved_space: float
) -> void:
	if not is_instance_valid(owner):
		return

	owner.set_anchors_preset(Control.PRESET_CENTER_TOP, true)

	var actual_width = (grid_width * slot_size) + ((grid_width - 1) * spacing)
	var actual_height = (grid_height * slot_size) + ((grid_height - 1) * spacing)
	var effective_top_offset = margin_top_preset + top_reserved_space

	owner.offset_left = -actual_width / 2.0
	owner.offset_right = actual_width / 2.0
	owner.offset_top = effective_top_offset
	owner.offset_bottom = effective_top_offset + actual_height

	owner.force_update_transform()
	owner.pivot_offset = Vector2(actual_width / 2.0, 0.0)

	if is_instance_valid(grid_background):
		grid_background.position = Vector2.ZERO
		grid_background.size = owner.size
