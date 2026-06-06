extends RefCounted


## DragShapeNodeBridge 只负责 DragShapeController 需要的跨节点查找。
## 它不缓存拖拽状态、不修改场景树，也不决定卡牌是否可以放置。


var _owner: Node = null


func _init(owner: Node) -> void:
	_owner = owner


func get_main_board() -> Node:
	if not is_instance_valid(_owner):
		return null
	var tree: SceneTree = _owner.get_tree()
	if tree == null:
		return null
	return tree.get_first_node_in_group("MainBoard")


func get_card_manager() -> Node:
	var main: Node = get_main_board()
	if not is_instance_valid(main):
		return null
	var manager: Variant = main.get("manager_instance")
	return manager as Node


func find_discard_pile() -> Node:
	var main: Node = get_main_board()
	if not is_instance_valid(main):
		return null
	var pile: Variant = main.get("discard_pile")
	return pile as Node


func find_player_hand() -> Node:
	var main: Node = get_main_board()
	if not is_instance_valid(main):
		return null
	var hand: Variant = main.get("player_hand")
	return hand as Node


func get_hex_map() -> Node:
	var main: Node = get_main_board()
	if not is_instance_valid(main):
		return null
	return main.get_node_or_null("../../map/HexMap")


func find_project_node() -> Node:
	return get_main_board()
