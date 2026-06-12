extends RefCounted


## EnemyIntentPresentationReferenceBridge 只负责为敌人意图表现协调器查找跨系统节点，并按旧入口顺序连接 hover 与重判信号。
## 它不判断交互阶段，不解析敌人意图，不驱动地图或时间轴表现，也不创建、写入或定位 tooltip。


func find_main_board(owner: Node) -> Control:
	if not is_instance_valid(owner):
		return null

	var tree := owner.get_tree()
	if tree == null:
		return null

	return tree.get_first_node_in_group("MainBoard") as Control


func collect_references(owner: Node, main_board: Control) -> Dictionary:
	var references := {
		"hex_map": null,
		"timeline_manager": null,
		"timeline_ui": null,
		"drag_shape_controller": null,
	}

	if not is_instance_valid(owner) or not is_instance_valid(main_board):
		return references

	references["hex_map"] = main_board.get_node_or_null("../../map/HexMap")
	references["timeline_manager"] = main_board.get_node_or_null("../TimelineSystem/TimelineManager")
	references["timeline_ui"] = main_board.get_node_or_null("../TimelineUI")

	var tree := owner.get_tree()
	if tree != null:
		references["drag_shape_controller"] = tree.get_first_node_in_group("DragShapeController")

	return references


func connect_reference_signals(
	timeline_manager: TimelineManager,
	hex_map: battle,
	timeline_hover_callable: Callable,
	revalidate_callable: Callable
) -> void:
	if is_instance_valid(timeline_manager):
		if not timeline_manager.action_hovered_changed.is_connected(timeline_hover_callable):
			timeline_manager.action_hovered_changed.connect(timeline_hover_callable)

	if is_instance_valid(hex_map) and hex_map.has_signal("enemy_roster_changed"):
		if not hex_map.enemy_roster_changed.is_connected(revalidate_callable):
			hex_map.enemy_roster_changed.connect(revalidate_callable)

	if is_instance_valid(hex_map) and hex_map.has_signal("tile_topology_changed"):
		if not hex_map.tile_topology_changed.is_connected(revalidate_callable):
			hex_map.tile_topology_changed.connect(revalidate_callable)
