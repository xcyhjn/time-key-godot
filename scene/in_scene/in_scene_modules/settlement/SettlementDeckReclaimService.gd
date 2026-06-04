class_name SettlementDeckReclaimService
extends RefCounted


## SettlementDeckReclaimService 负责把战斗中的运行时卡牌无动画回收到抽牌堆。
## 它不处理胜负触发、不打开奖励页，也不刷新按钮；这些仍由 InScene 主控决定。

const SETTLEMENT_RUNTIME_CARD_COLLECTOR := preload("res://scene/in_scene/in_scene_modules/settlement/SettlementRuntimeCardCollector.gd")

var _card_collector: SettlementRuntimeCardCollector = SETTLEMENT_RUNTIME_CARD_COLLECTOR.new()


func reclaim(config: Dictionary) -> Dictionary:
	var deck_pile: Pile = config.get("deck_pile") as Pile
	if not is_instance_valid(deck_pile):
		return {
			"reclaimed": false,
			"card_count": 0,
		}

	var manager_instance: Variant = config.get("manager_instance")
	if is_instance_valid(manager_instance) and manager_instance.has_method("deselect_card"):
		manager_instance.deselect_card()

	_reset_drag_controller_for_settlement(config.get("drag_controller"))

	var cards: Array[Card] = _card_collector.collect(config)
	_move_cards_to_deck_without_animation(cards, deck_pile)

	deck_pile._held_cards.shuffle()
	deck_pile.card_face_up = false
	_sync_deck_cards_after_silent_reclaim(deck_pile)

	return {
		"reclaimed": true,
		"card_count": cards.size(),
	}


func _move_cards_to_deck_without_animation(cards: Array[Card], deck_pile: Pile) -> void:
	for card: Card in cards:
		if is_instance_valid(card):
			_prepare_card_for_silent_deck_reclaim(card)
			_silent_add_card_to_deck(card, deck_pile)


func _silent_add_card_to_deck(card: Card, deck_pile: Pile) -> void:
	if not is_instance_valid(card) or not is_instance_valid(deck_pile):
		return

	var previous_container: CardContainer = card.card_container
	if is_instance_valid(previous_container):
		previous_container._held_cards.erase(card)

	var parent: Node = card.get_parent()
	if parent:
		parent.remove_child(card)

	var cards_node: Node = deck_pile.cards_node if is_instance_valid(deck_pile.cards_node) else deck_pile.get_node_or_null("Cards")
	if is_instance_valid(cards_node):
		cards_node.add_child(card)
	else:
		deck_pile.add_child(card)

	card.card_container = deck_pile
	if not deck_pile._held_cards.has(card):
		deck_pile._held_cards.append(card)
	card.global_position = deck_pile.global_position


func _prepare_card_for_silent_deck_reclaim(card: Card) -> void:
	if not is_instance_valid(card):
		return

	if card.has_method("_restore_normal_visuals"):
		card.call("_restore_normal_visuals")
	if _has_runtime_property(card, &"card_current_state"):
		card.set("card_current_state", 0)

	card.set_as_top_level(false)
	card.mouse_filter = Control.MOUSE_FILTER_IGNORE
	card.can_be_interacted_with = false
	card.modulate = Color.WHITE
	card.rotation = 0.0
	card.scale = Vector2.ONE
	card.show()


func _sync_deck_cards_after_silent_reclaim(deck_pile: Pile) -> void:
	if not is_instance_valid(deck_pile):
		return

	var cards_node: Node = deck_pile.cards_node if is_instance_valid(deck_pile.cards_node) else deck_pile.get_node_or_null("Cards")
	for i in range(deck_pile._held_cards.size()):
		var card: Card = deck_pile._held_cards[i]
		if not is_instance_valid(card):
			continue
		card.show_front = false
		card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		card.can_be_interacted_with = false
		if is_instance_valid(cards_node) and card.get_parent() == cards_node:
			cards_node.move_child(card, i)


func _reset_drag_controller_for_settlement(drag_controller: Variant) -> void:
	if not is_instance_valid(drag_controller):
		return

	if _has_runtime_property(drag_controller, &"is_dragging"):
		drag_controller.set("is_dragging", false)
	if _has_runtime_property(drag_controller, &"is_placing"):
		drag_controller.set("is_placing", false)
	if _has_runtime_property(drag_controller, &"current_card"):
		drag_controller.set("current_card", null)
	if _has_runtime_property(drag_controller, &"current_target_tile"):
		drag_controller.set("current_target_tile", null)


func _has_runtime_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
