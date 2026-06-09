extends RefCounted


## CustomCardNodeBridge 只负责 CustomCard 需要的跨节点查找。
## 它不负责判断卡牌状态、不修改手牌或地图数据，也不处理选中、拖拽或出牌流程。


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


func find_player_hand() -> Node:
	var main: Node = get_main_board()
	if not is_instance_valid(main):
		return null

	var hand: Variant = main.get("player_hand")
	return hand as Node


func get_player_hand_container(card_container: Variant) -> Node:
	if card_container is Hand:
		return card_container as Hand

	return find_player_hand()


func find_drag_shape_controller() -> Node:
	if not is_instance_valid(_owner):
		return null

	var tree: SceneTree = _owner.get_tree()
	if tree == null:
		return null

	return tree.get_first_node_in_group("DragShapeController")
