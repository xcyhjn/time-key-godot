class_name CursorTooltipController
extends RefCounted


## CursorTooltipController 只负责光标提示框的视觉包装、定位和尺寸同步。
## 它不决定提示文本内容，也不参与卡牌目标判定，避免把战斗规则带进 UI 层。


func enhance(cursor_tooltip: Variant, tooltip_config: Variant, visibility_changed_callback: Callable) -> PanelContainer:
	if not is_instance_valid(cursor_tooltip) or not (cursor_tooltip is RichTextLabel):
		return null

	var label: RichTextLabel = cursor_tooltip as RichTextLabel
	label.hide()

	if label.get_parent() is PanelContainer:
		return label.get_parent() as PanelContainer

	var original_parent: Node = label.get_parent()
	if not is_instance_valid(original_parent):
		return null

	var original_position: Vector2 = label.global_position
	var original_visible: bool = label.visible
	var original_z_index: int = label.z_index

	var panel: PanelContainer = PanelContainer.new()
	panel.z_index = original_z_index + 1
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	panel.visible = original_visible
	panel.add_theme_stylebox_override("panel", _build_panel_style(tooltip_config))

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 8)
	margin.add_theme_constant_override("margin_bottom", 8)
	panel.add_child(margin)

	original_parent.remove_child(label)
	margin.add_child(label)
	original_parent.add_child(panel)
	panel.global_position = original_position
	panel.custom_minimum_size = Vector2.ZERO

	label.add_theme_stylebox_override("normal", StyleBoxEmpty.new())
	label.fit_content = true
	label.scroll_active = false
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.custom_minimum_size = Vector2.ZERO

	if visibility_changed_callback.is_valid() and not label.visibility_changed.is_connected(visibility_changed_callback):
		label.visibility_changed.connect(visibility_changed_callback)

	return panel


func sync_visibility(cursor_tooltip: Variant, cursor_tooltip_panel: Variant, size_config: Dictionary) -> void:
	if is_instance_valid(cursor_tooltip_panel) and is_instance_valid(cursor_tooltip):
		refresh_size(cursor_tooltip, cursor_tooltip_panel, size_config)
		cursor_tooltip_panel.visible = cursor_tooltip.visible


func set_position(cursor_tooltip: Variant, cursor_tooltip_panel: Variant, position: Vector2, size_config: Dictionary) -> void:
	refresh_size(cursor_tooltip, cursor_tooltip_panel, size_config)
	if is_instance_valid(cursor_tooltip_panel):
		cursor_tooltip_panel.global_position = position
	elif is_instance_valid(cursor_tooltip):
		cursor_tooltip.global_position = position


func refresh_size(cursor_tooltip: Variant, cursor_tooltip_panel: Variant, size_config: Dictionary) -> void:
	if not is_instance_valid(cursor_tooltip) or not (cursor_tooltip is RichTextLabel):
		return

	var label: RichTextLabel = cursor_tooltip as RichTextLabel
	var min_width: float = maxf(float(size_config.get("min_width", 1.0)), 1.0)
	var max_width: float = maxf(float(size_config.get("max_width", min_width)), min_width)
	var min_height: float = float(size_config.get("min_height", 1.0))
	var original_autowrap: int = label.autowrap_mode

	label.size = Vector2.ZERO
	label.custom_minimum_size = Vector2.ZERO
	label.autowrap_mode = TextServer.AUTOWRAP_OFF
	var content_width: float = float(label.get_content_width())
	label.autowrap_mode = original_autowrap

	var target_width: float = clampf(content_width, min_width, max_width)
	label.size = Vector2(target_width, 0.0)
	var target_height: float = maxf(float(label.get_content_height()), min_height)
	label.custom_minimum_size = Vector2(target_width, target_height)

	if is_instance_valid(cursor_tooltip_panel):
		cursor_tooltip_panel.size = Vector2.ZERO
		cursor_tooltip_panel.custom_minimum_size = Vector2.ZERO


func _build_panel_style(tooltip_config: Variant) -> StyleBoxFlat:
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = tooltip_config.tooltip_bg_color
	style.border_width_left = tooltip_config.tooltip_border_width
	style.border_width_top = tooltip_config.tooltip_border_width
	style.border_width_right = tooltip_config.tooltip_border_width
	style.border_width_bottom = tooltip_config.tooltip_border_width
	style.border_color = tooltip_config.tooltip_border_color
	style.set_corner_radius_all(tooltip_config.tooltip_corner_radius)
	return style
