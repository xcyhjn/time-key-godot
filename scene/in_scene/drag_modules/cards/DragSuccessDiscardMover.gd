extends RefCounted


## DragSuccessDiscardMover 只负责成功放置后把卡牌移入弃牌区并触发旧弃牌效果。
## 它不查找节点，不处理回手牌 fallback，也不收起时间轴。


func move_to_discard(card: Control, discard_pile: Node, main_board: Node) -> bool:
	if not is_instance_valid(card):
		return false
	if not is_instance_valid(discard_pile) or not discard_pile.has_method("move_cards"):
		return false

	discard_pile.move_cards([card])

	if is_instance_valid(main_board) and main_board.has_method("_handle_discard_effects"):
		main_board.call_deferred("_handle_discard_effects", card)

	return true
