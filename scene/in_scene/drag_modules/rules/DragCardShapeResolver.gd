extends RefCounted


## DragCardShapeResolver 只负责从卡牌数据读取时间轴形状坐标。
## 它不启动拖拽、不修改卡牌节点，也不判断形状是否可以放置。


const TimelineClearEffectUtil = preload("res://scene/in_scene/timeline/TimelineClearEffect.gd")


func resolve_shape_coords(card: Control, is_timeline_clear_mode: bool) -> Array[Vector2i]:
	if not is_instance_valid(card):
		return [Vector2i(0, 0)]

	if is_timeline_clear_mode:
		return TimelineClearEffectUtil.get_clear_shape_coords(card)

	if _object_has_property(card, &"timeline_shape_coords") and card.timeline_shape_coords is Array and not card.timeline_shape_coords.is_empty():
		return convert_to_vector2i_array(card.timeline_shape_coords.duplicate())

	var raw_shape: Array = [Vector2i(0, 0)]
	if _object_has_property(card, &"card_info") and card.card_info is Dictionary:
		raw_shape = card.card_info.get("shape", [Vector2i(0, 0)])
	return convert_to_vector2i_array(raw_shape)


func convert_to_vector2i_array(raw_array: Array) -> Array[Vector2i]:
	var result: Array[Vector2i] = []

	for item: Variant in raw_array:
		if item is Vector2i:
			result.append(item)
		elif item is Array and item.size() >= 2:
			result.append(Vector2i(int(item[0]), int(item[1])))
		elif item is Dictionary and "x" in item and "y" in item:
			result.append(Vector2i(int(item["x"]), int(item["y"])))

	if result.is_empty():
		result.append(Vector2i(0, 0))

	return result


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
