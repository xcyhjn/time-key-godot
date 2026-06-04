class_name TileDestructionMutationService
extends RefCounted


## TileDestructionMutationService 负责高度超限后地块的真实删除。
## 批处理节奏仍由 TileDestructionBatchQueue 控制；这个模块只处理单个地块的 VFX、节点释放、数据字典清理和旧静态库同步。


## 执行单个地块的销毁 mutation。
## `stack` 是要销毁的地块节点，`coord` 是该地块在 map_data/stack_nodes 中的坐标 key。
func perform(stack: Area2D, coord: Vector2i, config: Dictionary) -> void:
	var sprites := _get_stack_sprites(stack)
	await _play_destruction_vfx(sprites, config)

	var occupant: Variant = _get_stack_occupant(stack)
	_queue_free_stack(stack)
	_remove_height_pool_entry(coord, config)
	_remove_map_entries(coord, config)
	_refresh_stack_interactivity(config)
	_remove_coord_from_landform_libraries(coord, config)
	_queue_free_occupant(occupant)
	_emit_destruction_signals(config)


## 读取地块当前登记的渲染节点。
## VFXManager 需要这组 sprite 播放抖动和溶解；如果 metadata 不完整，返回空数组让 VFX 安全跳过。
func _get_stack_sprites(stack: Area2D) -> Array:
	if not is_instance_valid(stack) or not stack.has_meta("sprites"):
		return []
	var sprites: Variant = stack.get_meta("sprites")
	if sprites is Array:
		return sprites
	return []


## 播放地块销毁表现。
## 具体特效仍由项目现有 VFXManager 执行，所有时长和强度由 HexMap 的导出变量传入。
func _play_destruction_vfx(sprites: Array, config: Dictionary) -> void:
	var vfx_manager: Variant = config.get("vfx_manager", null)
	var tree: SceneTree = config.get("tree", null)
	if vfx_manager == null or tree == null:
		return

	if not vfx_manager.has_method("play_tile_destruction_vfx"):
		return

	await vfx_manager.play_tile_destruction_vfx(
		sprites,
		tree,
		int(config.get("shake_count", 3)),
		float(config.get("shake_step_duration", 0.015)),
		float(config.get("shake_distance", 15.0)),
		float(config.get("dissolve_duration", 0.25))
	)


## 取出地块上的 occupant。
## 地块删除后 occupant 也会一起释放；这里先保存引用，避免 stack queue_free 后读取 metadata 不稳定。
func _get_stack_occupant(stack: Area2D) -> Variant:
	if is_instance_valid(stack) and stack.has_meta("occupant"):
		return stack.get_meta("occupant")
	return null


## 释放地块根节点。
## 这里只排队释放，不立即访问其子节点，保持 Godot 原有 queue_free 时序。
func _queue_free_stack(stack: Area2D) -> void:
	if is_instance_valid(stack):
		stack.queue_free()


## 从 GlobalClock.tile_h_pool 中移除被销毁地块坐标。
## 高度优先从 map_data 读取，保证使用销毁前最新的视觉高度。
func _remove_height_pool_entry(coord: Vector2i, config: Dictionary) -> void:
	var map_data: Dictionary = config.get("map_data", {})
	var destroyed_height := 0
	if map_data.has(coord) and typeof(map_data[coord]) == TYPE_DICTIONARY:
		destroyed_height = int(map_data[coord].get("height", 0))

	var tile_h_pool: Dictionary = config.get("tile_h_pool", {})
	if tile_h_pool.has(destroyed_height):
		tile_h_pool[destroyed_height].erase(coord)


## 从 HexMap 的核心数据表中删除地块。
## `stack_nodes` 和 `map_data` 是后续寻路、目标规则、奖励和高度同步共同依赖的数据源，必须一起清理。
func _remove_map_entries(coord: Vector2i, config: Dictionary) -> void:
	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	stack_nodes.erase(coord)

	var map_data: Dictionary = config.get("map_data", {})
	map_data.erase(coord)


## 通知 HexMap 重新计算地块输入状态。
## 地块删除后，结算奖励模式、普通 hover 和卡牌选取态都需要排除这个坐标。
func _refresh_stack_interactivity(config: Dictionary) -> void:
	var refresh_stack_interactivity: Callable = config.get("refresh_stack_interactivity", Callable())
	if refresh_stack_interactivity.is_valid():
		refresh_stack_interactivity.call()


## 清理旧建筑脚本中保存的静态坐标库。
## 当前主要用于 iron_mine 和 village，它们会把已生成坐标存在静态 Library 中。
func _remove_coord_from_landform_libraries(coord: Vector2i, config: Dictionary) -> void:
	var landform_library_holders: Array = config.get("landform_library_holders", [])
	for holder in landform_library_holders:
		if holder != null and "Library" in holder:
			holder.Library.erase(coord)


## 释放被销毁地块上的 occupant。
## 如果 occupant 已经无效或正在释放，则直接跳过。
func _queue_free_occupant(occupant: Variant) -> void:
	if is_instance_valid(occupant):
		occupant.queue_free()


## 发出销毁后需要刷新的场景级信号。
## 信号仍由 HexMap 自己发出，服务只通过回调通知。
func _emit_destruction_signals(config: Dictionary) -> void:
	var emit_enemy_roster_changed: Callable = config.get("emit_enemy_roster_changed", Callable())
	if emit_enemy_roster_changed.is_valid():
		emit_enemy_roster_changed.call()

	var emit_tile_topology_changed: Callable = config.get("emit_tile_topology_changed", Callable())
	if emit_tile_topology_changed.is_valid():
		emit_tile_topology_changed.call()
