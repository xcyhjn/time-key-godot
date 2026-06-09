extends RefCounted


## CustomCardEffectRangeParser 只负责解析卡牌六边形效果范围偏移。
## 它不读取地图节点，不执行卡牌效果，也不决定目标是否合法或如何高亮。


func parse(range_data: Variant) -> Array[Vector2i]:
	var offsets: Array[Vector2i] = []

	if typeof(range_data) == TYPE_INT or typeof(range_data) == TYPE_FLOAT:
		offsets = _parse_radius(int(range_data))
	elif typeof(range_data) == TYPE_ARRAY:
		offsets = _parse_custom_offsets(range_data)

	if offsets.is_empty():
		offsets.append(Vector2i(0, 0))

	return offsets


func _parse_radius(radius: int) -> Array[Vector2i]:
	var offsets: Array[Vector2i] = []

	for q in range(-radius, radius + 1):
		for r in range(max(-radius, -q - radius), min(radius, -q + radius) + 1):
			offsets.append(Vector2i(q, r))

	return offsets


func _parse_custom_offsets(range_items: Array) -> Array[Vector2i]:
	var offsets: Array[Vector2i] = []

	for item in range_items:
		if typeof(item) != TYPE_STRING:
			continue
		var parts := (item as String).split(",")
		if parts.size() == 2:
			offsets.append(Vector2i(int(parts[0]), int(parts[1])))

	return offsets
