class_name HeightViewStateSynchronizer
extends RefCounted


## 计算平铺视图中一个地块栈需要下落的距离。
## 只依赖 stack 高度和传入的 spacing/scale 配置，方便 HexMap 与运行期注册路径复用。
func get_drop_delta(stack: Area2D, config: Dictionary) -> float:
	var height := get_stack_height(stack)
	var spacing := float(config.get("filler_block_spacing", 48.0))
	var tile_scale := float(config.get("tile_scale", 0.6))
	var ref_scale := float(config.get("ref_scale", 0.6))
	return float(height - 1) * spacing * (tile_scale / ref_scale)


## 读取地块高度元数据。
## 视角同步只需要安全高度值，不修改 map_data。
func get_stack_height(stack: Area2D) -> int:
	if not is_instance_valid(stack):
		return 1
	if stack.has_meta("height"):
		return max(1, int(stack.get_meta("height")))
	return 1


## 获取或创建一个地块栈的 3D 原始位置缓存。
## 缓存由 HexMap 持有并通过 config 传入，便于切回 3D 时恢复。
func get_or_create_cache(stack: Area2D, sprites: Array, config: Dictionary) -> Dictionary:
	var caches: Dictionary = config.get("height_view_original_materials", {})
	if caches.has(stack):
		return caches[stack]

	var sprites_data: Array = []
	for sprite in sprites:
		if is_instance_valid(sprite):
			sprites_data.append({"sprite": sprite, "original_position": get_visual_node_position(sprite)})

	var occupant: Variant = stack.get_meta("occupant") if is_instance_valid(stack) and stack.has_meta("occupant") else null
	var occupant_data: Variant = null
	if is_instance_valid(occupant):
		occupant_data = {"node": occupant, "original_position": get_visual_node_position(occupant)}

	var collision: Variant = stack.get_meta("collision_node") if is_instance_valid(stack) and stack.has_meta("collision_node") else null
	var collision_data: Variant = null
	if is_instance_valid(collision):
		collision_data = {"node": collision, "original_position": get_visual_node_position(collision)}

	var health_bar_data: Variant = _build_health_bar_cache_data(occupant, config)
	var cache := {
		"sprites_data": sprites_data,
		"occupant_data": occupant_data,
		"collision_data": collision_data,
		"health_bar_data": health_bar_data
	}
	caches[stack] = cache
	return cache


## 向已有缓存中加入运行期新增的 stack 子贴图。
## 新增建筑或地形块在平铺视图中出现时，需要记录原始 3D y 值。
func get_or_add_sprite_cache(cache: Dictionary, sprite: Node2D) -> Dictionary:
	var sprites_data: Array = cache.get("sprites_data", [])
	for sprite_data in sprites_data:
		if sprite_data.get("sprite") == sprite:
			return sprite_data

	var new_data := {"sprite": sprite, "original_position": sprite.position}
	sprites_data.append(new_data)
	cache["sprites_data"] = sprites_data
	return new_data


## 读取 Node2D 或 Control 的局部位置。
## 血条 UI 可能是 Control，地块/建筑通常是 Node2D。
func get_visual_node_position(node: Node) -> Vector2:
	if node is Node2D:
		return (node as Node2D).position
	if node is Control:
		return (node as Control).position
	return Vector2.ZERO


## 同步一个节点的 y 坐标。
## animate 为 true 时通过 HexMap 传入的 `tween_position_y` 回调走原有 Tween 行为。
func sync_node_position_y(node: Node, target_y: float, animate: bool, config: Dictionary) -> void:
	if not is_instance_valid(node):
		return
	if animate:
		var tween_position_y: Callable = config.get("tween_position_y", Callable())
		if tween_position_y.is_valid():
			tween_position_y.call(node, target_y, float(config.get("position_tween_duration", 0.3)))
		return

	if node is Node2D:
		var node_2d := node as Node2D
		node_2d.position.y = target_y
	elif node is Control:
		var control := node as Control
		control.position.y = target_y


## 将缓存节点同步到平铺视图位置。
## 运行期新出现的 occupant/collision/health_bar 会先补入缓存，再按原始 y + drop_delta 下落。
func sync_cached_node_to_flat(cache: Dictionary, key: String, node: Node, drop_delta: float, animate: bool, config: Dictionary) -> void:
	if not is_instance_valid(node) or not (node is Node2D or node is Control):
		return

	var data: Variant = cache.get(key, null)
	if typeof(data) != TYPE_DICTIONARY or data.get("node") != node:
		data = {"node": node, "original_position": get_visual_node_position(node)}
		cache[key] = data

	var original_position: Vector2 = data.get("original_position", get_visual_node_position(node))
	sync_node_position_y(node, original_position.y + drop_delta, animate, config)


## 将一个地块栈立即同步到当前 3D/平铺视图状态。
## 该函数服务运行期新增实体、血条和地貌刷新；整图切换动画仍由 HexMap 的切换流程调度。
func sync_stack_to_current_view(coord: Vector2i, animate: bool, config: Dictionary) -> void:
	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	if not stack_nodes.has(coord):
		return

	var stack := stack_nodes[coord] as Area2D
	if not is_instance_valid(stack):
		return

	var cleanup_stack_sprites: Callable = config.get("cleanup_stack_sprites", Callable())
	if not cleanup_stack_sprites.is_valid():
		return

	var height := get_stack_height(stack)
	var sprites: Array = cleanup_stack_sprites.call(stack)

	if not bool(config.get("is_flat_view", false)):
		for sprite in sprites:
			var item := sprite as CanvasItem
			if is_instance_valid(item) and item.material:
				item.set_instance_shader_parameter("is_flat_view", 0.0)
		return

	var cache := get_or_create_cache(stack, sprites, config)
	var drop_delta := get_drop_delta(stack, config)

	for i in range(sprites.size()):
		var sprite: Variant = sprites[i]
		var item := sprite as CanvasItem
		if not is_instance_valid(item):
			continue
		if item.material:
			item.set_instance_shader_parameter("is_flat_view", 1.0)

		if i < height - 1:
			item.visible = false
			item.modulate.a = 0.0
			continue

		if item.get_parent() == stack and item is Node2D:
			var sprite_node := item as Node2D
			var sprite_data := get_or_add_sprite_cache(cache, sprite_node)
			var original_position: Vector2 = sprite_data.get("original_position", sprite_node.position)
			sync_node_position_y(sprite_node, original_position.y + drop_delta, animate, config)

	var occupant: Variant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
	if is_instance_valid(occupant):
		sync_cached_node_to_flat(cache, "occupant_data", occupant, drop_delta, animate, config)

		var health_bar := _find_health_bar_for_occupant(occupant, config)
		if is_instance_valid(health_bar):
			sync_cached_node_to_flat(cache, "health_bar_data", health_bar, drop_delta, animate, config)

	var collision: Variant = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
	if is_instance_valid(collision):
		sync_cached_node_to_flat(cache, "collision_data", collision, drop_delta, animate, config)

	var update_reward_tooltip: Callable = config.get("update_reward_tooltip", Callable())
	if update_reward_tooltip.is_valid():
		update_reward_tooltip.call(stack)

	var caches: Dictionary = config.get("height_view_original_materials", {})
	caches[stack] = cache


## 构建血条缓存数据。
## 只有 occupant 是 landform 且能找到血条时才写入 health_bar_data。
func _build_health_bar_cache_data(occupant: Variant, config: Dictionary) -> Variant:
	var health_bar := _find_health_bar_for_occupant(occupant, config)
	if is_instance_valid(health_bar):
		return {"node": health_bar, "original_position": get_visual_node_position(health_bar)}
	return null


## 根据 occupant 查找对应血条。
## 具体查找逻辑由 HexMap 以 Callable 注入，避免同步器直接依赖 BarManager。
func _find_health_bar_for_occupant(occupant: Variant, config: Dictionary) -> Node:
	if not (occupant is landform):
		return null
	var find_health_bar: Callable = config.get("find_health_bar_for_landform", Callable())
	if not find_health_bar.is_valid():
		return null
	var result: Variant = find_health_bar.call(occupant)
	if result is Node and is_instance_valid(result):
		return result
	return null
