extends RefCounted

## CraftResultDescriptionPanelPresenter 只负责合成结果描述面板的基础样式配置。
## 它不读取卡牌描述，不计算面板位置，也不改变合成状态。


func setup_panel(panel: PanelContainer, label: RichTextLabel) -> void:
	if not is_instance_valid(panel) or not is_instance_valid(label):
		return

	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.custom_minimum_size = Vector2.ZERO
	panel.size = Vector2.ZERO
	panel.add_theme_stylebox_override("panel", _create_panel_style())

	label.custom_minimum_size = Vector2.ZERO
	label.fit_content = true
	label.scroll_active = false
	label.bbcode_enabled = true
	label.clip_contents = false
	label.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	label.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	label.add_theme_color_override("default_color", Color(0.95, 0.95, 0.95, 1.0))


func _create_panel_style() -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.12, 0.12, 0.95)
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.border_color = Color(0.8, 0.6, 0.2, 1.0)
	style.set_corner_radius_all(6)
	return style
