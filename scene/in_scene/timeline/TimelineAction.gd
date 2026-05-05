class_name TimelineAction
extends RefCounted

enum Type { ENEMY, PLAYER }

var type: Type
var source_node: Node  # 释放者（敌人或玩家节点）
var target_tile: Node  # 目标地块节点（用于后续高亮）
var shape_coords: Array[Vector2i]  # 本地形状坐标，例如 [Vector2i(0,0), Vector2i(0,1)]
var origin_grid_pos: Vector2i  # 放置在时间轴上的左上角原点坐标
var color: Color  # 颜色（敌人红色，玩家由卡牌决定）
var action_data: Dictionary  # 具体的技能效果数据
var intent_priority: int = 0  # 敌人意图生成优先级，主要用于调试、展示和后续重判


func _init(p_type: Type, p_source: Node, p_target: Node, p_coords: Array[Vector2i], p_color: Color, p_data: Dictionary = { }):
	type = p_type
	source_node = p_source
	target_tile = p_target
	shape_coords = p_coords.duplicate()
	color = p_color
	action_data = p_data
	if action_data.has("intent_priority"):
		intent_priority = int(action_data["intent_priority"])


# 获取该行动在时间轴上的所有绝对坐标
func get_absolute_coords() -> Array[Vector2i]:
	var abs_coords: Array[Vector2i] = []
	for offset in shape_coords:
		abs_coords.append(origin_grid_pos + offset)
	return abs_coords
