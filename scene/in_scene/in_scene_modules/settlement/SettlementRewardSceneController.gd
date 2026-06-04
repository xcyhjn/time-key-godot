class_name SettlementRewardSceneController
extends RefCounted


## SettlementRewardSceneController 只负责打开结算奖励页。
## 它不消费奖励、不处理奖励页退出，也不改变战斗阶段。


func open_reward_scene(config: Dictionary) -> Dictionary:
	var reward_type: String = str(config.get("reward_type", ""))
	var reward_scene_paths: Dictionary = config.get("reward_scene_paths", {})
	if not reward_scene_paths.has(reward_type):
		return {
			"opened": false,
			"reason": "unknown_reward_type",
			"reward_type": reward_type,
		}

	var scene_path: String = str(reward_scene_paths[reward_type])
	var packed_scene: PackedScene = get_reward_scene(scene_path, config.get("scene_cache", {}))
	if packed_scene == null:
		return {
			"opened": false,
			"reason": "scene_load_failed",
			"scene_path": scene_path,
		}

	var owner: Node = config.get("owner") as Node
	if not is_instance_valid(owner):
		return {
			"opened": false,
			"reason": "owner_invalid",
			"scene_path": scene_path,
		}

	var reward_context: Dictionary = config.get("reward_context", {})
	var reward_instance: Node = packed_scene.instantiate()
	owner.add_child(reward_instance)
	reward_instance.show()

	var connect_close_callback: Callable = config.get("connect_close_callback", Callable())
	_connect_close_signal(reward_instance, connect_close_callback)
	_apply_reward_context_meta(reward_instance, reward_type, reward_context)
	_apply_deck_manager(reward_instance, config.get("manager_instance"))
	_open_reward_instance(reward_instance)

	return {
		"opened": true,
		"reward_instance": reward_instance,
		"scene_path": scene_path,
	}


func get_reward_scene(scene_path: String, scene_cache: Dictionary) -> PackedScene:
	if scene_cache.has(scene_path):
		return scene_cache[scene_path] as PackedScene

	var packed_scene: PackedScene = ResourceLoader.load(scene_path, "PackedScene") as PackedScene
	if packed_scene != null:
		scene_cache[scene_path] = packed_scene
	return packed_scene


func _connect_close_signal(reward_instance: Node, close_callback: Callable) -> void:
	if not is_instance_valid(reward_instance) or not close_callback.is_valid():
		return
	if not reward_instance.has_signal("reward_scene_close_requested"):
		return

	if reward_instance.is_connected("reward_scene_close_requested", close_callback):
		reward_instance.disconnect("reward_scene_close_requested", close_callback)
	reward_instance.connect("reward_scene_close_requested", close_callback)


func _apply_reward_context_meta(reward_instance: Node, reward_type: String, reward_context: Dictionary) -> void:
	if reward_context.is_empty():
		return

	reward_instance.set_meta("settlement_reward_context", reward_context)
	# 商店只有退出按钮，按下退出即视为已经使用该建筑。
	if reward_type == "shop":
		reward_instance.set_meta("settlement_reward_consume_on_exit", true)
	else:
		reward_instance.set_meta("settlement_reward_committed", false)


func _apply_deck_manager(reward_instance: Node, manager_instance: Variant) -> void:
	if reward_instance.has_method("set_deck_manager") and is_instance_valid(manager_instance):
		reward_instance.set_deck_manager(manager_instance)


func _open_reward_instance(reward_instance: Node) -> void:
	if reward_instance.has_method("open_shop"):
		reward_instance.open_shop()
	elif reward_instance.has_method("open"):
		reward_instance.open()
	else:
		reward_instance.show()
