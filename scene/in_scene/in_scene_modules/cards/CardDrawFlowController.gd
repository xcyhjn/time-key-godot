class_name CardDrawFlowController
extends RefCounted


## CardDrawFlowController 负责抽牌和洗牌的异步动作。
## 战斗阶段判断和 is_processing_deck 锁仍由 InScene 主脚本维护。


func draw_cards(config: Dictionary) -> Dictionary:
	var player_hand: Hand = config.get("player_hand") as Hand
	var deck_pile: Pile = config.get("deck_pile") as Pile
	var discard_pile: Pile = config.get("discard_pile") as Pile
	if not is_instance_valid(player_hand) or not is_instance_valid(deck_pile) or not is_instance_valid(discard_pile):
		return {"drawn_count": 0}

	var count: int = int(config.get("count", 0))
	var hand_limit: int = int(config.get("hand_limit", 7))
	if count <= 0 or player_hand._held_cards.size() >= hand_limit:
		return {"drawn_count": 0}

	_call_if_valid(config.get("disable_player_inputs"))

	var drawn_count: int = 0
	for i in range(count):
		if deck_pile._held_cards.size() == 0:
			if discard_pile._held_cards.size() > 0:
				await shuffle_cards(config)
			else:
				break

		if deck_pile._held_cards.size() <= 0:
			continue

		var top_card: Card = deck_pile._held_cards.back()
		player_hand.move_cards([top_card])
		drawn_count += 1
		_emit_card_drawn(config.get("signal_bus"), top_card)

		await _wait_process_frame(config.get("tree"))
		_call_if_valid(config.get("update_counts_and_ui"))
		await _wait_timer(config.get("tree"), float(config.get("draw_interval", 0.1)))

	_call_if_valid(config.get("enable_player_inputs"))
	return {"drawn_count": drawn_count}


func shuffle_cards(config: Dictionary) -> Dictionary:
	var deck_pile: Pile = config.get("deck_pile") as Pile
	var discard_pile: Pile = config.get("discard_pile") as Pile
	if not is_instance_valid(deck_pile) or not is_instance_valid(discard_pile):
		return {"shuffled": false, "card_count": 0}
	if discard_pile._held_cards.is_empty():
		return {"shuffled": false, "card_count": 0}

	var cards_to_move: Array = discard_pile._held_cards.duplicate()
	deck_pile.move_cards(cards_to_move)
	deck_pile._held_cards.shuffle()
	_emit_deck_shuffled(config.get("signal_bus"))

	deck_pile.card_face_up = false
	await _wait_timer(config.get("tree"), float(config.get("shuffle_delay", 0.6)))
	deck_pile.update_card_ui()
	_call_if_valid(config.get("update_counts_and_ui"))

	return {
		"shuffled": true,
		"card_count": cards_to_move.size(),
	}


func _emit_card_drawn(signal_bus: Variant, card: Card) -> void:
	if signal_bus and signal_bus.has_method("emit_card_drawn"):
		signal_bus.emit_card_drawn(card, 1)


func _emit_deck_shuffled(signal_bus: Variant) -> void:
	if signal_bus and signal_bus.has_method("emit_deck_shuffled"):
		signal_bus.emit_deck_shuffled()


func _wait_process_frame(tree: Variant) -> void:
	if is_instance_valid(tree):
		await tree.process_frame


func _wait_timer(tree: Variant, delay: float) -> void:
	if is_instance_valid(tree):
		await tree.create_timer(delay).timeout


func _call_if_valid(callback: Variant) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.call()
