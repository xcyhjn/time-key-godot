extends RefCounted

## CraftResultDescriptionPositionPresenter 只负责合成结果描述面板的尺寸计算和屏幕内定位。
## 它不写入描述文本，不配置面板样式，也不修改合成结果或槽位状态。


func position_panel(
	result_description_panel: PanelContainer,
	result_description_label: RichTextLabel,
	result_preview_card: Control,
	result_slot: Button,
	screen_size: Vector2,
	offset_x: int,
	offset_y: int,
	max_width: int
) -> void:
	if not is_instance_valid(result_description_panel) or not is_instance_valid(result_preview_card):
		return

	result_description_label.custom_minimum_size = Vector2.ZERO
	result_description_label.reset_size()
	result_description_panel.reset_size()
	await result_description_panel.get_tree().process_frame

	var content_size = result_description_label.get_combined_minimum_size()
	var panel_w = min(max(content_size.x + 36.0, 160.0), float(max_width))
	result_description_label.custom_minimum_size = Vector2(panel_w - 36.0, 0.0)
	result_description_label.reset_size()
	result_description_panel.reset_size()
	await result_description_panel.get_tree().process_frame
	content_size = result_description_label.get_combined_minimum_size()
	var panel_h = max(content_size.y + 36.0, 64.0)

	result_description_panel.size = Vector2(panel_w, panel_h)

	var anchor_pos = result_slot.global_position
	var anchor_size = result_slot.size
	var panel_x = anchor_pos.x + anchor_size.x + offset_x
	var panel_y = anchor_pos.y + offset_y

	if panel_x + panel_w > screen_size.x:
		panel_x = anchor_pos.x - panel_w - offset_x

	if panel_y + panel_h > screen_size.y - 4:
		panel_y = screen_size.y - panel_h - 4

	result_description_panel.global_position = Vector2(panel_x, panel_y)
