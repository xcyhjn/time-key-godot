class_name HexCoordRules

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


static func get_circular_coords(radius: int) -> Array[Vector2i]:
	var coords: Array[Vector2i] = []
	for q in range(-radius, radius + 1):
		for r in range(-radius, radius + 1):
			if q == 0 and r == 0:
				continue
			if get_axial_distance_from_origin(q, r) <= radius:
				coords.append(Vector2i(q, r))
	return coords


static func get_axial_distance_from_origin(q: int, r: int) -> int:
	return int((abs(q) + abs(q + r) + abs(r)) / 2)


static func hex_to_pixel(hex_coord: Vector2, spacing_x: float, spacing_y: float, scale_ratio: float) -> Vector2:
	var screen_x := hex_coord.x * spacing_x * scale_ratio
	var screen_y := hex_coord.y * spacing_y * scale_ratio + (hex_coord.x * spacing_y * scale_ratio * 0.5)
	return Vector2(screen_x, screen_y)
