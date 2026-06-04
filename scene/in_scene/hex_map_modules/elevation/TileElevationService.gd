class_name TileElevationService
extends RefCounted


## TileElevationService 负责编排单个地块的高度变化流程。
## 它会更新高度数据、补删侧面块、同步平铺视图缓存、驱动升降 Tween，并在高度越界时调用 HexMap 注入的销毁队列。
## 真实地块删除、敌人列表刷新、奖励状态和地图生成仍留在 HexMap，避免高度动画模块拥有过多场景职责。


## 单个地块高度变化的主入口。
## `stack` 是目标地块 Area2D，`delta_height` 是本次高度增减值；所有可调参数和回调都通过 config 传入。
func animate(stack: Area2D, delta_height: int, config: Dictionary) -> void:
	if delta_height == 0 or not is_instance_valid(stack):
		return

	var tree: SceneTree = config.get("tree", null)
	if tree == null:
		return

	while is_instance_valid(stack) and stack.has_meta("is_animating") and stack.get_meta("is_animating"):
		await tree.process_frame

	if not is_instance_valid(stack):
		return
	stack.set_meta("is_animating", true)

	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	var coord_variant: Variant = stack_nodes.find_key(stack)
	if coord_variant == null:
		stack.set_meta("is_animating", false)
		return

	var coord: Vector2i = coord_variant
	var old_height: int = int(stack.get_meta("height"))
	var new_height: int = old_height + delta_height
	var max_height := int(config.get("max_height", 6))
	var min_height := int(config.get("min_height", 0))
	var should_destroy_after_elevation := new_height > max_height or new_height <= min_height
	var visual_height := _get_visual_height(new_height, min_height, should_destroy_after_elevation)
	var actual_delta := visual_height - old_height

	if actual_delta == 0:
		stack.set_meta("is_animating", false)
		if should_destroy_after_elevation:
			await _queue_tile_destruction(stack, coord, config)
		return

	_apply_height_data(stack, coord, old_height, visual_height, config)

	if bool(config.get("is_flat_view", false)):
		await _animate_flat_view(stack, coord, old_height, visual_height, actual_delta, should_destroy_after_elevation, config)
		return

	await _animate_3d_view(stack, coord, old_height, visual_height, actual_delta, should_destroy_after_elevation, config)


## 根据真实目标高度计算本轮先展示的高度。
## 低于下限时旧逻辑会先显示为 1 层，再交给销毁队列；超过上限时保留超出的视觉高度，方便表现崩塌前的升高。
func _get_visual_height(new_height: int, min_height: int, should_destroy_after_elevation: bool) -> int:
	if should_destroy_after_elevation and new_height <= min_height:
		return 1
	return clampi(new_height, 1, 99)


## 统一写入地块高度数据和全局高度池。
## 这里保持旧数据结构不变：stack metadata、map_data[coord].height 和 GlobalClock.tile_h_pool 同步更新。
func _apply_height_data(stack: Area2D, coord: Vector2i, old_height: int, visual_height: int, config: Dictionary) -> void:
	stack.set_meta("height", visual_height)

	var map_data: Dictionary = config.get("map_data", {})
	if map_data.has(coord) and typeof(map_data[coord]) == TYPE_DICTIONARY:
		map_data[coord]["height"] = visual_height

	var tile_h_pool: Dictionary = config.get("tile_h_pool", {})
	if tile_h_pool.has(old_height):
		tile_h_pool[old_height].erase(coord)
	if not tile_h_pool.has(visual_height):
		tile_h_pool[visual_height] = []
	tile_h_pool[visual_height].append(coord)


## 平铺高度视图下的升降流程。
## 这一分支不播放整柱 3D 移动，只更新后台 3D 缓存、侧面块队列和高度数字反馈。
func _animate_flat_view(
	stack: Area2D,
	coord: Vector2i,
	old_height: int,
	visual_height: int,
	actual_delta: int,
	should_destroy_after_elevation: bool,
	config: Dictionary
) -> void:
	var sprites := _get_stack_sprites(stack)
	var cached_data: Variant = _get_flat_cache(stack, config)
	var side_tex: Texture2D = _get_side_texture(coord, config)
	var current_step_h := _get_current_step_height(config)
	var delta_y := -(actual_delta * float(config.get("elevation_move_distance", 48.0)))

	_shift_flat_cache_after_height_delta(cached_data, delta_y)

	if actual_delta > 0:
		_add_flat_side_sprites(stack, sprites, cached_data, side_tex, actual_delta, current_step_h, delta_y, config)
	elif actual_delta < 0:
		_remove_bottom_sprites(sprites, abs(actual_delta), cached_data)

	_refresh_sprite_shader_indices(sprites, visual_height)
	await _animate_height_label(stack, visual_height, config)

	if is_instance_valid(stack):
		stack.set_meta("is_animating", false)
	if should_destroy_after_elevation:
		await _queue_tile_destruction(stack, coord, config)


## 3D 视图下的升降流程。
## 它先移动整组可见对象，再在 Tween 回调中补删底部侧面块，并最终按需进入销毁队列。
func _animate_3d_view(
	stack: Area2D,
	coord: Vector2i,
	old_height: int,
	visual_height: int,
	actual_delta: int,
	should_destroy_after_elevation: bool,
	config: Dictionary
) -> void:
	var sprites := _get_stack_sprites(stack)
	var side_tex: Texture2D = _get_side_texture(coord, config)
	var current_step_h := _get_current_step_height(config)
	var moving_parts := _collect_3d_moving_parts(stack, sprites, config)
	var y_offset_movement := -(actual_delta * current_step_h)
	var original_positions := _capture_original_positions(moving_parts)
	var tween := _create_elevation_tween(config)

	if tween == null:
		_finish_3d_sprite_mutation(stack, sprites, side_tex, actual_delta, visual_height, current_step_h, config)
	else:
		_schedule_3d_movement(tween, moving_parts, original_positions, y_offset_movement, config)
		tween.chain().tween_callback(func() -> void:
			_finish_3d_sprite_mutation(stack, sprites, side_tex, actual_delta, visual_height, current_step_h, config)
		)
		await tween.finished

	if is_instance_valid(stack):
		stack.set_meta("is_animating", false)
	if should_destroy_after_elevation:
		await _queue_tile_destruction(stack, coord, config)
		return
	_emit_tile_topology_changed(config)


## 读取地块的渲染节点队列。
## 如果 metadata 不存在或类型不正确，则返回空数组，避免后续补删节点时触发解析错误。
func _get_stack_sprites(stack: Area2D) -> Array:
	if not is_instance_valid(stack) or not stack.has_meta("sprites"):
		return []
	var sprites: Variant = stack.get_meta("sprites")
	if sprites is Array:
		return sprites
	return []


## 读取当前平铺视图缓存。
## 缓存字典仍由 HexMap 持有，这里只按 stack 取出需要更新的那一份。
func _get_flat_cache(stack: Area2D, config: Dictionary) -> Variant:
	var caches: Dictionary = config.get("height_view_original_materials", {})
	if caches.has(stack):
		return caches[stack]
	return null


## 按地块坐标取当前地形的侧面贴图。
## 贴图解析通过 HexMap 注入回调完成，避免服务直接依赖 terrain enum 或资源数组。
func _get_side_texture(coord: Vector2i, config: Dictionary) -> Texture2D:
	var map_data: Dictionary = config.get("map_data", {})
	var terrain_type: Variant = null
	if map_data.has(coord) and typeof(map_data[coord]) == TYPE_DICTIONARY:
		terrain_type = map_data[coord].get("terrain_type", null)

	var get_side_tex: Callable = config.get("get_side_tex", Callable())
	if get_side_tex.is_valid():
		var result: Variant = get_side_tex.call(terrain_type)
		if result is Texture2D:
			return result
	return null


## 计算当前侧面块垂直间距。
## 公式保持旧逻辑：`filler_block_spacing * tile_scale / REF_SCALE`。
func _get_current_step_height(config: Dictionary) -> float:
	var spacing := float(config.get("filler_block_spacing", 48.0))
	var tile_scale := float(config.get("tile_scale", 0.6))
	var ref_scale := float(config.get("ref_scale", 0.6))
	return spacing * (tile_scale / ref_scale)


## 平铺视图高度变化后，先把已有缓存的原始 3D y 值整体移动。
## 这样切回 3D 时，地貌、碰撞箱和血条仍能保持各自原来的局部偏移。
func _shift_flat_cache_after_height_delta(cached_data: Variant, delta_y: float) -> void:
	if typeof(cached_data) != TYPE_DICTIONARY:
		return

	var sprites_data: Array = cached_data.get("sprites_data", [])
	for sprite_data in sprites_data:
		if typeof(sprite_data) == TYPE_DICTIONARY:
			var original_position: Vector2 = sprite_data.get("original_position", Vector2.ZERO)
			original_position.y += delta_y
			sprite_data["original_position"] = original_position

	_shift_cached_node_position(cached_data.get("occupant_data", null), delta_y)
	_shift_cached_node_position(cached_data.get("collision_data", null), delta_y)
	_shift_cached_node_position(cached_data.get("health_bar_data", null), delta_y)


## 移动单个缓存节点的原始位置。
## occupant、collision 和 health_bar 三类缓存结构相同，因此用这一个小函数统一处理。
func _shift_cached_node_position(node_data: Variant, delta_y: float) -> void:
	if typeof(node_data) != TYPE_DICTIONARY:
		return
	var original_position: Vector2 = node_data.get("original_position", Vector2.ZERO)
	original_position.y += delta_y
	node_data["original_position"] = original_position


## 平铺视图升高时补入新的底部侧面块。
## 新块保持不可见且标记为平铺视图状态，只写入缓存，等待后续切回 3D 时恢复正确位置。
func _add_flat_side_sprites(
	stack: Area2D,
	sprites: Array,
	cached_data: Variant,
	side_tex: Texture2D,
	actual_delta: int,
	current_step_h: float,
	delta_y: float,
	config: Dictionary
) -> void:
	var orig_bottom_y := 0.0
	if typeof(cached_data) == TYPE_DICTIONARY:
		var sprites_data: Array = cached_data.get("sprites_data", [])
		if not sprites_data.is_empty() and typeof(sprites_data[0]) == TYPE_DICTIONARY:
			var first_original_position: Vector2 = sprites_data[0].get("original_position", Vector2.ZERO)
			orig_bottom_y = first_original_position.y - delta_y

	for k in range(actual_delta):
		var new_sprite := _create_side_sprite(side_tex, config)
		new_sprite.visible = false
		new_sprite.modulate.a = 0.0
		new_sprite.position.y = 0.0
		_set_flat_shader_state(new_sprite, true)

		stack.add_child(new_sprite)
		stack.move_child(new_sprite, k)
		sprites.insert(k, new_sprite)

		if typeof(cached_data) == TYPE_DICTIONARY:
			var sprites_data: Array = cached_data.get("sprites_data", [])
			var new_3d_y := orig_bottom_y - (k * current_step_h)
			sprites_data.insert(k, {
				"sprite": new_sprite,
				"original_material": new_sprite.material,
				"original_position": Vector2(new_sprite.position.x, new_3d_y)
			})
			cached_data["sprites_data"] = sprites_data


## 从底部移除侧面块。
## 至少保留顶部块，和旧逻辑保持一致；平铺缓存存在时同步弹出对应缓存项。
func _remove_bottom_sprites(sprites: Array, remove_count: int, cached_data: Variant) -> void:
	for i in range(remove_count):
		if sprites.size() <= 1:
			return

		var bottom_sprite: Variant = sprites[0]
		sprites.pop_front()
		if is_instance_valid(bottom_sprite):
			bottom_sprite.queue_free()

		if typeof(cached_data) == TYPE_DICTIONARY:
			var sprites_data: Array = cached_data.get("sprites_data", [])
			if not sprites_data.is_empty():
				sprites_data.pop_front()
			cached_data["sprites_data"] = sprites_data


## 更新所有渲染节点的高度 shader 下标。
## 地块升降后 block_idx 和 total_height 必须重新写入，否则 hover、遮挡和平铺 shader 会读到旧高度。
func _refresh_sprite_shader_indices(sprites: Array, visual_height: int) -> void:
	for i in range(sprites.size()):
		var item := sprites[i] as CanvasItem
		if is_instance_valid(item) and item.material:
			item.set_instance_shader_parameter("block_idx", float(i))
			item.set_instance_shader_parameter("total_height", float(visual_height))


## 播放平铺视图高度数字变化反馈。
## 具体 label 样式和弹跳时长由 HexMap 的指示器配置回调提供。
func _animate_height_label(stack: Area2D, visual_height: int, config: Dictionary) -> void:
	var animate_label_height: Callable = config.get("animate_label_height", Callable())
	if animate_label_height.is_valid():
		await animate_label_height.call(stack, visual_height)


## 收集 3D 视图中需要跟着顶部地块整体移动的对象。
## 包括地块贴图、碰撞箱、occupant，以及没有挂在地块内部的外部血条节点。
func _collect_3d_moving_parts(stack: Area2D, sprites: Array, config: Dictionary) -> Array:
	var moving_parts: Array = []
	moving_parts.append_array(sprites)

	var collision: Variant = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
	if is_instance_valid(collision):
		moving_parts.append(collision)

	var occupant: Variant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
	if is_instance_valid(occupant):
		moving_parts.append(occupant)

		var get_health_bar_for_occupant: Callable = config.get("get_health_bar_for_occupant", Callable())
		if get_health_bar_for_occupant.is_valid():
			var health_bar: Variant = get_health_bar_for_occupant.call(occupant, moving_parts)
			if is_instance_valid(health_bar):
				moving_parts.append(health_bar)

	return moving_parts


## 保存移动对象的起始位置。
## key 使用 instance_id，和旧实现一致，避免数组中对象顺序变化影响位置恢复。
func _capture_original_positions(moving_parts: Array) -> Dictionary:
	var original_positions := {}
	for part in moving_parts:
		if is_instance_valid(part) and (part is Node2D or part is Control):
			original_positions[part.get_instance_id()] = part.position
	return original_positions


## 创建升降 Tween。
## Tween 必须由 HexMap 注入，因为 RefCounted 自身不在场景树中。
func _create_elevation_tween(config: Dictionary) -> Tween:
	var create_elevation_tween: Callable = config.get("create_elevation_tween", Callable())
	if not create_elevation_tween.is_valid():
		return null
	var result: Variant = create_elevation_tween.call()
	if result is Tween:
		return result
	return null


## 安排 3D 视图中所有对象的震动和最终移动。
## 如果某个节点的父级也在 moving_parts 中，它会跟随父级移动，因此这里不再给它单独 Tween。
func _schedule_3d_movement(
	tween: Tween,
	moving_parts: Array,
	original_positions: Dictionary,
	y_offset_movement: float,
	config: Dictionary
) -> void:
	var shake_intensity := float(config.get("ele_shake_intensity", 6.0))
	var shake_duration := float(config.get("ele_shake_duration", 0.08))
	var anim_duration := float(config.get("ele_anim_duration", 0.15))
	var trans_type: Tween.TransitionType = config.get("ele_trans_type", Tween.TRANS_ELASTIC)
	var ease_type: Tween.EaseType = config.get("ele_ease_type", Tween.EASE_OUT)

	for part in moving_parts:
		if not is_instance_valid(part) or not (part is Node2D or part is Control):
			continue
		if part.get_parent() in moving_parts:
			continue

		var p_id: int = part.get_instance_id()
		if not original_positions.has(p_id):
			continue

		var original_position: Vector2 = original_positions[p_id]
		var target_pos := original_position + Vector2(0, y_offset_movement)
		tween.tween_property(
			part,
			"position:x",
			original_position.x + randf_range(-shake_intensity, shake_intensity),
			shake_duration
		)
		tween.chain().tween_property(part, "position", target_pos, anim_duration).set_trans(trans_type).set_ease(ease_type)


## 3D 视图高度动画结束后补删底部侧面块，并刷新 shader 下标。
## 这一段放在 Tween callback 中执行，确保视觉移动结束后再改变地块层数。
func _finish_3d_sprite_mutation(
	stack: Area2D,
	sprites: Array,
	side_tex: Texture2D,
	actual_delta: int,
	visual_height: int,
	current_step_h: float,
	config: Dictionary
) -> void:
	if actual_delta > 0:
		for k in range(actual_delta):
			var new_sprite := _create_side_sprite(side_tex, config)
			new_sprite.position.y = -(k) * current_step_h
			new_sprite.modulate = Color(1.0, 1.0, 1.0)
			stack.add_child(new_sprite)
			stack.move_child(new_sprite, k)
			sprites.insert(k, new_sprite)
	elif actual_delta < 0:
		_remove_bottom_sprites(sprites, abs(actual_delta), null)

	_refresh_sprite_shader_indices(sprites, visual_height)


## 创建一块新的侧面 Sprite2D。
## 贴图、偏移、缩放和材质复制保持旧 HexMap 实现一致。
func _create_side_sprite(side_tex: Texture2D, config: Dictionary) -> Sprite2D:
	var sprite := Sprite2D.new()
	sprite.texture = side_tex
	sprite.centered = false
	sprite.offset = Vector2(-256, -400)
	sprite.scale = Vector2(
		float(config.get("tile_scale", 0.6)),
		float(config.get("tile_scale", 0.6))
	)

	var block_material: Variant = config.get("block_material", null)
	if block_material is ShaderMaterial:
		sprite.material = block_material.duplicate()

	return sprite


## 设置新建侧面块的平铺视图 shader 状态。
## 这里只写 instance 参数，不替换材质，和现有平铺视图模块保持一致。
func _set_flat_shader_state(item: CanvasItem, enabled: bool) -> void:
	if not is_instance_valid(item) or item.material == null:
		return
	item.set_instance_shader_parameter("is_flat_view", 1.0 if enabled else 0.0)


## 将超限地块交给 HexMap 的销毁队列。
## 队列和真实删除仍在 HexMap/TileDestructionBatchQueue 中，服务只负责在升降表现结束后触发它。
func _queue_tile_destruction(stack: Area2D, coord: Vector2i, config: Dictionary) -> void:
	var queue_tile_destruction: Callable = config.get("queue_tile_destruction", Callable())
	if queue_tile_destruction.is_valid():
		await queue_tile_destruction.call(stack, coord)


## 通知地块拓扑发生变化。
## 这里通过回调发出 HexMap 的 `tile_topology_changed` 信号，避免服务直接持有 HexMap 节点引用。
func _emit_tile_topology_changed(config: Dictionary) -> void:
	var emit_tile_topology_changed: Callable = config.get("emit_tile_topology_changed", Callable())
	if emit_tile_topology_changed.is_valid():
		emit_tile_topology_changed.call()
