extends RefCounted

## CraftResultPreviewPresenter 只负责把已创建的合成结果预览卡挂到结果槽。
## 它不创建预览卡，不读取配方，也不更新结果描述或连接线。


func attach_result_preview(
	result_anchor: Control,
	preview_card: Control,
	click_callback: Callable
) -> Control:
	if not is_instance_valid(result_anchor) or not is_instance_valid(preview_card):
		return null

	result_anchor.add_child(preview_card)
	preview_card.position = Vector2.ZERO

	if preview_card.has_method("set_selected"):
		preview_card.set_selected(true)

	if preview_card.has_signal("card_clicked") and click_callback.is_valid():
		preview_card.card_clicked.connect(click_callback)

	return preview_card
