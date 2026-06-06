extends RefCounted

## RemoveConfirmedCardUiCleaner 只负责确认删除动画完成后的卡牌显示清理。
## 它不删除真实牌组数据，不设置奖励提交状态，也不关闭奖励页。

const SELECTED_DRAFT_CARD_KEY := "selected_draft_card"


func cleanup_confirmed_card(
	selected_draft_card: Control,
	current_deck_cards: Array,
	deck_grid: GridContainer,
	selected_card_display: Control
) -> Dictionary:
	deck_grid.remove_child(selected_draft_card)
	selected_draft_card.queue_free()
	current_deck_cards.erase(selected_draft_card)

	for child in selected_card_display.get_children():
		child.queue_free()

	var cleanup_state: Dictionary = {}
	cleanup_state[SELECTED_DRAFT_CARD_KEY] = null
	return cleanup_state
