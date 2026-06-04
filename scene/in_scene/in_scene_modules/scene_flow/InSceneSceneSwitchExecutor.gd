class_name InSceneSceneSwitchExecutor
extends RefCounted


## InSceneSceneSwitchExecutor 只负责执行已经加载好的场景切换。
## PackedScene 加载、payload 解析和失败恢复都由外层模块处理。


func switch_to_scene(config: Dictionary) -> void:
	var tree: SceneTree = config.get("tree") as SceneTree
	var packed_scene: PackedScene = config.get("packed_scene") as PackedScene
	if not is_instance_valid(tree) or packed_scene == null:
		return

	var path: String = str(config.get("path", ""))
	var payload: Variant = config.get("payload")
	var scene_log: Variant = config.get("scene_log")
	var payload_bridge: Variant = config.get("payload_bridge")

	if scene_log:
		scene_log.scene_event("InSceneMain", "switch scene start", {"path": path, "payload": payload})

	var next_scene: Node = packed_scene.instantiate()
	if is_instance_valid(payload_bridge) and payload_bridge.has_method("apply_to_new_scene_before_tree"):
		payload_bridge.apply_to_new_scene_before_tree(next_scene, payload)

	var old_scene: Node = tree.current_scene
	tree.root.add_child(next_scene)
	tree.current_scene = next_scene

	if scene_log:
		scene_log.scene_event("InSceneMain", "switch scene success", {"path": path})

	if is_instance_valid(old_scene):
		old_scene.queue_free()
