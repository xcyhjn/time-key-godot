class_name HeightViewMapTransitionRunner
extends RefCounted


## 把整张战斗地图压平成平铺高度视图。
## 这个入口只编排全图遍历、缓存写入、侧面块淡出和高度指示器创建；高度数据本身仍由 HexMap 管理。
func compress_to_flat(config: Dictionary) -> void:
	var caches: Dictionary = config.get("height_view_original_materials", {})
	caches.clear()

	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	for coord in stack_nodes.keys():
		var stack := stack_nodes[coord] as Area2D
		if not is_instance_valid(stack):
			continue
		_compress_stack_to_flat(stack, config)


## 把整张战斗地图从平铺高度视图恢复到 3D 视图。
## 恢复时读取进入平铺视图前记录的原始位置缓存，并负责侧面块淡入和高度指示器移除。
func restore_to_3d(config: Dictionary) -> void:
	var stack_nodes: Dictionary = config.get("stack_nodes", {})
	for coord in stack_nodes.keys():
		var stack := stack_nodes[coord] as Area2D
		if not is_instance_valid(stack):
			continue
		_restore_stack_to_3d(stack, config)

	var caches: Dictionary = config.get("height_view_original_materials", {})
	caches.clear()


## 压平单个地块栈。
## 这里会先记录 sprite、occupant、collision 和 health_bar 的原始位置，再播放当前地块的下落与隐藏动画。
func _compress_stack_to_flat(stack: Area2D, config: Dictionary) -> void:
	var sprites := _cleanup_stack_sprites(stack, config)
	var height := _get_stack_height(stack, config)
	var cache := _build_stack_cache(stack, sprites, config)
	var caches: Dictionary = config.get("height_view_original_materials", {})
	caches[stack] = cache

	var drop_delta := _get_drop_delta(height, config)
	for i in range(sprites.size()):
		var item := sprites[i] as CanvasItem
		if not is_instance_valid(item):
			continue
		_set_flat_shader_state(item, true)

		if i < height - 1:
			_fade_side_sprite_out(item, config)
			continue

		if item.get_parent() == stack:
			_tween_node_y(item, get_visual_node_position(item).y + drop_delta, config)

	_tween_cached_node_to_flat(cache.get("occupant_data"), drop_delta, config)
	_tween_cached_node_to_flat(cache.get("collision_data"), drop_delta, config)
	_tween_cached_node_to_flat(cache.get("health_bar_data"), drop_delta, config)
	_create_height_indicator(stack, height, config)


## 恢复单个地块栈。
## 如果该地块没有缓存，也仍然会恢复侧面块可见性并移除高度指示器，避免切换中途新增地块留下残影。
func _restore_stack_to_3d(stack: Area2D, config: Dictionary) -> void:
	var caches: Dictionary = config.get("height_view_original_materials", {})
	if caches.has(stack):
		var cache: Dictionary = caches[stack]
		_restore_cached_sprites(stack, cache, config)
		_restore_cached_node(cache.get("occupant_data"), config)
		_restore_cached_node(cache.get("collision_data"), config)
		_restore_cached_node(cache.get("health_bar_data"), config)

	_restore_side_sprites(stack, config)
	_remove_height_indicator(stack, config)


## 构建一个地块栈的恢复缓存。
## 缓存结构沿用旧 HexMap 字典字段，方便运行期单地块同步继续读取同一份数据。
func _build_stack_cache(stack: Area2D, sprites: Array, config: Dictionary) -> Dictionary:
	var sprites_data: Array = []
	for sprite in sprites:
		var item := sprite as CanvasItem
		if is_instance_valid(item):
			sprites_data.append({"sprite": item, "original_position": get_visual_node_position(item)})
			_set_flat_shader_state(item, true)

	var occupant: Variant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
	var occupant_data: Variant = null
	if is_instance_valid(occupant):
		occupant_data = {"node": occupant, "original_position": get_visual_node_position(occupant)}

	var collision: Variant = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
	var collision_data: Variant = null
	if is_instance_valid(collision):
		collision_data = {"node": collision, "original_position": get_visual_node_position(collision)}

	var health_bar := _find_health_bar_for_occupant(occupant, config)
	var health_bar_data: Variant = null
	if is_instance_valid(health_bar):
		health_bar_data = {"node": health_bar, "original_position": get_visual_node_position(health_bar)}

	return {
		"sprites_data": sprites_data,
		"occupant_data": occupant_data,
		"collision_data": collision_data,
		"health_bar_data": health_bar_data
	}


## 恢复缓存中的地块 sprite。
## 只有仍挂在原地块下的 sprite 才会移动回原位，外部 UI 节点只恢复 shader 平铺状态。
func _restore_cached_sprites(stack: Area2D, cache: Dictionary, config: Dictionary) -> void:
	var sprites_data: Array = cache.get("sprites_data", [])
	for sprite_data in sprites_data:
		var item: Variant = sprite_data.get("sprite")
		if not is_instance_valid(item):
			continue
		_set_flat_shader_state(item, false)
		if item.get_parent() == stack:
			var original_position: Vector2 = sprite_data.get("original_position", get_visual_node_position(item))
			_tween_node_y(item, original_position.y, config)


## 把缓存中的 occupant、collision 或 health_bar 移回进入平铺视图前的位置。
## data 为 null 时直接跳过，保持旧逻辑里“没有外部节点就不处理”的行为。
func _restore_cached_node(data: Variant, config: Dictionary) -> void:
	if typeof(data) != TYPE_DICTIONARY:
		return
	var node: Variant = data.get("node")
	if not is_instance_valid(node):
		return
	var original_position: Vector2 = data.get("original_position", get_visual_node_position(node))
	_tween_node_y(node, original_position.y, config)


## 把缓存节点移动到平铺视图位置。
## 该函数用于 occupant、collision 和 health_bar，让它们和顶部地块一起下落。
func _tween_cached_node_to_flat(data: Variant, drop_delta: float, config: Dictionary) -> void:
	if typeof(data) != TYPE_DICTIONARY:
		return
	var node: Variant = data.get("node")
	if not is_instance_valid(node):
		return
	var original_position: Vector2 = data.get("original_position", get_visual_node_position(node))
	_tween_node_y(node, original_position.y + drop_delta, config)


## 恢复一个地块栈的侧面块可见性。
## 侧面块数量按当前高度重新判断，兼容切换过程中高度发生变化的情况。
func _restore_side_sprites(stack: Area2D, config: Dictionary) -> void:
	var sprites := _cleanup_stack_sprites(stack, config)
	var height := _get_stack_height(stack, config)
	for i in range(sprites.size()):
		var item := sprites[i] as CanvasItem
		if is_instance_valid(item) and i < height - 1:
			item.visible = true
			_fade_side_sprite_in(item, config)


## 读取并清理一个地块栈的渲染 sprite 列表。
## 具体收集规则仍由 HexMap 注入，避免这个模块知道地貌、血条或状态图标的细节。
func _cleanup_stack_sprites(stack: Area2D, config: Dictionary) -> Array:
	var cleanup_stack_sprites: Callable = config.get("cleanup_stack_sprites", Callable())
	if not cleanup_stack_sprites.is_valid():
		return []
	var result: Variant = cleanup_stack_sprites.call(stack)
	if result is Array:
		return result
	return []


## 读取地块高度。
## 优先使用 HexMap 注入的高度读取函数，缺失时再回退到 stack metadata。
func _get_stack_height(stack: Area2D, config: Dictionary) -> int:
	var get_stack_height: Callable = config.get("get_stack_height", Callable())
	if get_stack_height.is_valid():
		var result: Variant = get_stack_height.call(stack)
		if typeof(result) == TYPE_INT or typeof(result) == TYPE_FLOAT:
			return max(1, int(result))

	if is_instance_valid(stack) and stack.has_meta("height"):
		return max(1, int(stack.get_meta("height")))
	return 1


## 根据高度和配置计算平铺视图需要下落的距离。
## 公式保持和旧 HexMap 实现一致：`(height - 1) * filler_block_spacing * tile_scale / REF_SCALE`。
func _get_drop_delta(height: int, config: Dictionary) -> float:
	var spacing := float(config.get("filler_block_spacing", 48.0))
	var tile_scale := float(config.get("tile_scale", 0.6))
	var ref_scale := float(config.get("ref_scale", 0.6))
	return float(height - 1) * spacing * (tile_scale / ref_scale)


## 读取 Node2D 或 Control 的局部位置。
## 全图过渡需要同时处理地块贴图、实体节点和 UI 血条。
func get_visual_node_position(node: Node) -> Vector2:
	if node is Node2D:
		return (node as Node2D).position
	if node is Control:
		return (node as Control).position
	return Vector2.ZERO


## 设置一个渲染节点是否处于平铺视图 shader 状态。
## 这里只写 `is_flat_view` instance 参数，不替换材质。
func _set_flat_shader_state(item: CanvasItem, enabled: bool) -> void:
	if not is_instance_valid(item) or item.material == null:
		return
	item.set_instance_shader_parameter("is_flat_view", 1.0 if enabled else 0.0)


## 把侧面块淡出并在动画结束后隐藏。
## Tween 由 HexMap 注入创建，保证动画仍挂在场景树中的 HexMap 节点下。
func _fade_side_sprite_out(item: CanvasItem, config: Dictionary) -> void:
	var tw := _create_tween(config)
	if tw == null:
		item.modulate.a = 0.0
		item.visible = false
		return
	tw.tween_property(item, "modulate:a", 0.0, float(config.get("side_fade_duration", 0.3)))
	tw.tween_callback(func() -> void:
		if is_instance_valid(item):
			item.visible = false
	)


## 把侧面块淡入。
## 如果无法创建 Tween，则直接恢复透明度，避免节点卡在不可见状态。
func _fade_side_sprite_in(item: CanvasItem, config: Dictionary) -> void:
	var tw := _create_tween(config)
	if tw == null:
		item.modulate.a = 1.0
		return
	tw.tween_property(item, "modulate:a", 1.0, float(config.get("side_fade_duration", 0.3)))


## 移动一个 Node2D 或 Control 的 y 坐标。
## 优先使用 HexMap 的 `_tween_position_y()` 回调，缺失时直接写入位置。
func _tween_node_y(node: Node, target_y: float, config: Dictionary) -> void:
	if not is_instance_valid(node):
		return
	var tween_position_y: Callable = config.get("tween_position_y", Callable())
	if tween_position_y.is_valid():
		tween_position_y.call(node, target_y, float(config.get("position_tween_duration", 0.3)))
		return
	if node is Node2D:
		(node as Node2D).position.y = target_y
	elif node is Control:
		(node as Control).position.y = target_y


## 通过 HexMap 注入的 create_tween 创建 Tween。
## RefCounted 自身不在场景树中，因此不能直接调用 Node.create_tween()。
func _create_tween(config: Dictionary) -> Tween:
	var create_tween_callback: Callable = config.get("create_tween", Callable())
	if not create_tween_callback.is_valid():
		return null
	var result: Variant = create_tween_callback.call()
	if result is Tween:
		return result
	return null


## 为地块创建高度指示器。
## 光柱和数字的具体样式仍由 HeightViewIndicatorPresenter 负责。
func _create_height_indicator(stack: Area2D, height: int, config: Dictionary) -> void:
	var create_height_indicator: Callable = config.get("create_height_indicator", Callable())
	if create_height_indicator.is_valid():
		create_height_indicator.call(stack, height)


## 移除地块高度指示器。
## 这个回调把全图恢复循环和具体 presenter 解耦。
func _remove_height_indicator(stack: Area2D, config: Dictionary) -> void:
	var remove_height_indicator: Callable = config.get("remove_height_indicator", Callable())
	if remove_height_indicator.is_valid():
		remove_height_indicator.call(stack)


## 查找 occupant 对应的血条节点。
## 具体命名规则和 BarManager 查找仍由 HexMap 注入，避免模块直接依赖场景节点路径。
func _find_health_bar_for_occupant(occupant: Variant, config: Dictionary) -> Node:
	var find_health_bar: Callable = config.get("find_health_bar_for_occupant", Callable())
	if not find_health_bar.is_valid() or not is_instance_valid(occupant):
		return null
	var result: Variant = find_health_bar.call(occupant)
	if result is Node and is_instance_valid(result):
		return result
	return null
