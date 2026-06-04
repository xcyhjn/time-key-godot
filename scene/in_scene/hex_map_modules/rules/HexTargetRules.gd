class_name HexTargetRules

## HexTargetRules 保存地图目标选择的轻量规则入口。
## 地图与主界面都通过这里判断卡牌目标，让 hover 高亮、tooltip 文案和放牌入口共享同一套细则。

## 判断当前选中卡牌是否能以指定 stack 为中心释放。
## 规则按卡牌 effects 判断；未知效果保持 permissive，避免旧卡或实验卡被结构拆分误伤。
static func is_stack_valid_target(stack: Area2D, selected_card: Variant, context: Dictionary = {}) -> bool:
	if not is_instance_valid(stack) or not _is_live_object(selected_card):
		return false

	var card_info := _get_card_info(selected_card)
	var effects := _get_effects(card_info)
	if effects.is_empty():
		return true

	var center_coord: Variant = _get_stack_coord(stack, context)
	var stack_nodes: Dictionary = context.get("stack_nodes", {})
	var has_restrictive_effect := false
	var has_valid_restrictive_target := false

	for effect in effects:
		if typeof(effect) != TYPE_DICTIONARY:
			continue

		var effect_type := str(effect.get("type", "")).to_lower()
		match effect_type:
			"clear":
				return true
			"built", "build":
				has_restrictive_effect = true
				has_valid_restrictive_target = has_valid_restrictive_target or is_empty_build_target(stack, context)
			"damage":
				has_restrictive_effect = true
				has_valid_restrictive_target = has_valid_restrictive_target or _range_has_target_for_effect(selected_card, stack, center_coord, stack_nodes, effect_type)
			"recover", "heal":
				has_restrictive_effect = true
				has_valid_restrictive_target = has_valid_restrictive_target or _range_has_target_for_effect(selected_card, stack, center_coord, stack_nodes, effect_type)
			"poison":
				has_restrictive_effect = true
				has_valid_restrictive_target = has_valid_restrictive_target or _range_has_target_for_effect(selected_card, stack, center_coord, stack_nodes, effect_type)
			"elevation":
				has_restrictive_effect = true
				has_valid_restrictive_target = true
			_:
				return true

	return has_valid_restrictive_target if has_restrictive_effect else true


## 判断目标地块是否可以建造。
## 这里同时检查 stack occupant 和 map_data，保持放牌阶段与 BuiltCommand 的最终结算防线一致。
static func is_empty_build_target(stack: Area2D, context: Dictionary = {}) -> bool:
	if not is_instance_valid(stack):
		return false

	if stack.has_meta("occupant"):
		var occupant: Variant = stack.get_meta("occupant")
		if is_instance_valid(occupant):
			return false

	var coord: Variant = _get_stack_coord(stack, context)
	if coord == null:
		return false

	var map_data: Dictionary = context.get("map_data", {})
	if not map_data.has(coord):
		return false

	var tile_data: Variant = map_data[coord]
	if typeof(tile_data) != TYPE_DICTIONARY:
		return false
	if tile_data.has("landform") and is_instance_valid(tile_data["landform"]):
		return false
	if tile_data.has("landform_in") and is_instance_valid(tile_data["landform_in"]):
		return false

	return true


## 根据卡牌提供的 `get_absolute_effect_range(center_coord)` 收集真实存在的地图 stack。
## `hex_map.gd` 负责传入 `stack_nodes`，本函数只过滤不存在或已释放的节点。
static func get_effect_range_stacks(card: Variant, center_coord: Variant, stack_nodes: Dictionary) -> Array[Area2D]:
	var range_stacks: Array[Area2D] = []
	if center_coord == null:
		return range_stacks
	if not _is_live_object(card):
		return range_stacks
	if not card.has_method("get_absolute_effect_range"):
		return range_stacks

	for coord in card.get_absolute_effect_range(center_coord):
		var stack_value: Variant = stack_nodes.get(coord, null)
		if is_instance_valid(stack_value) and stack_value is Area2D:
			range_stacks.append(stack_value)
	return range_stacks


## 从卡牌对象读取 card_info。
## 自定义卡牌通过属性保存 JSON；无该属性时返回空字典，让调用方走保守路径。
static func _get_card_info(card: Variant) -> Dictionary:
	if not _is_live_object(card):
		return {}
	var card_info: Variant = card.get("card_info")
	if typeof(card_info) == TYPE_DICTIONARY:
		return card_info
	return {}


## 读取 effects 数组。
## JSON 结构异常时返回空数组，避免目标高亮阶段抛错。
static func _get_effects(card_info: Dictionary) -> Array:
	var effects: Variant = card_info.get("effects", [])
	if typeof(effects) == TYPE_ARRAY:
		return effects
	return []


## 解析 stack 在 HexMap 中的坐标。
## 优先使用 context 里传入的显式坐标；否则从 stack_nodes 反查，兼容 MainBoard 的放牌入口。
static func _get_stack_coord(stack: Area2D, context: Dictionary) -> Variant:
	if context.has("center_coord"):
		var explicit_coord: Variant = context.get("center_coord")
		if explicit_coord != null:
			return explicit_coord

	var stack_nodes: Dictionary = context.get("stack_nodes", {})
	if stack_nodes.is_empty():
		return null
	return stack_nodes.find_key(stack)


## 判断卡牌范围内是否至少存在一个对应效果可作用的实体。
## 伤害、治疗和中毒都可能是 AOE；因此目标中心为空但范围内命中实体时仍视为合法。
static func _range_has_target_for_effect(card: Variant, center_stack: Area2D, center_coord: Variant, stack_nodes: Dictionary, effect_type: String) -> bool:
	var target_stacks := get_effect_range_stacks(card, center_coord, stack_nodes)
	if target_stacks.is_empty() and is_instance_valid(center_stack):
		target_stacks.append(center_stack)

	for target_stack in target_stacks:
		var entity := _get_stack_occupant(target_stack)
		match effect_type:
			"damage":
				if _can_damage_entity(entity):
					return true
			"recover", "heal":
				if _can_recover_entity(entity):
					return true
			"poison":
				if _can_poison_entity(entity):
					return true
	return false


## 读取 stack occupant。
## 统一把没有 occupant 或 occupant 已释放的地块视作空地。
static func _get_stack_occupant(stack: Area2D) -> Node:
	if not is_instance_valid(stack) or not stack.has_meta("occupant"):
		return null
	var entity: Variant = stack.get_meta("occupant")
	if _is_live_object(entity) and entity is Node:
		return entity
	return null


## 伤害目标：需要实体存在、支持 take_damage，且如果有 HP 则必须存活。
static func _can_damage_entity(entity: Node) -> bool:
	if not is_instance_valid(entity) or not entity.has_method("take_damage"):
		return false
	var hp: Variant = entity.get("HP")
	return hp == null or float(hp) > 0.0


## 治疗目标：需要实体存在、支持 heal；若有 HP/Max_Blood，则只治疗未满血目标。
static func _can_recover_entity(entity: Node) -> bool:
	if not is_instance_valid(entity) or not entity.has_method("heal"):
		return false
	var hp: Variant = entity.get("HP")
	var max_hp: Variant = entity.get("Max_Blood")
	if hp != null and max_hp != null:
		return float(hp) < float(max_hp)
	return true


## 中毒目标：需要实体存在、支持 add_status，且如果有 HP 则必须存活。
static func _can_poison_entity(entity: Node) -> bool:
	if not is_instance_valid(entity) or not entity.has_method("add_status"):
		return false
	var hp: Variant = entity.get("HP")
	return hp == null or float(hp) > 0.0


## 判断 Variant 是否为仍然有效的 Object。
## 目标规则会接收来自 CardManager、stack meta 和旧路径的动态值，先做对象门禁能避免类型误传。
static func _is_live_object(value: Variant) -> bool:
	return value is Object and is_instance_valid(value)
