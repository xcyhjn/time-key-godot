extends RefCounted

## TimelineEnemyIntentCandidateCollector 负责从敌人列表收集本回合可进入时间轴的意图候选。
## 它不排序候选，不寻找时间轴放置位置，不创建 TimelineAction，不修改 grid，也不决定目标地块。


func collect_candidates(enemies_on_board: Array, hex_map: battle, get_priority: Callable) -> Array[Dictionary]:
	var candidates: Array[Dictionary] = []

	for enemy in enemies_on_board:
		var enemy_node = enemy
		if not is_instance_valid(enemy_node):
			continue
		if not enemy_node.has_method("get_intent_shape") or not enemy_node.has_method("get_intent_action"):
			continue

		# 只有显式启用“敌人意图展示系统”的单位，才参与新意图链路。
		if enemy_node.has_method("is_intent_preview_enabled") and not enemy_node.is_intent_preview_enabled():
			continue

		# 时间轴生成只接收当前回合有合法目标的意图。
		# 地图 hover 的灰态提示仍由 EnemyIntentPresentationController 处理。
		if is_instance_valid(hex_map) and enemy_node.has_method("can_generate_intent") and not enemy_node.can_generate_intent(hex_map):
			continue

		var shape: Array[Vector2i] = enemy_node.get_intent_shape()
		if shape.is_empty():
			continue

		candidates.append({
			"enemy": enemy_node,
			"shape": shape,
			"priority": int(get_priority.call(enemy_node))
		})

	return candidates
