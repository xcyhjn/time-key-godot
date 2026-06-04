class_name HexTargetRules


static func is_stack_valid_target(stack: Area2D, selected_card: Variant) -> bool:
	return is_instance_valid(stack) and is_instance_valid(selected_card)


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
