class_name InSceneReturnPayloadBuilder
extends RefCounted


## InSceneReturnPayloadBuilder 只负责组装“局内 -> 局外”的返回数据。
## 它不切换场景、不播放过渡动画，也不写入 MapState 的 pending resolution。


func build(config: Dictionary) -> Dictionary:
	var map_state: Variant = config.get("map_state")
	var global_timecoin: Variant = config.get("global_timecoin")
	var global_db: Variant = config.get("global_db")
	var room_context: Dictionary = _get_room_context(map_state)
	var deck_snapshot: Array = _get_deck_snapshot(global_db)

	return {
		"transition_type": "return_from_combat",
		"combat_result": "completed",
		"battle_state": "settlement",
		"battle_tag": str(config.get("battle_tag", "")),
		"map_seed": str(config.get("map_seed", "")),
		"era": int(config.get("era", 1)),
		"timecoins": _get_timecoins(global_timecoin),
		"deck_snapshot": deck_snapshot,
		"deck_size": deck_snapshot.size(),
		"room_context": room_context,
		"clear_active_room_context": true,
	}


func _get_room_context(map_state: Variant) -> Dictionary:
	if not map_state:
		return {}

	var context: Variant = map_state.get_active_room_context()
	if typeof(context) == TYPE_DICTIONARY:
		return context
	return {}


func _get_timecoins(global_timecoin: Variant) -> int:
	if not global_timecoin:
		return 0
	return int(global_timecoin.get_timecoins())


func _get_deck_snapshot(global_db: Variant) -> Array:
	if not global_db:
		return []
	return global_db.player_deck.duplicate()
