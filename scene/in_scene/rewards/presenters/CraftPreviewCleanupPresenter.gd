extends RefCounted

## CraftPreviewCleanupPresenter 只负责合成预览卡节点释放和预览引用清空结果。
## 它不创建预览卡，不刷新配方，不更新结果描述，也不修改牌组。

const RESULT_PREVIEW_CARD_KEY := "result_preview_card"
const CURRENT_RESULT_CARD_ID_KEY := "current_result_card_id"


func clear_all_previews(
	slot_preview_cards: Dictionary,
	result_preview_card: Control,
	slot_1: int,
	slot_2: int
) -> Dictionary:
	clear_slot_preview(slot_preview_cards, slot_1)
	clear_slot_preview(slot_preview_cards, slot_2)
	return clear_result_preview(result_preview_card)


func clear_slot_preview(slot_preview_cards: Dictionary, slot_index: int) -> void:
	var preview = slot_preview_cards[slot_index]
	if is_instance_valid(preview):
		preview.queue_free()
	slot_preview_cards[slot_index] = null


func clear_result_preview(result_preview_card: Control) -> Dictionary:
	if is_instance_valid(result_preview_card):
		result_preview_card.queue_free()

	var clear_state: Dictionary = {}
	clear_state[RESULT_PREVIEW_CARD_KEY] = null
	clear_state[CURRENT_RESULT_CARD_ID_KEY] = ""
	return clear_state
