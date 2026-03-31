class_name HexUtils

static func hex_distance(a: Vector2i, b: Vector2i) -> int:
	return (abs(a.x - b.x) + abs(a.y - b.y) + abs(a.x + a.y - b.x - b.y)) / 2

static func hex_to_pixel(q: int, r: int, step_x: float, step_y: float, stagger_y: float) -> Vector2:
	return Vector2(q * step_x, r * step_y + q * stagger_y)

static func pixel_to_hex(pos: Vector2, step_x: float, step_y: float, stagger_y: float) -> Vector2i:
	var q_float = pos.x / step_x
	var r_float = (pos.y - q_float * stagger_y) / step_y
	return hex_round(q_float, r_float)

static func hex_round(q: float, r: float) -> Vector2i:
	var s = -q - r
	var rq = round(q); var rr = round(r); var rs = round(s)
	if abs(rq - q) > abs(rr - r) and abs(rq - q) > abs(rs - s): rq = -rr - rs
	elif abs(rr - r) > abs(rs - s): rr = -rq - rs
	return Vector2i(int(rq), int(rr))
