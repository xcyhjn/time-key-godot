class_name InSceneReturnFlowController
extends RefCounted


## InSceneReturnFlowController 只编排“局内结算后返回局外”的顺序。
## Payload 构造、实际切场和失败恢复仍由已有入口处理，避免同批扩大边界。


func return_to_out_scene(config: Dictionary) -> void:
	_call_if_valid(config.get("push_era_to_global"))

	var return_payload: Dictionary = _build_return_payload(config.get("build_return_payload"))
	_log_return_payload(config.get("scene_log"), return_payload)
	_store_pending_resolution(config.get("map_state"), return_payload)

	_call_if_valid(config.get("disable_player_inputs"))
	_call_if_valid(config.get("hide_ui_for_external_scene"))
	_prepare_return_audio(config.get("sound_manager"))
	await _play_dim_transition(config.get("dim"))
	_call_if_valid(config.get("switch_scene_with_data"), [
		str(config.get("out_scene_path", "")),
		return_payload,
	])


func _build_return_payload(callback: Variant) -> Dictionary:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			var payload: Variant = callable.call()
			if typeof(payload) == TYPE_DICTIONARY:
				return payload
	return {}


func _log_return_payload(scene_log: Variant, return_payload: Dictionary) -> void:
	if scene_log:
		scene_log.scene_event("InSceneMain", "return to out scene", return_payload)


func _store_pending_resolution(map_state: Variant, return_payload: Dictionary) -> void:
	if map_state and map_state.has_method("set_pending_room_resolution"):
		map_state.set_pending_room_resolution(return_payload)


func _prepare_return_audio(sound_manager: Variant) -> void:
	if not is_instance_valid(sound_manager):
		return
	if sound_manager.has_method("stop_looping_sfx"):
		sound_manager.stop_looping_sfx()
	if sound_manager.has_method("stop_bgm"):
		sound_manager.stop_bgm()
	if sound_manager.has_method("play_bgm_main_menu"):
		sound_manager.play_bgm_main_menu()


func _play_dim_transition(dim: Variant) -> void:
	if is_instance_valid(dim) and dim.has_method("use"):
		await dim.use(0, 0)


func _call_if_valid(callback: Variant, args: Array = []) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.callv(args)
