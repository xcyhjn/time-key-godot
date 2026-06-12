extends RefCounted


## TileHealthStateRules 只负责计算 Tile 血量相关的纯规则结果。
## 它不发信号、不切换贴图、不清理状态组件，也不通知 HexMap 拓扑变化。


const STATE_BROKEN: StringName = &"broken"
const STATE_CAPTURED: StringName = &"captured"
const STATE_NORMAL: StringName = &"normal"


func normalize_health(new_hp: float, max_blood: float) -> Dictionary:
	var normalized_hp: float = clampf(new_hp, 0.0, max_blood)
	return {
		"hp": normalized_hp,
		"damage_rate": get_damage_rate(normalized_hp, max_blood)
	}


func get_damage_rate(current_hp: float, max_blood: float) -> float:
	if max_blood <= 0.0:
		return 0.0
	return 1.0 - (float(current_hp) / float(max_blood))


func get_main_state_for_health(current_hp: float, max_blood: float, capture_rate: float) -> StringName:
	if current_hp <= 0.0:
		return STATE_BROKEN
	if current_hp < max_blood * capture_rate:
		return STATE_CAPTURED
	return STATE_NORMAL
