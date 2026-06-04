class_name HexTerrainRules

## HexTerrainRules 集中保存地形/高度的纯规则。
## 它不实例化节点，也不直接读取 `hex_map.gd` 的导出变量；调用方必须把导出值作为参数传入。

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


## 根据扇形地图中的轴坐标距离，返回高度权重层级。
## tier 越高越靠近核心区域，后续高度掷骰会给更高地块概率。
static func get_fan_tier(coord: Vector2i, inner_tier_radius: int) -> int:
	var dist := HexCoordRules.get_axial_distance_from_origin(coord.x, coord.y)
	if dist <= inner_tier_radius:
		return 3
	if dist <= inner_tier_radius + 2:
		return 2
	return 1


## 使用传入的 RandomNumberGenerator 按 tier 掷高度。
## 战斗地图生成使用这个版本，以便外部 seed 能稳定复现同一张地图。
static func roll_height_by_tier_with_rng(tier: int, rng: RandomNumberGenerator) -> int:
	var roll := rng.randf()
	if tier == 3:
		return rng.randi_range(4, 6) if roll < 0.6 else rng.randi_range(2, 3)
	return rng.randi_range(1, 3) if roll < 0.7 else rng.randi_range(3, 4)


## 不依赖外部 RNG 的旧接口保留给兼容调用。
## 新地图生成优先使用 `roll_height_by_tier_with_rng()`。
static func roll_height_by_tier(tier: int) -> int:
	var roll := randf()
	if tier == 3:
		return randi_range(4, 6) if roll < 0.6 else randi_range(2, 3)
	return randi_range(1, 3) if roll < 0.7 else randi_range(3, 4)


## 根据房间类型计算圆形地图高度范围，并用外部 RNG 取具体高度。
## `battle_elite` 和 `boss_stage` 会在普通范围上增加额外高度上限。
static func roll_circular_height_with_rng(
	room_type: String,
	base_h_min: int,
	base_h_max: int,
	elite_h_bonus: int,
	rng: RandomNumberGenerator
) -> int:
	var height_range := get_height_range_for_room(room_type, base_h_min, base_h_max, elite_h_bonus)
	return rng.randi_range(height_range.x, height_range.y)


## 返回某个房间类型的圆形地图高度上下限。
## 这个函数只计算范围，不随机，方便以后做基线表或调参工具。
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


## 把高度映射为地形枚举。
## 优先使用 `terrain_by_height` 导出字典；字典缺失时使用项目当前默认兜底规则。
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


## 地形枚举转调试文本。
## 主要用于日志/面板展示，不参与玩法判定。
static func terrain_name(terrain: int, unknown_name: String = "UNK") -> String:
	match terrain:
		TERRAIN_BEACH: return "BEACH"
		TERRAIN_PLAINS: return "PLAINS"
		TERRAIN_HILLS: return "HILLS"
		TERRAIN_MOUNTAIN: return "MOUNTAIN"
		_: return unknown_name


## 地貌枚举转调试文本。
## 保持和 `hex_map.gd` 内 LandformType 枚举序号一致。
static func landform_name(landform_type: int, unknown_name: String = "UNK") -> String:
	match landform_type:
		LANDFORM_NONE: return "NONE"
		LANDFORM_MINE: return "MINE"
		LANDFORM_CAVE: return "CAVE"
		LANDFORM_VILLAGE: return "VILLAGE"
		LANDFORM_RUINS: return "RUINS"
		_: return unknown_name
