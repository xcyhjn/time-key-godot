extends RefCounted


## CustomCardTimelineShapeParser 只负责把卡牌时间轴形状数据解析成坐标、尺寸和标准 key。
## 它不读取场景树，不修改卡牌节点，也不决定卡牌能否放置或如何打出。


func parse(shape_data: Variant) -> Dictionary:
	var temp_coords: Array[Vector2i] = _extract_coords(shape_data)
	if temp_coords.is_empty():
		temp_coords.append(Vector2i(0, 0))

	var bounds: Dictionary = _get_bounds(temp_coords)
	var min_x: int = int(bounds["min_x"])
	var min_y: int = int(bounds["min_y"])
	var max_x: int = int(bounds["max_x"])
	var max_y: int = int(bounds["max_y"])

	var normalized_coords: Array[Vector2i] = []
	for coord in temp_coords:
		normalized_coords.append(Vector2i(coord.x - min_x, coord.y - min_y))

	var shape_size := Vector2i(max_x - min_x + 1, max_y - min_y + 1)
	return {
		"coords": normalized_coords,
		"size": shape_size,
		"key": _build_shape_key(normalized_coords, shape_size)
	}


func _extract_coords(shape_data: Variant) -> Array[Vector2i]:
	if _is_vector2i_array(shape_data):
		var parsed_coords: Array[Vector2i] = []
		for item in shape_data:
			parsed_coords.append(item)
		return parsed_coords

	var raw_rows: Array[String] = []
	if typeof(shape_data) == TYPE_ARRAY:
		for row in shape_data:
			raw_rows.append(str(row))
	elif typeof(shape_data) == TYPE_STRING:
		var text := (shape_data as String).replace("\n", ",").replace(" ", ",")
		for row in text.split(",", false):
			raw_rows.append(row.strip_edges())

	var result: Array[Vector2i] = []
	for y in range(raw_rows.size()):
		var row_text := raw_rows[y]
		for x in range(row_text.length()):
			if row_text[x] == "1":
				result.append(Vector2i(x, y))

	return result


func _is_vector2i_array(value: Variant) -> bool:
	if typeof(value) != TYPE_ARRAY or value.is_empty():
		return false

	for item in value:
		if typeof(item) != TYPE_VECTOR2I:
			return false

	return true


func _get_bounds(coords: Array[Vector2i]) -> Dictionary:
	var min_x := 9999
	var min_y := 9999
	var max_x := -9999
	var max_y := -9999

	for coord in coords:
		if coord.x < min_x:
			min_x = coord.x
		if coord.x > max_x:
			max_x = coord.x
		if coord.y < min_y:
			min_y = coord.y
		if coord.y > max_y:
			max_y = coord.y

	return {
		"min_x": min_x,
		"min_y": min_y,
		"max_x": max_x,
		"max_y": max_y
	}


func _build_shape_key(coords: Array[Vector2i], shape_size: Vector2i) -> String:
	var canonical_rows: PackedStringArray = []
	for y in range(shape_size.y):
		var row_text := ""
		for x in range(shape_size.x):
			if coords.has(Vector2i(x, y)):
				row_text += "1"
			else:
				row_text += "0"
		canonical_rows.append(row_text)

	return ",".join(canonical_rows)
