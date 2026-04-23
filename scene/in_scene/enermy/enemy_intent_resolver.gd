class_name EnemyIntentResolver
extends RefCounted

# ==========================================
# 脚本名称: enemy_intent_resolver.gd
# 功能概述:
# - 这是“敌人意图系统”的解析层。
# - 它负责从一个敌方/中立/友方单位上读取意图接口，然后组装成 EnemyIntentData。
# - 它不负责真正的渲染，只负责把“原始意图配置”变成“可直接用于地图和时间轴展示的数据”。
#
# 接收什么数据:
# - 一个产生意图的节点(通常是 landform 或敌人实体)
# - 当前 HexMap 引用
# - 可选的 TimelineAction（如果已经生成到了时间轴）
#
# 进行怎样的处理:
# - 读取 source_node / source_coord / description
# - 读取敌人自己的 effect_range 配置
# - 复用卡牌 effect_range 的解析格式，把范围展开成 target_coords
# - 判定意图是否合法
# - 生成 invalid_reason / includes_self / target_affiliation
# - 如果已有时间轴动作，则补齐 timeline_action 与 timeline_cells
#
# 主要函数职责:
# - resolve_enemy_intent():
#   输入一个敌人节点，输出一份 EnemyIntentData
# - parse_effect_range_offsets():
#   复用卡牌的 effect_range 格式，解析相对坐标
# - resolve_effect_tiles():
#   根据目标中心 + 相对坐标，得到最终地图格列表
#
# 数据最终传递到哪里:
# - 传给未来的 EnemyIntentPresentationController 做地图高亮、时间轴高亮和 tooltip 显示
# - 也可以被 TimelineManager / HexMap 用于重判意图合法性
# ==========================================


static func resolve_enemy_intent(source_node: Node, hex_map: battle, timeline_action: TimelineAction = null) -> EnemyIntentData:
	var data = EnemyIntentData.new()
	data.source_node = source_node
	data.timeline_action = timeline_action

	if not is_instance_valid(source_node) or not is_instance_valid(hex_map):
		data.invalid_reason = "意图源或地图无效"
		data.build_debug_label()
		return data

	data.source_coord = _resolve_source_coord(source_node)
	data.description = _resolve_description(source_node)
	data.effect_range_raw = _resolve_effect_range(source_node)
	data.target_affiliation = _resolve_target_affiliation(source_node)
	data.includes_self = _resolve_includes_self(source_node, hex_map)
	data.is_valid = _resolve_validity(source_node, hex_map)
	data.invalid_reason = _resolve_invalid_reason(source_node, hex_map)
	data.target_center_coord = _resolve_target_center_coord(source_node, hex_map)

	if data.target_center_coord != null:
		data.target_coords = resolve_effect_tiles(data.target_center_coord, data.effect_range_raw, hex_map)

	if timeline_action != null:
		data.timeline_cells = timeline_action.get_absolute_coords()

	data.build_debug_label()
	return data


static func parse_effect_range_offsets(range_data: Variant) -> Array[Vector2i]:
	var offsets: Array[Vector2i] = []

	# 情况1：数字半径。与卡牌 effect_range 规则保持一致。
	if typeof(range_data) == TYPE_INT or typeof(range_data) == TYPE_FLOAT:
		var radius = int(range_data)
		for q in range(-radius, radius + 1):
			for r in range(max(-radius, -q - radius), min(radius, -q + radius) + 1):
				offsets.append(Vector2i(q, r))

	# 情况2：自定义偏移数组，如 ["0,0", "1,0", "0,1"]
	elif typeof(range_data) == TYPE_ARRAY:
		for item in range_data:
			if typeof(item) == TYPE_STRING:
				var parts = item.split(",")
				if parts.size() == 2:
					offsets.append(Vector2i(int(parts[0]), int(parts[1])))
			elif item is Vector2i:
				offsets.append(item)

	# 兜底：至少影响目标中心本身
	if offsets.is_empty():
		offsets.append(Vector2i(0, 0))

	return offsets


static func resolve_effect_tiles(target_center_coord: Variant, range_data: Variant, hex_map: battle) -> Array[Vector2i]:
	var resolved: Array[Vector2i] = []
	if target_center_coord == null:
		return resolved

	var offsets = parse_effect_range_offsets(range_data)
	for offset in offsets:
		var coord = target_center_coord + offset
		if hex_map.map_data.has(coord):
			resolved.append(coord)

	return resolved


static func _resolve_source_coord(source_node: Node) -> Vector2i:
	if source_node.get("location") != null:
		return source_node.location
	return Vector2i.ZERO


static func _resolve_description(source_node: Node) -> String:
	if source_node.has_method("get_intent_description"):
		return source_node.get_intent_description()
	return "未知意图"


static func _resolve_effect_range(source_node: Node) -> Variant:
	if source_node.has_method("get_intent_effect_range"):
		return source_node.get_intent_effect_range()
	return 0


static func _resolve_target_center_coord(source_node: Node, hex_map: battle) -> Variant:
	if source_node.has_method("get_intent_target_center_coord"):
		return source_node.get_intent_target_center_coord(hex_map)
	return null


static func _resolve_validity(source_node: Node, hex_map: battle) -> bool:
	if source_node.has_method("can_generate_intent"):
		return source_node.can_generate_intent(hex_map)
	return false


static func _resolve_invalid_reason(source_node: Node, hex_map: battle) -> String:
	if source_node.has_method("get_intent_invalid_reason"):
		return source_node.get_intent_invalid_reason(hex_map)
	return "无可用目标"


static func _resolve_target_affiliation(source_node: Node) -> String:
	if source_node.has_method("get_intent_target_affiliation"):
		return source_node.get_intent_target_affiliation()
	return "enemy"


static func _resolve_includes_self(source_node: Node, hex_map: battle) -> bool:
	if source_node.has_method("does_intent_include_self"):
		return source_node.does_intent_include_self(hex_map)
	return false
