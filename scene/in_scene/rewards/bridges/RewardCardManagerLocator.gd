extends RefCounted


## RewardCardManagerLocator 只负责奖励页面自动查找 CardManager。
## 它不生成奖励卡牌，不修改牌组，也不处理奖励确认或退出流程。


func find_card_manager(owner: Node, card_manager_script: GDScript) -> Node:
	if not is_instance_valid(owner):
		return null

	var tree := owner.get_tree()
	if tree == null:
		return null

	var tree_root := tree.root
	var card_manager := _get_card_manager_from_meta(tree_root, card_manager_script)
	if card_manager != null:
		return card_manager

	card_manager = _get_card_manager_from_meta(tree.current_scene, card_manager_script)
	if card_manager != null:
		return card_manager

	var parent := owner.get_parent()
	while parent != null:
		if _is_card_manager(parent, card_manager_script):
			return parent
		parent = parent.get_parent()

	if tree_root != null:
		var card_manager_node := tree_root.find_child("CardManager", true, false)
		if _is_card_manager(card_manager_node, card_manager_script):
			return card_manager_node

	return null


func _get_card_manager_from_meta(node: Node, card_manager_script: GDScript) -> Node:
	if not is_instance_valid(node) or not node.has_meta("card_manager"):
		return null

	var card_manager = node.get_meta("card_manager")
	if _is_card_manager(card_manager, card_manager_script):
		return card_manager
	return null


func _is_card_manager(candidate: Variant, card_manager_script: GDScript) -> bool:
	if not is_instance_valid(candidate):
		return false
	if card_manager_script == null:
		return false
	if not candidate is Node:
		return false
	return candidate.get_script() == card_manager_script
