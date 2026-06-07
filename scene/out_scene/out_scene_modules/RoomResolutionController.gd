extends RefCounted


## RoomResolutionController 只负责局外房间结算 payload 的读取、解析和章节推进判断。
## 它不播放章节揭示动画，不移动玩家，不切换场景，也不直接写入 current_tier。


func consume_pending_payload(pending_external_event: Variant, map_state: Variant) -> Dictionary:
	var resolution_payload: Dictionary = {}

	if pending_external_event is Dictionary:
		var external_payload: Dictionary = pending_external_event
		resolution_payload = external_payload.duplicate(true)
		_call_map_state_method(map_state, &"clear_pending_room_resolution")
	elif _has_map_state_method(map_state, &"peek_pending_room_resolution"):
		var peeked_payload: Variant = _call_map_state_method(map_state, &"peek_pending_room_resolution")
		if peeked_payload is Dictionary:
			var peeked_resolution: Dictionary = peeked_payload
			resolution_payload = peeked_resolution.duplicate(true)

		if not resolution_payload.is_empty() and _has_map_state_method(map_state, &"consume_pending_room_resolution"):
			var consumed_payload: Variant = _call_map_state_method(map_state, &"consume_pending_room_resolution")
			if consumed_payload is Dictionary:
				var consumed_resolution: Dictionary = consumed_payload
				resolution_payload = consumed_resolution.duplicate(true)

	return resolution_payload


func should_clear_active_room_context(payload: Dictionary) -> bool:
	return bool(payload.get("clear_active_room_context", true))


func should_advance_tier_from_boss_payload(
	payload: Dictionary,
	tile_data: Dictionary,
	layer_boundaries: Array,
	current_tier: int,
	fallback_hex: Vector2i,
	boss_tile_type: int
) -> bool:
	if str(payload.get("combat_result", "")) != "completed":
		return false

	var room_context: Dictionary = _get_room_context(payload)
	var is_boss_room := str(payload.get("battle_tag", "")) == "boss_stage"
	is_boss_room = is_boss_room or int(room_context.get("room_type", 0)) == boss_tile_type
	is_boss_room = is_boss_room or str(room_context.get("room_data", "")).begins_with("boss_stage")
	if not is_boss_room:
		return false

	var max_tier_index := int(layer_boundaries.size()) - 1
	if max_tier_index < 0 or current_tier >= max_tier_index:
		return false

	var boss_hex: Vector2i = get_room_hex_from_payload(payload, fallback_hex)
	if not tile_data.has(boss_hex) or int(tile_data.get(boss_hex, 0)) != boss_tile_type:
		return false

	var current_boundary: int = int(layer_boundaries[clamp(current_tier, 0, max_tier_index)])
	return get_hex_distance_from_origin(boss_hex) == current_boundary


func build_tier_advance_plan(layer_boundaries: Array, current_tier: int) -> Dictionary:
	var max_tier_index := int(layer_boundaries.size()) - 1
	if max_tier_index < 0 or current_tier >= max_tier_index:
		return {"should_advance": false}

	var previous_radius: int = int(layer_boundaries[clamp(current_tier, 0, max_tier_index)])
	var next_tier := current_tier + 1
	var next_radius: int = int(layer_boundaries[clamp(next_tier, 0, max_tier_index)])

	return {
		"should_advance": true,
		"previous_radius": previous_radius,
		"next_tier": next_tier,
		"next_radius": next_radius,
	}


func get_room_hex_from_payload(payload: Dictionary, fallback_hex: Vector2i) -> Vector2i:
	var room_context: Dictionary = _get_room_context(payload)
	var room_hex: Variant = room_context.get("room_hex", fallback_hex)
	return variant_to_vector2i(room_hex, fallback_hex)


func variant_to_vector2i(value: Variant, fallback: Vector2i = Vector2i.ZERO) -> Vector2i:
	if value is Vector2i:
		return value
	if value is Vector2:
		return Vector2i(int(value.x), int(value.y))
	if value is Dictionary and value.has("x") and value.has("y"):
		return Vector2i(int(value.x), int(value.y))
	if value is String:
		var stripped: String = value.strip_edges()
		if stripped.begins_with("(") and stripped.ends_with(")"):
			stripped = stripped.substr(1, stripped.length() - 2)
		var parts: PackedStringArray = stripped.split(",", false)
		if parts.size() >= 2:
			return Vector2i(int(parts[0].strip_edges()), int(parts[1].strip_edges()))
	return fallback


func get_hex_distance_from_origin(coords: Vector2i) -> int:
	return (abs(coords.x) + abs(coords.y) + abs(coords.x + coords.y)) >> 1


func _get_room_context(payload: Dictionary) -> Dictionary:
	var room_context: Variant = payload.get("room_context", {})
	if room_context is Dictionary:
		return room_context
	return {}


func _has_map_state_method(map_state: Variant, method_name: StringName) -> bool:
	var map_state_object := map_state as Object
	return map_state_object != null and map_state_object.has_method(method_name)


func _call_map_state_method(map_state: Variant, method_name: StringName) -> Variant:
	var map_state_object := map_state as Object
	if map_state_object == null or not map_state_object.has_method(method_name):
		return null
	return map_state_object.call(method_name)
