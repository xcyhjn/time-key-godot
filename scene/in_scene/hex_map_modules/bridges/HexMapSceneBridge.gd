class_name HexMapSceneBridge
extends RefCounted


## HexMapSceneBridge 集中维护 HexMap 依赖的局内场景节点路径。
## 它只做查找，不缓存玩法状态，也不修改节点；
## 这样后续调整场景树时，可以优先改这里，而不是在 HexMap 主控里到处搜索硬路径。

## 查找高度视图切换按钮。
## 保留旧顺序：先从 HexMap 相对路径找，再从 current_scene 的 ui 下找。
func find_height_view_button(hex_map: Node) -> Node:
	return _find_from_hex_or_scene(
		hex_map,
		"../../ui/HeightViewToggleButton",
		"ui/HeightViewToggleButton"
	)


## 查找敌人意图管理器。
## 当前只迁移旧相对路径，不额外扩大兜底，避免改变 TimelineSystem 缺失时的表现。
func find_enemy_intent_manager(hex_map: Node) -> Node:
	return _find_from_hex(hex_map, "../../ui/TimelineSystem/EnemyIntentManager")


## 查找局内地图相机。
## 保留旧顺序：先从 HexMap 旁边找 `../Camera2D`，再从 current_scene 的 `map/Camera2D` 找。
func find_camera(hex_map: Node) -> Node:
	return _find_from_hex_or_scene(
		hex_map,
		"../Camera2D",
		"map/Camera2D"
	)


## 查找 UI 总敌方血量条。
## 保留旧顺序：先从 HexMap 相对路径找，再从 current_scene 的 ui 下找。
func find_total_enemy_health_bar(hex_map: Node) -> Node:
	return _find_from_hex_or_scene(
		hex_map,
		"../../ui/TotalEnemyHealthBar",
		"ui/TotalEnemyHealthBar"
	)


## 查找 HexMap 子节点 BarManager。
## 单体血条仍属于地图内部渲染辅助节点，本函数只集中节点名。
func find_bar_manager(hex_map: Node) -> Node:
	return _find_from_hex(hex_map, "BarManager")


## 按旧命名规则查找某个 occupant 对应的单体血条。
## 命名规则保持 `HealthBar_<instance_id>`，避免影响高度视图、升降和外部渲染节点注册。
func find_health_bar_for_occupant(hex_map: Node, occupant: Variant) -> Node:
	if not is_instance_valid(occupant):
		return null

	var bar_manager := find_bar_manager(hex_map)
	if not is_instance_valid(bar_manager):
		return null

	return bar_manager.get_node_or_null("HealthBar_" + str(occupant.get_instance_id()))


## 从 HexMap 自身按相对路径查找节点。
## 调用方传入无效 HexMap 时直接返回 null，保持旧兜底不会抛错。
func _find_from_hex(hex_map: Node, path: String) -> Node:
	if not is_instance_valid(hex_map):
		return null
	return hex_map.get_node_or_null(path)


## 先从 HexMap 相对路径查找，再从 current_scene 查找。
## 这个 helper 只用于原本已经有 current_scene 兜底的节点，避免 bridge 悄悄改变依赖边界。
func _find_from_hex_or_scene(hex_map: Node, hex_path: String, scene_path: String) -> Node:
	var node := _find_from_hex(hex_map, hex_path)
	if is_instance_valid(node):
		return node
	return _find_from_current_scene(hex_map, scene_path)


## 从当前场景根节点查找节点。
## current_scene 可能为空或已切换，所以每次查找都即时读取，不在 bridge 内缓存。
func _find_from_current_scene(hex_map: Node, path: String) -> Node:
	if not is_instance_valid(hex_map):
		return null

	var tree := hex_map.get_tree()
	if tree == null or not is_instance_valid(tree.current_scene):
		return null

	return tree.current_scene.get_node_or_null(path)
