extends RefCounted

## ShopGlobalNodeFinder 只负责查找商店依赖的全局节点。
## 它不读取时代值，不消费时间币，也不处理购买、刷新或升级流程。


func find_global_clock(owner: Node) -> Node:
	return find_global_node(owner, "GlobalClock", "global_clock", "global_clock.gd")


func find_global_timecoin(owner: Node) -> Node:
	return find_global_node(owner, "GlobalTimecoin", "global_timecoin", "global_timecoin.gd")


func find_global_node(owner: Node, autoload_name: String, fallback_name: String, script_name: String) -> Node:
	if not is_instance_valid(owner):
		return null
	if owner.has_node("/root/%s" % autoload_name):
		return owner.get_node("/root/%s" % autoload_name)

	var tree = owner.get_tree()
	if tree == null or tree.root == null:
		return null

	var scene_root = tree.root
	var found = find_node_with_script_recursive(scene_root, script_name)
	if found:
		return found

	var by_name = scene_root.find_child(autoload_name, true, false)
	if by_name:
		return by_name
	return scene_root.find_child(fallback_name, true, false)


func find_node_with_script_recursive(root: Node, script_name: String) -> Node:
	if not is_instance_valid(root):
		return null

	var script = root.get_script()
	if script and script_name in script.resource_path:
		return root

	for child in root.get_children():
		var found = find_node_with_script_recursive(child, script_name)
		if found:
			return found

	return null
