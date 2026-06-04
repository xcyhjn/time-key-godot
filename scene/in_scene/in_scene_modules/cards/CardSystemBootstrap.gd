class_name CardSystemBootstrap
extends RefCounted


## CardSystemBootstrap 只负责局内卡牌系统的创建和基础信号连接。
## 它不抽牌、不洗牌、不处理弃牌效果；这些运行时生命周期仍由 InScene 主控保留。


## 创建 CardManager、手牌区、抽牌堆、弃牌堆，并连接本批稳定的 UI 按钮。
## 返回运行时节点引用，调用方继续维护旧变量，避免影响其他系统读取 MainBoard。
func setup(config: Dictionary) -> Dictionary:
	var owner: Node = config.get("owner") as Node
	if not is_instance_valid(owner):
		return {"ready": false}

	var viewport_rect: Rect2 = owner.get_viewport().get_visible_rect()
	var screen_size: Vector2 = viewport_rect.size
	var manager_instance: CardManager = _create_card_manager(owner, config)
	if not is_instance_valid(manager_instance):
		return {"ready": false}

	await owner.get_tree().process_frame
	_configure_card_factory(manager_instance, config)

	var player_hand: Hand = _create_player_hand(owner, config, screen_size)
	var deck_pile: Pile = _create_deck_pile(owner, config)
	var discard_pile: Pile = _create_discard_pile(owner, config)

	_create_initial_deck(manager_instance, deck_pile)
	_connect_card_ui_buttons(config)

	return {
		"ready": true,
		"manager_instance": manager_instance,
		"player_hand": player_hand,
		"deck_pile": deck_pile,
		"discard_pile": discard_pile,
	}


func _create_card_manager(owner: Node, config: Dictionary) -> CardManager:
	var manager_scene: PackedScene = config.get("manager_scene") as PackedScene
	if not is_instance_valid(manager_scene):
		push_error("project.gd: 无法加载 card_manager.tscn！")
		return null

	var manager_instance: CardManager = manager_scene.instantiate() as CardManager
	if not is_instance_valid(manager_instance):
		push_error("project.gd: 无法实例化 CardManager！")
		return null

	var card_factory_scene: PackedScene = config.get("card_factory_scene") as PackedScene
	if is_instance_valid(card_factory_scene):
		manager_instance.card_factory_scene = card_factory_scene
	else:
		push_warning("project.gd: card_factory.tscn 加载失败！")

	manager_instance.debug_mode = true
	owner.add_child(manager_instance)
	return manager_instance


func _configure_card_factory(manager_instance: CardManager, config: Dictionary) -> void:
	var card_scene_ref: PackedScene = config.get("card_scene_ref") as PackedScene
	if is_instance_valid(manager_instance.card_factory) and is_instance_valid(card_scene_ref):
		manager_instance.card_factory.default_card_scene = card_scene_ref
	else:
		push_error("project.gd: card_factory 或 card_scene_ref 无效！")


func _create_player_hand(owner: Node, config: Dictionary, screen_size: Vector2) -> Hand:
	var hand_scene: PackedScene = config.get("hand_scene") as PackedScene
	if not is_instance_valid(hand_scene):
		push_error("project.gd: hand_scene 无效！")
		return null

	var player_hand: Hand = hand_scene.instantiate() as Hand
	if not is_instance_valid(player_hand):
		push_error("project.gd: 无法实例化 Hand！")
		return null

	var card_size: Vector2 = config.get("card_size", Vector2.ZERO)
	var hand_x_position_ratio: float = float(config.get("hand_x_position_ratio", 0.5))
	var hand_y_position_ratio: float = float(config.get("hand_y_position_ratio", 0.7))
	var hand_y_offset: float = float(config.get("hand_y_offset", 0.0))

	player_hand.name = "PlayerHand"
	owner.add_child(player_hand)
	player_hand.position = Vector2(
		screen_size.x * hand_x_position_ratio - card_size.x / 2.0,
		screen_size.y * hand_y_position_ratio + hand_y_offset
	)
	return player_hand


func _create_deck_pile(owner: Node, config: Dictionary) -> Pile:
	var pile_scene: PackedScene = config.get("pile_scene") as PackedScene
	if not is_instance_valid(pile_scene):
		push_error("project.gd: pile_scene 无效！")
		return null

	var deck_pile: Pile = pile_scene.instantiate() as Pile
	if not is_instance_valid(deck_pile):
		push_error("project.gd: 无法实例化 deck_pile！")
		return null

	deck_pile.name = "DeckPile"
	owner.add_child(deck_pile)
	deck_pile.position = Vector2(0, 400)
	deck_pile.visible = false
	return deck_pile


func _create_discard_pile(owner: Node, config: Dictionary) -> Pile:
	var pile_scene: PackedScene = config.get("pile_scene") as PackedScene
	if not is_instance_valid(pile_scene):
		return null

	var discard_pile: Pile = pile_scene.instantiate() as Pile
	if not is_instance_valid(discard_pile):
		push_error("project.gd: 无法实例化 discard_pile！")
		return null

	discard_pile.name = "DiscardPile"
	owner.add_child(discard_pile)
	discard_pile.add_to_group("DiscardPile")
	discard_pile.position = Vector2(2000, 400)
	discard_pile.visible = false
	return discard_pile


func _create_initial_deck(manager_instance: CardManager, deck_pile: Pile) -> void:
	if is_instance_valid(manager_instance.card_factory) and is_instance_valid(deck_pile):
		for card_id in GlobalDB.player_deck:
			manager_instance.card_factory.create_card(card_id, deck_pile)

		if deck_pile._held_cards.size() > 0:
			deck_pile._held_cards.shuffle()
	else:
		push_error("project.gd: 无法生成初始卡牌，card_factory 或 deck_pile 无效！")


func _connect_card_ui_buttons(config: Dictionary) -> void:
	var connect_once: Callable = config.get("connect_once", Callable())
	_connect_pressed_button(config.get("discard_button"), config.get("discard_pressed"), connect_once, false)
	_connect_gui_button(config.get("deck_button"), config.get("deck_gui_input"))
	_connect_pressed_button(config.get("end_turn_button"), config.get("end_turn_pressed"), connect_once, true)
	_connect_pressed_button(config.get("shop_button"), config.get("shop_pressed"), connect_once, true)
	_connect_pressed_button(config.get("acquire_reward_button"), config.get("acquire_reward_pressed"), connect_once, true)
	_connect_pressed_button(config.get("remove_reward_button"), config.get("remove_reward_pressed"), connect_once, true)
	_connect_pressed_button(config.get("craft_reward_button"), config.get("craft_reward_pressed"), connect_once, true)


func _connect_pressed_button(button: Variant, callback: Variant, connect_once: Callable, use_connect_once: bool) -> void:
	if not is_instance_valid(button) or not (callback is Callable):
		return

	var callable: Callable = callback
	if use_connect_once and connect_once.is_valid():
		connect_once.call(button.pressed, callable)
		return

	if button.pressed.is_connected(callable):
		button.pressed.disconnect(callable)
	button.pressed.connect(callable)


func _connect_gui_button(button: Variant, callback: Variant) -> void:
	if not is_instance_valid(button) or not (callback is Callable):
		return

	var callable: Callable = callback
	if button.gui_input.is_connected(callable):
		button.gui_input.disconnect(callable)
	button.gui_input.connect(callable)
