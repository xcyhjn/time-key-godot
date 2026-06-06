extends RefCounted

## CraftSlotPlaceholderPresenter 只负责合成槽位和结果槽占位符的显隐与文案。
## 它不创建或释放预览卡，不判断配方，也不修改合成选择状态。


func refresh_slot_placeholders(
	slot1_placeholder: Label,
	slot2_placeholder: Label,
	result_placeholder: Label,
	slot_preview_cards: Dictionary,
	slot_entries: Dictionary,
	result_preview_card: Control,
	slot_1: int,
	slot_2: int,
	no_recipe_text: String
) -> void:
	if is_instance_valid(slot1_placeholder):
		slot1_placeholder.visible = slot_preview_cards[slot_1] == null
	if is_instance_valid(slot2_placeholder):
		slot2_placeholder.visible = slot_preview_cards[slot_2] == null

	if not is_instance_valid(result_placeholder):
		return

	if result_preview_card != null:
		result_placeholder.visible = false
	elif slot_entries[slot_1] != null and slot_entries[slot_2] != null:
		result_placeholder.visible = true
		result_placeholder.text = no_recipe_text
	else:
		result_placeholder.visible = true
		result_placeholder.text = "结果槽"
