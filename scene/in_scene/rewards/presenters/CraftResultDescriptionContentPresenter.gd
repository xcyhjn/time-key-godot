extends RefCounted

## CraftResultDescriptionContentPresenter 只负责合成结果描述面板的文本显示和隐藏。
## 它不计算面板位置，不配置面板样式，也不修改合成结果或槽位状态。


func update_result_description(
	result_preview_card: Control,
	result_description_panel: PanelContainer,
	result_description_label: RichTextLabel,
	has_object_property: Callable,
	position_callback: Callable
) -> void:
	if not is_instance_valid(result_description_panel) or not is_instance_valid(result_description_label):
		return

	if is_instance_valid(result_preview_card):
		var description_text := _extract_description_text(result_preview_card, has_object_property)
		result_description_label.clear()
		result_description_label.append_text(description_text if description_text != "" else "无效果文本")
		result_description_panel.size = Vector2.ZERO
		result_description_panel.show()
		position_callback.call_deferred()
	else:
		result_description_panel.hide()
		result_description_label.clear()


func _extract_description_text(result_preview_card: Control, has_object_property: Callable) -> String:
	if result_preview_card.has_method("get_parsed_description"):
		return str(result_preview_card.get_parsed_description())
	if has_object_property.call(result_preview_card, &"raw_description"):
		return str(result_preview_card.get("raw_description"))
	return ""
