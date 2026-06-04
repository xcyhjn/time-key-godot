class_name MapGenerationService
extends RefCounted


## MapGenerationService 负责生成局内地图的纯规则数据。
## 它只返回新的 `map_data` 字典，不创建节点、不实例化地貌，也不写 GlobalClock。
## HexMap 继续保留 Inspector 导出变量，并把这些值打包成 config 传入。

const MODE_FAN := 0
const MODE_CIRCULAR := 1
const SHAPE_FAN := "fan"
const SHAPE_CIRCULAR := "circular"
const TERRAIN_NEXUS_CORE := "nexus_core"


## 根据地图模式生成新的 map_data。
## 无效模式保持旧行为：报错后回退到扇形战斗地图。
func generate(config: Dictionary) -> Dictionary:
	var mode := int(config.get("mode", MODE_FAN))
	match mode:
		MODE_FAN:
			return generate_fan(config)
		MODE_CIRCULAR:
			return generate_circular(config)
		_:
			push_error("无效的地图生成模式: " + str(mode))
			var fallback_config := config.duplicate()
			fallback_config["mode"] = int(config.get("fallback_mode", MODE_FAN))
			return generate_fan(fallback_config)


## 生成扇形战斗地图数据。
## 坐标采样、核心地块和 tier 字段都保持旧 HexMap 行为。
func generate_fan(config: Dictionary) -> Dictionary:
	var coords := HexCoordRules.get_fan_coords(
		int(config["fan_radius"]),
		float(config["fan_angle_span"]),
		float(config.get("fan_center_angle", 90.0)),
		float(config["spacing_x"]),
		float(config["spacing_y"]),
		float(config["scale_ratio"])
	)
	return _populate_map_data(coords, SHAPE_FAN, config)


## 生成圆形随机地图数据。
## 圆形模式没有 nexus_core，也不会写 tier 字段。
func generate_circular(config: Dictionary) -> Dictionary:
	var coords := HexCoordRules.get_circular_coords(int(config["map_radius"]))
	return _populate_map_data(coords, SHAPE_CIRCULAR, config)


## 将坐标数组转换为 map_data。
## 返回值使用字典包裹，方便后续扩展生成报告或调试指标，而不改变 HexMap 调用形式。
func _populate_map_data(coords: Array[Vector2i], shape_type: String, config: Dictionary) -> Dictionary:
	var rng := config["rng"] as RandomNumberGenerator
	_prepare_rng(rng, config)

	var next_map_data: Dictionary = {}
	if shape_type == SHAPE_FAN:
		_add_nexus_core_tile(next_map_data, config)

	for coord in coords:
		var tile_data := _build_tile_data(coord, shape_type, rng, config)
		if not tile_data.is_empty():
			next_map_data[coord] = tile_data

	return {
		"map_data": next_map_data,
		"shape_type": shape_type,
	}


## 初始化本次生成使用的随机数。
## 外部 seed 优先使用 `incoming_map_seed`，没有 seed 时沿用旧 `received_text.hash()` 行为。
func _prepare_rng(rng: RandomNumberGenerator, config: Dictionary) -> void:
	if rng == null:
		return

	var received_text := str(config.get("received_text", ""))
	if bool(config.get("use_external_seed", false)) and received_text != "":
		var incoming_map_seed := str(config.get("incoming_map_seed", ""))
		var seed_text := incoming_map_seed if incoming_map_seed != "" else received_text
		rng.seed = seed_text.hash()
	else:
		rng.randomize()


## 写入扇形地图中心的 nexus_core。
## 这个特殊地块使用字符串 terrain，后续渲染仍由 HexMap 的纹理入口兼容。
func _add_nexus_core_tile(next_map_data: Dictionary, config: Dictionary) -> void:
	next_map_data[Vector2i(0, 0)] = {
		"height": int(config["nexus_height"]),
		"tier": 3,
		"terrain": TERRAIN_NEXUS_CORE,
		"terrain_type": TERRAIN_NEXUS_CORE,
		"landform": null,
	}


## 构建单个坐标的地块规则数据。
## 字段结构保持旧约定：height、terrain、terrain_type、landform，扇形模式额外带 tier。
func _build_tile_data(coord: Vector2i, shape_type: String, rng: RandomNumberGenerator, config: Dictionary) -> Dictionary:
	var height: int
	var tier := 1

	match shape_type:
		SHAPE_FAN:
			tier = HexTerrainRules.get_fan_tier(coord, int(config["inner_tier_radius"]))
			height = HexTerrainRules.roll_height_by_tier_with_rng(tier, rng)
		SHAPE_CIRCULAR:
			height = HexTerrainRules.roll_circular_height_with_rng(
				str(config.get("room_type", "")),
				int(config["base_h_min"]),
				int(config["base_h_max"]),
				int(config["elite_h_bonus"]),
				rng
			)
		_:
			push_error("未知的形状类型: " + shape_type)
			return {}

	var terrain := _terrain_from_height(height, config)
	var tile_data := {
		"height": height,
		"terrain": terrain,
		"terrain_type": terrain,
		"landform": null,
	}

	if shape_type == SHAPE_FAN:
		tile_data["tier"] = tier

	return tile_data


## 按高度换算地形枚举。
## terrain_by_height 仍来自 HexMap 的导出变量，服务只消费快照。
func _terrain_from_height(height: int, config: Dictionary) -> int:
	return HexTerrainRules.terrain_from_height(
		height,
		config.get("terrain_by_height", {}),
		HexTerrainRules.TERRAIN_BEACH,
		HexTerrainRules.TERRAIN_PLAINS,
		HexTerrainRules.TERRAIN_HILLS,
		HexTerrainRules.TERRAIN_MOUNTAIN
	)
