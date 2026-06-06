extends RefCounted

## RemoveDeckDisplayCleaner 只负责清空删除奖励页的牌组显示节点和展示状态。
## 它不删除真实牌组数据，不关闭场景，也不生成新的卡牌。

const SELECTED_DRAFT_CARD_KEY := "selected_draft_card"


func clear_deck_display(
	current_deck_cards: Array,
	original_deck_card_ids: Array[String],
	deck_grid: GridContainer,
	selected_card_display: Control
) -> Dictionary:
	for card in current_deck_cards:
		if is_instance_valid(card):
			card.queue_free()

	current_deck_cards.clear()
	original_deck_card_ids.clear()

	for child in deck_grid.get_children():
		child.queue_free()

	for child in selected_card_display.get_children():
		child.queue_free()

	var clear_state: Dictionary = {}
	clear_state[SELECTED_DRAFT_CARD_KEY] = null
	return clear_state
