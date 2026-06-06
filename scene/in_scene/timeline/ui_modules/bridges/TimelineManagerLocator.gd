extends RefCounted

## TimelineManagerLocator 只负责为 TimelineUI 查找 TimelineManager 节点。
## 它不读取时间轴数据，不连接信号，也不创建或移除行动块。


func find_timeline_manager(owner: Node) -> TimelineManager:
	if not is_instance_valid(owner):
		return null

	var tree = owner.get_tree()
	if tree:
		var managers = tree.get_nodes_in_group("TimelineManager")
		if not managers.is_empty():
			return managers[0] as TimelineManager

		var found = tree.root.find_child("TimelineManager", true, false)
		if found:
			return found as TimelineManager

	var parent = owner.get_parent()
	while parent:
		if parent is TimelineManager:
			return parent as TimelineManager
		parent = parent.get_parent()

	if tree:
		for node in tree.get_nodes_in_group("managers"):
			if node is TimelineManager:
				return node as TimelineManager

	return null
