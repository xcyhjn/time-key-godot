extends RefCounted

## RemoveDeckCardSelectionPresenter 只负责删除奖励页牌组卡牌的单选表现和按钮状态。
## 它不删除卡牌，不创建卡牌，也不修改牌组数据。


func apply_selection(clicked_card: Control, selected_draft_card: Control, current_deck_cards: Array, btn_confirm: Button, btn_back: Button) -> Control:
	if selected_draft_card == clicked_card:
		_set_cards_selected(current_deck_cards, null)
		btn_confirm.disabled = true
		btn_back.disabled = false
		return null

	_set_cards_selected(current_deck_cards, clicked_card)
	btn_confirm.disabled = false
	btn_back.disabled = true
	return clicked_card


func _set_cards_selected(current_deck_cards: Array, selected_card: Control) -> void:
	for card in current_deck_cards:
		card.set_selected(card == selected_card)
