# 功能: 战斗状态数据库，集中定义中毒等状态的显示、数值规则和 tooltip 文案。
# 核心逻辑:
# - get_status_definition(): 返回指定状态的完整配置。
# - get_status_color()/get_status_description(): 给 tooltip 和文本染色层提供稳定入口。
# - get_status_icon_path(): 给 StatusComponent 创建常驻状态 icon。
class_name StatusDB
extends Node

const POISON_ID: StringName = &"poison"

const STATUS_DEFINITIONS: Dictionary = {
	POISON_ID: {
		"display_name": "中毒",
		"color": "#a855f7",
		"description": "每回合向周围扩散，每层中毒在回合开始时候扣除所属建筑10%的血量，在这之后减少一层",
		"icon_path": "res://image/effect/poison_icon.png",
		"turn_damage_max_hp_ratio": 0.1,
		"spread_amount": 1,
		"decay_per_turn": 1,
		"vfx_name": &"poison",
		"icon_delay_seconds": 1.0,
	},
}


static func has_status(status_id: StringName) -> bool:
	return STATUS_DEFINITIONS.has(status_id)


static func get_status_definition(status_id: StringName) -> Dictionary:
	var definition: Variant = STATUS_DEFINITIONS.get(status_id, {})
	if definition is Dictionary:
		return definition.duplicate(true)
	return {}


static func get_status_display_name(status_id: StringName) -> String:
	return str(STATUS_DEFINITIONS.get(status_id, {}).get("display_name", String(status_id)))


static func get_status_color(status_id: StringName) -> String:
	return str(STATUS_DEFINITIONS.get(status_id, {}).get("color", "#ffffff"))


static func get_status_description(status_id: StringName) -> String:
	return str(STATUS_DEFINITIONS.get(status_id, {}).get("description", ""))


static func get_status_icon_path(status_id: StringName) -> String:
	return str(STATUS_DEFINITIONS.get(status_id, {}).get("icon_path", ""))


static func get_status_icon_delay_seconds(status_id: StringName) -> float:
	return float(STATUS_DEFINITIONS.get(status_id, {}).get("icon_delay_seconds", 0.0))


static func get_poison_damage_ratio() -> float:
	return float(STATUS_DEFINITIONS[POISON_ID].get("turn_damage_max_hp_ratio", 0.1))


static func get_poison_spread_amount() -> int:
	return int(STATUS_DEFINITIONS[POISON_ID].get("spread_amount", 1))


static func get_poison_decay_per_turn() -> int:
	return int(STATUS_DEFINITIONS[POISON_ID].get("decay_per_turn", 1))
