extends Node
class_name MapGenerator

@export_group("基础设置")
@export var total_radius: int = 12
@export var void_probability: float = 0.7 
@export var layer_boundaries: Array[int] = [4, 8, 12]

@export_group("地形权重分布")
@export var weight_normal: int = 60
@export var weight_elite: int = 15
@export var weight_event: int = 15
@export var weight_mountain: int = 10

enum TileType { MOUNTAIN = -1, VOID = 0, NORMAL = 1, ELITE = 2, EVENT = 3, BOSS = 4, START = 5, CHAR_1 = 6, CHAR_2 = 7, CHAR_3 = 8, CHAR_4 = 9, CHAR_5 = 10, CHAR_6 = 11 }
enum RoomFeature { ENHANCE, DELETE, COMBINE, TRADE }

var rng = RandomNumberGenerator.new()
var hex_directions = [Vector2i(-1, 1), Vector2i(-1, 0), Vector2i(0, -1), Vector2i(1, -1), Vector2i(1, 0), Vector2i(0, 1)]
var _cached_total_weight: int = -1

func generate_logical_map() -> Dictionary:
	var data = {}
	var hex_dirs_size = hex_directions.size()
	
	for q in range(-total_radius, total_radius + 1):
		for r in range(-total_radius, total_radius + 1):
			var sum_qr = q + r
			if abs(sum_qr) <= total_radius:
				var coords = Vector2i(q, r)
				var d = (abs(q) + abs(r) + abs(sum_qr)) >> 1
				if d == 0: 
					data[coords] = TileType.START
				elif d == 1:
					for i in range(hex_dirs_size):
						if coords == hex_directions[i]: 
							data[coords] = (TileType.CHAR_1 + i) as TileType
				elif d in layer_boundaries: 
					data[coords] = TileType.BOSS
				elif rng.randf() < void_probability: 
					data[coords] = TileType.VOID
				else: 
					data[coords] = _get_random_type()
	
	return _ensure_all_paths(data)

func _get_random_type() -> TileType:
	if _cached_total_weight < 0:
		_cached_total_weight = weight_normal + weight_elite + weight_event + weight_mountain
	var roll = rng.randi_range(0, _cached_total_weight - 1)
	if roll < weight_normal: return TileType.NORMAL
	elif roll < weight_normal + weight_elite: return TileType.ELITE
	elif roll < weight_normal + weight_elite + weight_event: return TileType.EVENT
	return TileType.MOUNTAIN

func _ensure_all_paths(data: Dictionary) -> Dictionary:
	var target_r = layer_boundaries[2]
	for start_node in hex_directions:
		if not _has_path_to_edge(data, start_node, target_r):
			_force_dig_path(data, start_node, target_r)
	return data

func _has_path_to_edge(data: Dictionary, start: Vector2i, target_dist: int) -> bool:
	var queue = [start]
	var visited = {}
	visited[start] = true
	var hex_dirs_size = hex_directions.size()
	
	while queue.size() > 0:
		var curr = queue.pop_front()
		var curr_x = curr.x
		var curr_y = curr.y
		var curr_dist = (abs(curr_x) + abs(curr_y) + abs(curr_x + curr_y)) >> 1
		if curr_dist >= target_dist: 
			return true
		
		for i in range(hex_dirs_size):
			var dir = hex_directions[i]
			var n = Vector2i(curr_x + dir.x, curr_y + dir.y)
			if data.has(n) and not visited.has(n) and data[n] > 0 and is_in_sector(n, start):
				visited[n] = true
				queue.append(n)
	return false

func _force_dig_path(data: Dictionary, start: Vector2i, target_dist: int):
	var curr = start
	var hex_dirs_size = hex_directions.size()
	
	while true:
		var curr_x = curr.x
		var curr_y = curr.y
		var curr_dist = (abs(curr_x) + abs(curr_y) + abs(curr_x + curr_y)) >> 1
		if curr_dist >= target_dist:
			break
		
		var next_step = curr
		for i in range(hex_dirs_size):
			var dir = hex_directions[i]
			var n = Vector2i(curr_x + dir.x, curr_y + dir.y)
			if data.has(n) and is_in_sector(n, start):
				var n_dist = (abs(n.x) + abs(n.y) + abs(n.x + n.y)) >> 1
				if n_dist > curr_dist:
					next_step = n
					break
		curr = next_step
		if data[curr] <= 0: 
			data[curr] = TileType.NORMAL

func distribute_features(tile_data: Dictionary) -> Dictionary:
	var features = {}
	var pools = [[], [], []]
	var lb0 = layer_boundaries[0]
	var lb1 = layer_boundaries[1]
	
	for coords in tile_data:
		if tile_data[coords] == TileType.NORMAL:
			var d = (abs(coords.x) + abs(coords.y) + abs(coords.x + coords.y)) >> 1
			if d < lb0: 
				pools[0].append(coords)
			elif d < lb1: 
				pools[1].append(coords)
			else: 
				pools[2].append(coords)
	
	for i in range(3):
		_assign_to_pool(features, pools[i], 3, RoomFeature.ENHANCE)
		_assign_to_pool(features, pools[i], 2, RoomFeature.DELETE)
		_assign_to_pool(features, pools[i], 1, RoomFeature.COMBINE)
		_assign_to_pool(features, pools[i], 2, RoomFeature.TRADE)
	return features

func _assign_to_pool(f_map: Dictionary, pool: Array, count: int, feature: RoomFeature):
	var available = pool.duplicate()
	for i in range(min(count, available.size())):
		var pos = available.pop_at(rng.randi_range(0, available.size() - 1))
		if not f_map.has(pos): f_map[pos] = []
		f_map[pos].append(feature)

func get_hex_dist(a: Vector2i, b: Vector2i) -> int:
	return (abs(a.x - b.x) + abs(a.y - b.y) + abs(a.x + a.y - b.x - b.y)) / 2

func is_in_sector(coords: Vector2i, chosen_dir: Vector2i) -> bool:
	if coords == Vector2i.ZERO: return true
	return get_hex_dist(Vector2i.ZERO, coords) == get_hex_dist(chosen_dir, coords) + 1
