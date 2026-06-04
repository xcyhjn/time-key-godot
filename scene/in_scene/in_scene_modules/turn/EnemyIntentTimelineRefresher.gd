class_name EnemyIntentTimelineRefresher
extends RefCounted


## EnemyIntentTimelineRefresher 只负责把当前敌人组刷新到时间轴上。
## 它不推进回合、不抽牌，也不处理敌人意图的 hover 表现。


func refresh(config: Dictionary) -> Dictionary:
	var timeline_manager: Variant = config.get("timeline_manager")
	var tree: SceneTree = config.get("tree") as SceneTree
	if not is_instance_valid(timeline_manager) or not is_instance_valid(tree):
		return {"refreshed": false, "enemy_count": 0}

	timeline_manager.clear_grid()
	var all_enemies: Array = tree.get_nodes_in_group("Enemies")
	if all_enemies.is_empty():
		return {"refreshed": false, "enemy_count": 0}

	timeline_manager.generate_enemy_intents(all_enemies)
	timeline_manager.debug_print_grid()

	return {
		"refreshed": true,
		"enemy_count": all_enemies.size(),
	}
