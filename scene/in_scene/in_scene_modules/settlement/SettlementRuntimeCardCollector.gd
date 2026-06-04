class_name SettlementRuntimeCardCollector
extends RefCounted


## SettlementRuntimeCardCollector 只负责收集结算期需要回收到抽牌堆的运行时卡牌。
## 它不移动节点、不洗牌、不改卡牌视觉，避免收集逻辑和实际回收动作互相缠住。


func collect(config: Dictionary) -> Array[Card]:
	var cards: Array[Card] = []
	var seen_cards: Dictionary = {}
	var deck_pile: Variant = config.get("deck_pile")

	for container: CardContainer in _get_runtime_card_containers(config):
		if container == deck_pile:
			continue
		for card in container._held_cards.duplicate():
			_append_reclaim_card(cards, seen_cards, card)

	var search_root: Node = config.get("search_root") as Node
	for card: Card in _collect_loose_runtime_cards(search_root, seen_cards):
		_append_reclaim_card(cards, seen_cards, card)
	return cards


func _get_runtime_card_containers(config: Dictionary) -> Array[CardContainer]:
	var containers: Array[CardContainer] = []
	_append_runtime_container(containers, config.get("player_hand"))
	_append_runtime_container(containers, config.get("discard_pile"))
	_append_runtime_container(containers, config.get("deck_pile"))

	var manager_instance: Variant = config.get("manager_instance")
	if is_instance_valid(manager_instance) and manager_instance is CardManager:
		var card_manager: CardManager = manager_instance as CardManager
		for container in card_manager.card_container_dict.values():
			_append_runtime_container(containers, container)
	return containers


func _append_runtime_container(containers: Array[CardContainer], candidate: Variant) -> void:
	if is_instance_valid(candidate) and candidate is CardContainer and not containers.has(candidate):
		containers.append(candidate)


func _append_reclaim_card(cards: Array[Card], seen_cards: Dictionary, candidate: Variant) -> void:
	if is_instance_valid(candidate) and candidate is Card and not seen_cards.has(candidate):
		cards.append(candidate)
		seen_cards[candidate] = true


func _collect_loose_runtime_cards(search_root: Node, seen_cards: Dictionary) -> Array[Card]:
	var loose_cards: Array[Card] = []
	if not is_instance_valid(search_root):
		return loose_cards

	for candidate in search_root.find_children("*", "Card", true, false):
		if is_instance_valid(candidate) and candidate is Card and not seen_cards.has(candidate):
			var card: Card = candidate as Card
			loose_cards.append(card)
			seen_cards[card] = true
	return loose_cards
