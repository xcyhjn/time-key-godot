extends RefCounted

## TimelineEnemyIntentPrioritySelector 负责敌方意图候选的优先级读取、优先级分层排序和同级可放置候选选择。
## 它不读取或修改 TimelineManager.grid，不创建 TimelineAction，不调用 place_action()，也不决定敌人意图目标地块。


func get_enemy_intent_priority(enemy: Node, default_priority: int) -> int:
	if not is_instance_valid(enemy):
		return default_priority
	if enemy.has_method("get_intent_priority"):
		return int(enemy.get_intent_priority())
	var property_value = enemy.get("intent_priority")
	if property_value != null:
		return int(property_value)
	return default_priority


func get_sorted_priority_values(candidates: Array[Dictionary]) -> Array[int]:
	var priority_lookup: Dictionary = {}
	for candidate in candidates:
		priority_lookup[int(candidate["priority"])] = true

	var priorities: Array[int] = []
	for priority in priority_lookup.keys():
		priorities.append(int(priority))

	priorities.sort()
	priorities.reverse()
	return priorities


func filter_candidates_by_priority(candidates: Array[Dictionary], priority: int) -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for candidate in candidates:
		if int(candidate["priority"]) == priority:
			result.append(candidate)
	return result


func pick_placeable_candidate(
	candidates: Array[Dictionary],
	find_available_spot: Callable,
	randomize_same_priority: bool
) -> Dictionary:
	var placeable: Array[Dictionary] = []

	for candidate in candidates:
		var shape: Array[Vector2i] = candidate["shape"]
		var spot: Vector2i = find_available_spot.call(shape)
		if spot == Vector2i(-1, -1):
			continue
		placeable.append({
			"candidate": candidate,
			"spot": spot
		})

	if placeable.is_empty():
		return {}

	if randomize_same_priority:
		return placeable.pick_random()
	return placeable[0]
