extends RefCounted


## OutScenePayloadBridge 只负责在局外切入新场景前写入外部 payload。
## 它不加载场景，不挂树，不释放旧场景，也不解析 payload 内容。


func apply_before_scene_enters_tree(next_scene: Node, payload: String, property_checker: Callable) -> bool:
	if next_scene == null:
		return false

	var delivered := false

	var hex_map := next_scene.get_node_or_null("map/HexMap")
	if _apply_external_event(hex_map, payload):
		delivered = true

	var main_board := next_scene.get_node_or_null("ui/Main")
	if _apply_external_event(main_board, payload):
		delivered = true

	if not delivered and _apply_external_event(next_scene, payload):
		delivered = true

	if not delivered:
		var target_node := next_scene.get_node_or_null("Main/Node2D")
		if _can_write_received_text(target_node, property_checker):
			target_node.received_text = payload
			delivered = true

	return delivered


func _apply_external_event(target: Variant, payload: String) -> bool:
	if not is_instance_valid(target):
		return false
	if not target.has_method("apply_external_event"):
		return false

	target.apply_external_event(payload)
	return true


func _can_write_received_text(target: Variant, property_checker: Callable) -> bool:
	if not is_instance_valid(target):
		return false
	if not property_checker.is_valid():
		return false
	return bool(property_checker.call(target, &"received_text"))
