class_name HexCoordRules

## HexCoordRules 只放六边形坐标与屏幕坐标换算，不读取/修改场景节点。
## 这些函数被 `hex_map.gd` 的地图生成流程调用，调整地图半径、角度、间距时优先检查这里。

## 生成扇形战斗地图坐标。
## `radius` 控制轴坐标半径，`angle_span/center_angle` 控制扇形朝向，
## `spacing_x/spacing_y/scale_ratio` 用于把轴坐标投影到像素后再判断角度。
static func get_fan_coords(
	radius: int,
	angle_span: float,
	center_angle: float,
	spacing_x: float,
	spacing_y: float,
	scale_ratio: float
) -> Array[Vector2i]:
	var coords: Array[Vector2i] = []
	var min_angle := center_angle - (angle_span / 2.0)
	var max_angle := center_angle + (angle_span / 2.0)

	for q in range(-radius, radius + 1):
		for r in range(-radius, radius + 1):
			if q == 0 and r == 0:
				continue
			if get_axial_distance_from_origin(q, r) <= radius:
				var pixel_pos := hex_to_pixel(Vector2(q, r), spacing_x, spacing_y, scale_ratio)
				var angle_deg := rad_to_deg(pixel_pos.angle())
				if angle_deg < 0:
					angle_deg += 360.0
				if angle_deg >= min_angle and angle_deg <= max_angle:
					coords.append(Vector2i(q, r))
	return coords


## 生成以原点为中心的完整圆形/六边形半径坐标。
## 原点 `(0, 0)` 会被跳过，因为当前战斗地图中心通常由专门逻辑处理。
static func get_circular_coords(radius: int) -> Array[Vector2i]:
	var coords: Array[Vector2i] = []
	for q in range(-radius, radius + 1):
		for r in range(-radius, radius + 1):
			if q == 0 and r == 0:
				continue
			if get_axial_distance_from_origin(q, r) <= radius:
				coords.append(Vector2i(q, r))
	return coords


## 计算轴坐标到原点的六边形距离。
## 这是 fan tier、地图边界、未来范围规则都可以复用的基础公式。
static func get_axial_distance_from_origin(q: int, r: int) -> int:
	return int((abs(q) + abs(q + r) + abs(r)) / 2)


## 将轴坐标转换为当前项目使用的 2D 像素坐标。
## `scale_ratio` 通常来自 `tile_scale / REF_SCALE`，确保地图缩放和 hitbox 对齐。
static func hex_to_pixel(hex_coord: Vector2, spacing_x: float, spacing_y: float, scale_ratio: float) -> Vector2:
	var screen_x := hex_coord.x * spacing_x * scale_ratio
	var screen_y := hex_coord.y * spacing_y * scale_ratio + (hex_coord.x * spacing_y * scale_ratio * 0.5)
	return Vector2(screen_x, screen_y)
