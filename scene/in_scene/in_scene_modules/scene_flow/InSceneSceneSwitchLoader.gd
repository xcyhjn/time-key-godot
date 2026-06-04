class_name InSceneSceneSwitchLoader
extends RefCounted


## InSceneSceneSwitchLoader 只负责切场景前的 PackedScene 加载、失败恢复和错误记录。
## 它不实例化新场景、不改 current_scene，也不释放旧场景。


func load_packed_scene(path: String, fail_message: String, scene_log: Variant) -> PackedScene:
	if path.strip_edges() == "":
		log_error(fail_message + "：场景路径为空", {"path": path}, scene_log)
		return null

	var packed_scene: PackedScene = ResourceLoader.load(path, "PackedScene") as PackedScene
	if packed_scene == null:
		log_error(fail_message + "：无法加载 PackedScene", {
			"path": path,
			"resource_exists": ResourceLoader.exists(path, "PackedScene"),
		}, scene_log)
		return null

	return packed_scene


func recover_dim_after_failed_switch(dim: Variant) -> void:
	if is_instance_valid(dim) and dim.has_method("use"):
		dim.use(1, 1)


func log_error(message: String, extra: Dictionary, scene_log: Variant) -> void:
	if scene_log:
		scene_log.error_event("InSceneMain", message, extra)
	push_error("[InSceneMain] %s %s" % [message, str(extra)])
