extends RefCounted


## TimecoinGlobalBridge 只负责为 TimecoinUI 查找 GlobalTimecoin 节点。
## 它不负责连接信号、不读取或修改时间币数值，也不刷新 UI 或播放动画。


func find_timecoin_singleton(owner: Node) -> Node:
	if not is_instance_valid(owner):
		return null

	if owner.has_node("/root/GlobalTimecoin"):
		return owner.get_node("/root/GlobalTimecoin")

	var parent_node: Node = owner.get_parent()
	if is_instance_valid(parent_node):
		var grandparent: Node = parent_node.get_parent()
		if _is_node_script_match(grandparent, "global_timecoin.gd"):
			return grandparent

	var tree: SceneTree = owner.get_tree()
	if tree == null or tree.root == null:
		return null

	for child in tree.root.get_children():
		if _is_node_script_match(child, "global_timecoin.gd"):
			return child

	var scene_root: Node = tree.current_scene
	if is_instance_valid(scene_root):
		return find_node_with_script(scene_root, "global_timecoin.gd")

	return null


func find_node_with_script(node: Node, script_path: String) -> Node:
	if _is_node_script_match(node, script_path):
		return node

	if not is_instance_valid(node):
		return null

	for child in node.get_children():
		var result: Node = find_node_with_script(child, script_path)
		if result:
			return result

	return null


func _is_node_script_match(node: Node, script_path: String) -> bool:
	if not is_instance_valid(node):
		return false

	var script: Script = node.get_script()
	return script != null and script_path in script.resource_path
