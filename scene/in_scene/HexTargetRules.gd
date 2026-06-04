class_name HexTargetRules

## HexTargetRules 保存地图目标选择的轻量规则入口。
## 当前阶段保持旧行为：只要地块和当前选中卡牌有效，就允许作为目标。
## 后续如果要区分伤害/治疗/建造目标，应先扩展这里，再让 `hex_map.gd` 继续只负责表现。

## 判断当前选中卡牌是否能以指定 stack 为中心释放。
## 目前不读取卡牌 JSON 细节，避免在结构拆分阶段改变玩法。
static func is_stack_valid_target(stack: Area2D, selected_card: Variant) -> bool:
	return is_instance_valid(stack) and is_instance_valid(selected_card)


## 根据卡牌提供的 `get_absolute_effect_range(center_coord)` 收集真实存在的地图 stack。
## `hex_map.gd` 负责传入 `stack_nodes`，本函数只过滤不存在或已释放的节点。
static func get_effect_range_stacks(card: Variant, center_coord: Variant, stack_nodes: Dictionary) -> Array[Area2D]:
	var range_stacks: Array[Area2D] = []
	if center_coord == null:
		return range_stacks
	if not is_instance_valid(card):
		return range_stacks
	if not card.has_method("get_absolute_effect_range"):
		return range_stacks

	for coord in card.get_absolute_effect_range(center_coord):
		var stack_value: Variant = stack_nodes.get(coord, null)
		if is_instance_valid(stack_value) and stack_value is Area2D:
			range_stacks.append(stack_value)
	return range_stacks
