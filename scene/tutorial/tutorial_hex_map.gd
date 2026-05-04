# 功能: 教程专用 HexMap，继承正式战斗地图渲染/交互逻辑，但使用固定地块、固定怪物配置。
# 核心逻辑: _generate_map_data 注入固定高度与地形；_assign_terrains_and_enemies 只按导出表生成指定地貌，避免随机地图影响教程节奏。
extends "res://scene/in_scene/hex_map.gd"
class_name TutorialHexMap

@export_group("教程固定地块")
## 固定地块高度表。键为六边形轴向坐标，值为该格高度。
@export var tutorial_tile_heights: Dictionary = {
	Vector2i(0, 0): 4,
	Vector2i(1, 0): 2,
	Vector2i(0, 1): 2,
	Vector2i(-1, 1): 1,
	Vector2i(-1, 0): 2,
	Vector2i(0, -1): 1,
	Vector2i(1, -1): 2,
}
## 可选地形覆盖表。未填写的格子会根据高度走正式 get_terrain_from_height。
@export var tutorial_tile_terrain_overrides: Dictionary = {}
## 是否保留中心 nexus_core 标记，方便沿用正式地图的核心地块视觉。
@export var tutorial_use_nexus_core_center: bool = true

@export_group("教程固定地貌/敌人")
## 固定地貌表。每项格式: {"coord": Vector2i(1, 0), "script": preload(".../village.gd")}
@export var tutorial_landform_defs: Array[Dictionary] = [
	{
		"coord": Vector2i(1, 0),
		"script": preload("res://scene/in_scene/enermy/village.gd"),
	},
	{
		"coord": Vector2i(-1, 0),
		"script": preload("res://scene/in_scene/enermy/background.gd"),
	},
]


func _generate_map_data():
	map_data.clear()

	for coord in tutorial_tile_heights.keys():
		var coord_v2i: Vector2i = Vector2i(coord)
		var height: int = maxi(1, int(tutorial_tile_heights[coord]))
		var terrain_type = tutorial_tile_terrain_overrides.get(coord_v2i, get_terrain_from_height(height))

		if tutorial_use_nexus_core_center and coord_v2i == Vector2i.ZERO:
			terrain_type = "nexus_core"

		map_data[coord_v2i] = {
			"height": height,
			"tier": 3,
			"terrain": terrain_type,
			"terrain_type": terrain_type,
			"landform": null,
		}


func _assign_terrains_and_enemies():
	_rebuild_tutorial_height_pool()

	for def in tutorial_landform_defs:
		var coord: Vector2i = Vector2i(def.get("coord", Vector2i.ZERO))
		if not map_data.has(coord):
			continue

		var landform_script = def.get("script", null)
		if not (landform_script is Script):
			continue

		var inst = landform_script.new(coord, self)
		map_data[coord]["landform"] = inst
		map_data[coord]["landform_type"] = inst.landform_name


func _rebuild_tutorial_height_pool() -> void:
	if not GlobalClock or not _object_has_property(GlobalClock, &"tile_h_pool"):
		return

	for h_key in GlobalClock.tile_h_pool.keys():
		GlobalClock.tile_h_pool[h_key].clear()

	for coord in map_data.keys():
		var height: int = int(map_data[coord].get("height", 1))
		if not GlobalClock.tile_h_pool.has(height):
			GlobalClock.tile_h_pool[height] = []
		GlobalClock.tile_h_pool[height].append(coord)
