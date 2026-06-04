class_name InScenePayloadBridge
extends RefCounted


## InScenePayloadBridge 负责把 payload 转交给目标场景或 HexMap。
## 它不解析 payload 内容，也不负责创建或销毁场景节点。


func apply_to_new_scene_before_tree(next_scene: Node, payload: Variant) -> void:
	if payload == null:
		return
	if next_scene.has_method("apply_external_event"):
		next_scene.apply_external_event(payload)


func forward_to_hex_map(hex_map: Variant, payload: Variant, scene_log: Variant) -> void:
	if not is_instance_valid(hex_map):
		if scene_log:
			scene_log.error_event("InSceneMain", "hex_map missing while applying payload")
		return
	if payload == null:
		return

	var payload_text: String = str(payload).strip_edges()
	if payload_text == "":
		return

	if scene_log:
		scene_log.scene_event("InSceneMain", "forward payload to hex_map", {"payload": payload_text})
	hex_map.apply_external_event(payload_text)
