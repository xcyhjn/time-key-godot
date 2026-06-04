class_name HexTerrainRules

const TERRAIN_BEACH := 0
const TERRAIN_PLAINS := 1
const TERRAIN_HILLS := 2
const TERRAIN_MOUNTAIN := 3

const LANDFORM_NONE := 0
const LANDFORM_MINE := 1
const LANDFORM_CAVE := 2
const LANDFORM_VILLAGE := 3
const LANDFORM_RUINS := 4

const ROOM_BATTLE_ELITE := "battle_elite"
const ROOM_BOSS_STAGE := "boss_stage"


static func get_fan_tier(coord: Vector2i, inner_tier_radius: int) -> int:
	var dist := HexCoordRules.get_axial_distance_from_origin(coord.x, coord.y)
	if dist <= inner_tier_radius:
		return 3
	if dist <= inner_tier_radius + 2:
		return 2
	return 1


static func roll_height_by_tier_with_rng(tier: int, rng: RandomNumberGenerator) -> int:
	var roll := rng.randf()
	if tier == 3:
		return rng.randi_range(4, 6) if roll < 0.6 else rng.randi_range(2, 3)
	return rng.randi_range(1, 3) if roll < 0.7 else rng.randi_range(3, 4)


static func roll_height_by_tier(tier: int) -> int:
	var roll := randf()
	if tier == 3:
		return randi_range(4, 6) if roll < 0.6 else randi_range(2, 3)
	return randi_range(1, 3) if roll < 0.7 else randi_range(3, 4)


static func roll_circular_height_with_rng(
	room_type: String,
	base_h_min: int,
	base_h_max: int,
	elite_h_bonus: int,
	rng: RandomNumberGenerator
) -> int:
	var height_range := get_height_range_for_room(room_type, base_h_min, base_h_max, elite_h_bonus)
	return rng.randi_range(height_range.x, height_range.y)


static func get_height_range_for_room(
	room_type: String,
	base_h_min: int,
	base_h_max: int,
	elite_h_bonus: int
) -> Vector2i:
	var h_min := base_h_min
	var h_max := base_h_max
	if room_type == ROOM_BATTLE_ELITE:
		h_max += elite_h_bonus
	elif room_type == ROOM_BOSS_STAGE:
		h_max += elite_h_bonus + 1
	return Vector2i(h_min, h_max)


static func terrain_from_height(
	height: int,
	terrain_by_height: Dictionary,
	beach: int = TERRAIN_BEACH,
	plains: int = TERRAIN_PLAINS,
	hills: int = TERRAIN_HILLS,
	mountain: int = TERRAIN_MOUNTAIN
) -> int:
	if terrain_by_height.has(height):
		return int(terrain_by_height[height])
	if height <= 1:
		return beach
	if height <= 3:
		return plains
	if height == 4:
		return hills
	return mountain


static func terrain_name(terrain: int, unknown_name: String = "UNK") -> String:
	match terrain:
		TERRAIN_BEACH: return "BEACH"
		TERRAIN_PLAINS: return "PLAINS"
		TERRAIN_HILLS: return "HILLS"
		TERRAIN_MOUNTAIN: return "MOUNTAIN"
		_: return unknown_name


static func landform_name(landform_type: int, unknown_name: String = "UNK") -> String:
	match landform_type:
		LANDFORM_NONE: return "NONE"
		LANDFORM_MINE: return "MINE"
		LANDFORM_CAVE: return "CAVE"
		LANDFORM_VILLAGE: return "VILLAGE"
		LANDFORM_RUINS: return "RUINS"
		_: return unknown_name
