class_name HandDiscardFlowController
extends RefCounted


## HandDiscardFlowController 负责“结束回合时强制弃掉全部手牌”。
## 它不推进回合、不结算时间轴，也不决定什么时候调用。


func discard_all(config: Dictionary) -> Dictionary:
	var player_hand: Hand = config.get("player_hand") as Hand
	var discard_pile: Pile = config.get("discard_pile") as Pile
	if not is_instance_valid(player_hand) or not is_instance_valid(discard_pile):
		return {"discarded_count": 0}
	if player_hand._held_cards.is_empty():
		return {"discarded_count": 0}

	var cards_to_discard: Array = player_hand._held_cards.duplicate()
	_prepare_cards_for_forced_discard(cards_to_discard)
	_call_if_valid(config.get("hide_tooltip"))
	discard_pile.move_cards(cards_to_discard)
	await _wait_process_frame(config.get("tree"))

	return {"discarded_count": cards_to_discard.size()}


func _prepare_cards_for_forced_discard(cards: Array) -> void:
	for card in cards:
		if not is_instance_valid(card):
			continue
		if card.has_method("force_deselect"):
			card.force_deselect()
		if card.has_method("change_state"):
			card.change_state(0)


func _wait_process_frame(tree: Variant) -> void:
	if is_instance_valid(tree):
		await tree.process_frame


func _call_if_valid(callback: Variant) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.call()
