class_name LandformPlacementService
extends RefCounted


## LandformPlacementService 负责把地貌实例投放到已生成的 map_data 上。
## 它会写入 map_data 的 `landform` 和 `landform_type` 字段，并维护传入的 tile_h_pool；
## 但它不创建地块节点、不挂接 sprite、不生成血条，这些仍由后续地块创建服务处理。

const INVALID_COORD := Vector2i(-100, -100)
const TERRAIN_NEXUS_CORE := "nexus_core"


## 按旧顺序投放地貌：先中立，再敌方。
## 每一类内部仍保持“每种脚本先尝试一个，再随机补足剩余配额”的流程。
func assign(map_data: Dictionary, config: Dictionary) -> Dictionary:
	var rng := config["rng"] as RandomNumberGenerator
	if rng:
		rng.randomize()

	var valid_tile_count := _rebuild_height_pool(map_data, config)
	var absolute_max_landforms := int(valid_tile_count * float(config["max_landform_ratio"]))
	var current_placed := 0

	var neutral_to_place: int = min(int(config["max_neutral_landforms"]), absolute_max_landforms - current_placed)
	current_placed += place_from_pool(config["neutral_pool"], neutral_to_place, map_data, config)

	var enemy_to_place: int = min(int(config["max_enemy_landforms"]), absolute_max_landforms - current_placed)
	current_placed += place_from_pool(config["enemy_pool"], enemy_to_place, map_data, config)

	return {
		"placed": current_placed,
		"valid_tile_count": valid_tile_count,
		"absolute_max_landforms": absolute_max_landforms,
	}


## 从指定地貌池投放一定数量的地貌。
## 这个入口保留给 HexMap 旧调试 wrapper 使用，也方便后续单独测试某个池子的投放结果。
func place_from_pool(pool: Array[Script], max_count: int, map_data: Dictionary, config: Dictionary) -> int:
	if pool.is_empty() or max_count <= 0:
		return 0

	var number := 0
	for landform_script in pool:
		if number >= max_count:
			break
		if _try_place_landform(landform_script, map_data, config):
			number += 1

	var remain := max_count - number
	if remain > 0:
		for i in range(remain):
			var landform_script := pool.pick_random() as Script
			if _try_place_landform(landform_script, map_data, config):
				number += 1

	return number


## 使用地貌探针选择一个可投放坐标。
## 探针只用于读取 landform_rules 和 get_possible_coords()，选择完成后立即释放。
func pick_landform_coord(landform_script: Script, map_data: Dictionary, config: Dictionary) -> Vector2i:
	var owner_battle := config["owner_battle"] as Node
	var buffer = landform_script.new(INVALID_COORD, owner_battle)
	var buffer_pool := _build_candidate_pool(buffer, map_data, config)

	if buffer_pool.is_empty():
		buffer.queue_free()
		return INVALID_COORD

	var coord_index := randi() % buffer_pool.size()
	var test_coord: Vector2i = buffer_pool[coord_index]

	while not buffer_pool.is_empty() and not buffer.get_possible_coords(test_coord, map_data):
		buffer_pool.remove_at(coord_index)
		if buffer_pool.is_empty():
			break
		coord_index = randi() % buffer_pool.size()
		test_coord = buffer_pool[coord_index]

	buffer.queue_free()
	return INVALID_COORD if buffer_pool.is_empty() else test_coord


## 重建高度池，并统计非 nexus_core 的有效地块数。
## tile_h_pool 仍由 GlobalClock 持有；服务只消费 HexMap 传入的引用。
func _rebuild_height_pool(map_data: Dictionary, config: Dictionary) -> int:
	var tile_h_pool := config["tile_h_pool"] as Dictionary
	for h_key in tile_h_pool.keys():
		tile_h_pool[h_key].clear()

	var coords_list := map_data.keys()
	coords_list.shuffle()

	var valid_tile_count := 0
	var terrain_from_height := config["terrain_from_height"] as Callable
	for coord in coords_list:
		var data := map_data[coord] as Dictionary
		if data.has("terrain") and str(data["terrain"]) == TERRAIN_NEXUS_CORE:
			continue

		valid_tile_count += 1
		var height: int = int(data["height"])
		var terrain_type = data["terrain_type"]
		if terrain_type == null:
			terrain_type = terrain_from_height.call(height)
			data["terrain_type"] = terrain_type

		if not tile_h_pool.has(height):
			tile_h_pool[height] = []
		tile_h_pool[height].append(coord)

	return valid_tile_count


## 尝试创建并登记一个地貌实例。
## 分组规则保持旧行为：Enemy 加入 Enemies，Middle 加入 Middle。
func _try_place_landform(landform_script: Script, map_data: Dictionary, config: Dictionary) -> bool:
	var coord := pick_landform_coord(landform_script, map_data, config)
	if coord == INVALID_COORD:
		return false

	var owner_battle := config["owner_battle"] as Node
	var inst = landform_script.new(coord, owner_battle)
	map_data[coord]["landform"] = inst
	map_data[coord]["landform_type"] = inst.landform_name

	if inst.get("Attitude") == inst.Attitude_Pool.Enemy:
		inst.add_to_group("Enemies")
	elif inst.get("Attitude") == inst.Attitude_Pool.Middle:
		inst.add_to_group("Middle")

	return true


## 根据地貌规则建立候选池。
## 有 require_height 时从高度池抽取；没有高度限制时沿用旧行为，把整张 map_data 的 key 作为候选。
func _build_candidate_pool(buffer: Node, map_data: Dictionary, config: Dictionary) -> Array:
	var tile_h_pool := config["tile_h_pool"] as Dictionary
	if buffer.landform_rules.has("require_height") and buffer.landform_rules["require_height"] != null:
		var valid_heights = buffer.landform_rules["require_height"]
		var target_h: int = int(valid_heights[randi() % valid_heights.size()])
		if tile_h_pool.has(target_h):
			return tile_h_pool[target_h].duplicate()
		return []

	return map_data.keys().duplicate()
