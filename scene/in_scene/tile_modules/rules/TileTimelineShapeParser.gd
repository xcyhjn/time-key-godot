extends RefCounted


## TileTimelineShapeParser 只负责把地貌实体的时间占位矩阵解析为坐标和尺寸。
## 它不读取场景树、不修改 tile 节点，也不创建 TimelineAction 或判断敌人意图是否合法。


func parse(matrix_data: Variant) -> Dictionary:
	var parsed_coords: Array[Vector2i] = _extract_coords(matrix_data)
	if parsed_coords.is_empty():
		parsed_coords.append(Vector2i(0, 0))

	var shape_size: Vector2i = _get_shape_size(parsed_coords)
	return {
		"coords": parsed_coords,
		"size": shape_size
	}


func _extract_coords(matrix_data: Variant) -> Array[Vector2i]:
	var rows: Array[String] = _extract_rows(matrix_data)
	var result: Array[Vector2i] = []

	for y in range(rows.size()):
		var row: String = rows[y].strip_edges()
		for x in range(row.length()):
			if row[x] == "1":
				result.append(Vector2i(x, y))

	return result


func _extract_rows(matrix_data: Variant) -> Array[String]:
	var rows: Array[String] = []
	var trimmed: String = str(matrix_data).strip_edges()

	if trimmed.contains(","):
		for row_text in trimmed.split(",", false):
			rows.append(row_text)
	elif trimmed.contains("\n"):
		for row_text in trimmed.split("\n", false):
			rows.append(row_text)
	elif trimmed.contains(" ") and (trimmed.contains("0") or trimmed.contains("1")):
		for row_text in trimmed.split(" ", false):
			rows.append(row_text)
	else:
		rows.append(trimmed)

	return rows


func _get_shape_size(coords: Array[Vector2i]) -> Vector2i:
	var max_x: int = 0
	var max_y: int = 0

	for coord in coords:
		max_x = max(max_x, coord.x)
		max_y = max(max_y, coord.y)

	return Vector2i(max_x + 1, max_y + 1)
