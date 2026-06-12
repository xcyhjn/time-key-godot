extends RefCounted

## TimelineEnemyIntentTargetResolver 负责把敌方意图声明的目标中心坐标映射为 HexMap.stack_nodes 里的地块节点。
## 它不选择目标，不创建 TimelineAction，不修改 HexMap.stack_nodes，也不处理地图或时间轴表现。


func resolve_target_tile(enemy: Node, hex_map: battle) -> Node:
	if not is_instance_valid(enemy) or not is_instance_valid(hex_map):
		return null
	if not enemy.has_method("get_intent_target_center_coord"):
		return null

	var target_coord: Variant = enemy.get_intent_target_center_coord(hex_map)
	if target_coord != null and hex_map.stack_nodes.has(target_coord):
		return hex_map.stack_nodes[target_coord]
	return null
