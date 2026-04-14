@tool
## Central orchestrator for the card framework system.
##
## CardManager coordinates all card-related operations including drag-and-drop,
## history management, and container registration. It serves as the root node
## for card game scenes and manages the lifecycle of cards and containers.
##
## Key Responsibilities:
## - Card factory management and initialization
## - Container registration and coordination
## - Drag-and-drop event handling and routing
## - History tracking for undo/redo operations
## - Debug mode and visual debugging support
##
## Setup Requirements:
## - Must be positioned ABOVE CardContainers in scene tree hierarchy
## - Requires card_factory_scene to be assigned in inspector
## - Configure card_size to match your card assets
##
## Flexible Layout Examples:
## [codeblock]
## # Traditional (Direct Children):
## CardManager
## ├── Hand (CardContainer)
## ├── Foundation (CardContainer)
## └── Deck (CardContainer)
##
## # Modern Flexible Layout:
## MainScene
## ├── CardManager
## ├── VBoxContainer
## │   ├── PlayerHand (CardContainer) ✅
## │   └── TableArea
## │       └── Foundation (CardContainer) ✅
## └── UI
##     └── DeckPile (CardContainer) ✅
## [/codeblock]
class_name CardManager
extends Control

# Constants
const CARD_ACCEPT_TYPE = "card"


## Default size for all cards in the game
@export var card_size := CardFrameworkSettings.LAYOUT_DEFAULT_CARD_SIZE
## Scene containing the card factory implementation
@export var card_factory_scene: PackedScene
## Enables visual debugging for drop zones and interactions
@export var debug_mode := false


# Core system components
var card_factory: CardFactory
var card_container_dict: Dictionary = {}
var history: Array[HistoryElement] = []

var current_selected_card: Node = null # 当前选中的卡牌
#修改部分到此为止
func _init() -> void:
	if Engine.is_editor_hint():
		return
	

func _ready() -> void:
	if not _pre_process_exported_variables():
		return

	if Engine.is_editor_hint():
		return

	# Register CardManager to scene root for flexible CardContainer discovery
	var scene_root = get_tree().root
	if scene_root:
		scene_root.set_meta("card_manager", self)
		if debug_mode:
			print("CardManager registered to scene root (using tree root): ", scene_root.name)

	card_factory.card_size = card_size
	card_factory.preload_card_data()


## Undoes the last card movement operation.
## Restores cards to their previous positions using stored history.
func undo() -> void:
	if history.is_empty():
		return
	
	var last = history.pop_back()
	if last.from != null:
		last.from.undo(last.cards, last.from_indices)


## Clears all history entries, preventing further undo operations.
func reset_history() -> void:
	history.clear()


func _exit_tree() -> void:
	# Unregister CardManager from scene root
	var scene_root = get_tree().current_scene
	if scene_root and scene_root.has_meta("card_manager"):
		scene_root.remove_meta("card_manager")
		if debug_mode:
			print("CardManager unregistered from scene root")
	

func _add_card_container(id: int, card_container: CardContainer) -> void:
	card_container_dict[id] = card_container
	card_container.debug_mode = debug_mode


func _delete_card_container(id: int) -> void:
	card_container_dict.erase(id)


# Handles dropped cards by finding suitable container
func _on_drag_dropped(cards: Array) -> void:
	if cards.is_empty():
		return
	
	# Store original mouse_filter states and temporarily disable input during drop processing
	var original_mouse_filters = {}
	for card in cards:
		original_mouse_filters[card] = card.mouse_filter
		card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		
	# Find first container that accepts the cards
	for key in card_container_dict.keys():
		var card_container = card_container_dict[key]
		var result = card_container.check_card_can_be_dropped(cards)
		if result:
			var index = card_container.get_partition_index()
			# Restore mouse_filter before move_cards (DraggableObject will manage it from here)
			for card in cards:
				card.mouse_filter = original_mouse_filters[card]
			card_container.move_cards(cards, index)
			return
	
	for card in cards:
		# Restore mouse_filter before return_card (DraggableObject will manage it from here)
		card.mouse_filter = original_mouse_filters[card]
		card.return_card()


func _add_history(to: CardContainer, cards: Array) -> void:
	var from = null
	var from_indices = []
	
	# Record indices FIRST, before any movement operations
	for i in range(cards.size()):
		var c = cards[i]
		var current = c.card_container
		if i == 0:
			from = current
		else:
			if from != current:
				push_error("All cards must be from the same container!")
				return
		
		# Record index immediately to avoid race conditions
		if from != null:
			var original_index = from._held_cards.find(c)
			if original_index == -1:
				push_error("Card not found in source container during history recording!")
				return
			from_indices.append(original_index)
	
	var history_element = HistoryElement.new()
	history_element.from = from
	history_element.to = to
	history_element.cards = cards
	history_element.from_indices = from_indices
	history.append(history_element)


func _is_valid_directory(path: String) -> bool:
	var dir = DirAccess.open(path)
	return dir != null


func _pre_process_exported_variables() -> bool:
	if card_factory_scene == null:
		push_error("CardFactory is not assigned! Please set it in the CardManager Inspector.")
		return false
	
	var factory_instance = card_factory_scene.instantiate() as CardFactory
	if factory_instance == null:
		push_error("Failed to create an instance of CardFactory! CardManager imported an incorrect card factory scene.")
		return false
	
	add_child(factory_instance)
	card_factory = factory_instance
	return true
	
	# ==========================================
# ★ 新增：在脚本底部添加选中与弃牌接口
# ==========================================
# 尝试选中一张牌（如果已有选中的牌则拒绝）
func select_card(card: Node) -> bool:
	if current_selected_card != null and current_selected_card != card:
		return false 
	current_selected_card = card
	return true

# 取消当前选中状态
func deselect_card() -> void:
	current_selected_card = null

# ==========================================
# ★ 终极安全方案：通过节点名字找牌堆
# ==========================================
func get_container_by_name(container_name: String) -> CardContainer:
	for id in card_container_dict:
		var container = card_container_dict[id]
		if container.name == container_name:
			return container
	return null

func play_and_discard(card: Node, target_container_name: String) -> void:
	var target_container = get_container_by_name(target_container_name)
	
	if target_container:
		# ★ 只需要传卡牌数组进去，插件会自动把它放在牌堆最上面
		target_container.move_cards([card])
	else:
		print("❌ 找不到名为 '", target_container_name, "' 的牌堆！卡牌将被安全销毁。")
		if card.get("card_container") != null:
			card.card_container.remove_card(card)
		card.hide() 
		card.call_deferred("queue_free")
# ==========================================
# ★ 新增：牌组 ID 管理系统对接接口
# ==========================================

# 1. 获取当前全局牌组（给删卡、选卡界面用）
func get_deck_card_ids() -> Array[String]:
	return GlobalDB.player_deck.duplicate()

# 2. 从全局牌组中永久删卡
func remove_card_from_deck(card_id: String) -> void:
	var idx = GlobalDB.player_deck.find(card_id)
	if idx != -1:
		GlobalDB.player_deck.remove_at(idx)
		print("已从全局牌组永久删除卡牌 ID: ", card_id)

# 3. 获得新卡，加入全局牌组
func add_card_to_deck(card_id: String) -> void:
	GlobalDB.player_deck.append(card_id)
	print("已将新卡牌加入全局牌组 ID: ", card_id)
