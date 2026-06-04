class_name CardManagerLocator
extends RefCounted


## CardManagerLocator 统一处理局内 CardManager 的运行时查找。
## CardManager 由 `ui/Main` 在运行时创建；HexMap、奖励和 UI 都需要读取它。
## 本模块先迁移 HexMap 的旧查找顺序，避免把跨场景兜底散落在地图主控里。

## 按固定顺序查找当前有效的 CardManager。
## 顺序必须保持旧行为：先读 MainBoard.manager_instance，再读 root meta，最后读 current_scene meta。
func find(tree: SceneTree) -> Node:
	if tree == null:
		return null

	var manager_from_main := _find_from_main_board(tree)
	if is_instance_valid(manager_from_main):
		return manager_from_main

	var manager_from_root := _get_valid_card_manager_from_meta(tree.root)
	if is_instance_valid(manager_from_root):
		return manager_from_root

	var manager_from_scene := _get_valid_card_manager_from_meta(tree.current_scene)
	if is_instance_valid(manager_from_scene):
		return manager_from_scene

	return null


## 从 MainBoard group 中读取运行时创建的 manager_instance。
## 这是当前局内正常路径，优先级最高。
func _find_from_main_board(tree: SceneTree) -> Node:
	var main_board := tree.get_first_node_in_group("MainBoard")
	if not is_instance_valid(main_board):
		return null
	var manager_from_main = main_board.get("manager_instance")
	if is_instance_valid(manager_from_main):
		return manager_from_main
	return null


## 安全读取节点上缓存的 CardManager。
## 第二次从局外进入局内时，root 或 current_scene 可能残留上一场已释放的 CardManager；
## 遇到这种无效引用时主动移除 metadata，保持旧防护逻辑。
func _get_valid_card_manager_from_meta(owner_node: Node) -> Node:
	if not is_instance_valid(owner_node):
		return null
	if not owner_node.has_meta("card_manager"):
		return null

	var cached_manager = owner_node.get_meta("card_manager")
	if is_instance_valid(cached_manager):
		return cached_manager

	owner_node.remove_meta("card_manager")
	return null
