class_name RuntimeLandformRegistrar
extends RefCounted


## 注册一个运行期地貌实体。
## 该函数只负责 map_data 写入、实体挂接、分组和视觉贴图收编；信号/VFX/交互刷新仍由 HexMap 调用方处理。
func register_landform(coord: Vector2i, entity: landform, landform_type: String, context: Dictionary) -> Dictionary:
	var result := _make_result(false, null, false)
	if not is_instance_valid(entity):
		return result

	var stack_nodes: Dictionary = context.get("stack_nodes", {})
	var map_data: Dictionary = context.get("map_data", {})
	if not stack_nodes.has(coord) or not map_data.has(coord):
		return result

	var stack := stack_nodes[coord] as Area2D
	if not is_instance_valid(stack):
		return result

	var resolved_type := _resolve_landform_type(entity, landform_type)
	_write_map_data(coord, entity, resolved_type, map_data)
	var tile_data: Dictionary = map_data[coord]
	attach_entity_to_stack(coord, entity, stack, context)
	apply_landform_group(entity)
	_attach_visual_sprite(coord, entity, stack, tile_data, context)

	result["success"] = true
	result["stack"] = stack
	result["is_enemy"] = _is_enemy_landform(entity)
	return result


## 兼容旧路径刷新指定坐标的地貌视觉。
## 旧代码可能先手动写 map_data 再调用 `add_landform_visual_at()`；这里保留这种入口。
func refresh_visual_at(coord: Vector2i, context: Dictionary) -> Dictionary:
	var result := _make_result(false, null, false)
	var stack_nodes: Dictionary = context.get("stack_nodes", {})
	var map_data: Dictionary = context.get("map_data", {})
	if not stack_nodes.has(coord) or not map_data.has(coord):
		return result

	var stack := stack_nodes[coord] as Area2D
	if not is_instance_valid(stack):
		return result

	var data: Variant = map_data[coord]
	if typeof(data) != TYPE_DICTIONARY:
		return result

	var tile_data := data as Dictionary
	var entity := tile_data.get("landform") as landform
	if not is_instance_valid(entity):
		return result

	if not tile_data.has("landform_in") or not is_instance_valid(tile_data["landform_in"]):
		tile_data["landform_in"] = entity
	if not tile_data.has("landform_type") or str(tile_data.get("landform_type", "")) == "":
		tile_data["landform_type"] = entity.landform_name
	map_data[coord] = tile_data

	attach_entity_to_stack(coord, entity, stack, context)
	apply_landform_group(entity)
	_attach_visual_sprite(coord, entity, stack, tile_data, context)

	result["success"] = true
	result["stack"] = stack
	result["is_enemy"] = _is_enemy_landform(entity)
	return result


## 把逻辑实体挂到地块栈下，并重算它的 3D 基准坐标。
## 平铺视图下落不在这里处理；HexMap 之后会调用 `_sync_stack_to_current_view()`。
func attach_entity_to_stack(coord: Vector2i, entity: landform, stack: Area2D, context: Dictionary) -> void:
	if not is_instance_valid(entity) or not is_instance_valid(stack):
		return

	var height := _get_stack_height(stack)
	var current_step_h := float(context.get("step_height", 48.0)) * (float(context.get("tile_scale", 0.6)) / float(context.get("ref_scale", 0.6)))
	var top_block_y := -float(height - 1) * current_step_h

	entity.owner_battle = context.get("owner_battle", null)
	entity.location = coord
	entity.target = coord
	entity.position = Vector2(
		float(context.get("hitbox_offset_x", 0.0)),
		top_block_y + float(context.get("hitbox_offset_y", -110.0))
	) + (context.get("landform_instance_offset", Vector2.ZERO) as Vector2)

	var old_parent := entity.get_parent()
	if old_parent != stack:
		if is_instance_valid(old_parent):
			old_parent.remove_child(entity)
		stack.add_child(entity)

	stack.set_meta("occupant", entity)


## 统一处理运行期实体阵营分组。
## 这里只添加目标分组，不移除旧分组，保持旧逻辑的低风险行为。
func apply_landform_group(entity: landform) -> void:
	if not is_instance_valid(entity):
		return
	if _is_enemy_landform(entity):
		if not entity.is_in_group("Enemies"):
			entity.add_to_group("Enemies")
	else:
		if not entity.is_in_group("Middle"):
			entity.add_to_group("Middle")


## 重扫一个地块栈中的可渲染节点。
## 运行期新增建筑、虚影和血条可能绕过旧 sprites 缓存，因此刷新视觉后需要统一收编。
func cleanup_stack_sprites(stack: Area2D) -> Array:
	var cleaned: Array = []
	for sprite in _get_stack_sprites(stack):
		_append_stack_render_sprite(cleaned, sprite)

	if is_instance_valid(stack):
		for child in stack.get_children():
			if child is Sprite2D:
				_append_stack_render_sprite(cleaned, child)

	var occupant: Variant = stack.get_meta("occupant") if is_instance_valid(stack) and stack.has_meta("occupant") else null
	if occupant is landform:
		var occupant_landform := occupant as landform
		_append_stack_render_sprite(cleaned, occupant_landform.tex)
		for child in occupant_landform.get_children():
			if child is Sprite2D:
				_append_stack_render_sprite(cleaned, child)

	if is_instance_valid(stack):
		stack.set_meta("sprites", cleaned)
	return cleaned


## 读取地块高度元数据。
## 运行期注册只需要安全高度值，用于实体锚点和 shader 参数。
func _get_stack_height(stack: Area2D) -> int:
	if not is_instance_valid(stack):
		return 1
	if stack.has_meta("height"):
		return max(1, int(stack.get_meta("height")))
	return 1


## 从 stack meta 中安全读取 sprites。
## meta 不存在或类型不对时返回空数组，避免注册路径抛错。
func _get_stack_sprites(stack: Area2D) -> Array:
	if not is_instance_valid(stack) or not stack.has_meta("sprites"):
		return []
	var stored_sprites: Variant = stack.get_meta("sprites")
	if stored_sprites is Array:
		return stored_sprites
	return []


## 将一个渲染节点加入列表。
## 过滤状态图标、无效节点和重复节点，保持旧 `_cleanup_stack_sprites()` 的行为。
func _append_stack_render_sprite(list: Array, sprite: Variant) -> void:
	if not is_instance_valid(sprite):
		return
	if not (sprite is Node and sprite is CanvasItem):
		return
	if (sprite as Node).name.begins_with("StatusIcon_"):
		return
	if (sprite as Node).is_queued_for_deletion():
		return
	if list.has(sprite):
		return
	list.append(sprite)


## 写入 map_data 中和地貌相关的字段。
## 这里直接修改传入 Dictionary，调用方继续持有同一个 map_data 引用。
func _write_map_data(coord: Vector2i, entity: landform, resolved_type: String, map_data: Dictionary) -> void:
	var tile_data: Dictionary = map_data[coord]
	tile_data["landform"] = entity
	tile_data["landform_in"] = entity
	tile_data["landform_type"] = resolved_type
	map_data[coord] = tile_data


## 调用地貌自己的 attach_visual，并应用 HexMap block shader 参数。
## attach_visual 仍是 landform 的扩展点，本模块只统一后处理材质和 sprites 元数据。
func _attach_visual_sprite(coord: Vector2i, entity: landform, stack: Area2D, tile_data: Dictionary, context: Dictionary) -> void:
	var height := int(tile_data.get("height", _get_stack_height(stack)))
	var tile_scale := float(context.get("tile_scale", 0.6))
	var current_step_h := float(context.get("step_height", 48.0)) * (tile_scale / float(context.get("ref_scale", 0.6)))
	entity.attach_visual(stack, height, current_step_h, tile_scale)

	var landform_sprite := _resolve_landform_sprite(coord, entity, stack)
	if is_instance_valid(landform_sprite):
		_apply_landform_sprite_shader(landform_sprite, height, context)
		var sprites := cleanup_stack_sprites(stack)
		if not sprites.has(landform_sprite):
			sprites.append(landform_sprite)
			stack.set_meta("sprites", sprites)


## 解析本次 attach_visual 生成的 Sprite。
## 优先使用 landform.tex，若为空则回退到旧命名约定 `LandformSprite_x_y`。
func _resolve_landform_sprite(coord: Vector2i, entity: landform, stack: Area2D) -> Sprite2D:
	var landform_sprite := entity.tex as Sprite2D
	if is_instance_valid(landform_sprite):
		return landform_sprite
	var unique_name := "LandformSprite_%s_%s" % [coord.x, coord.y]
	return stack.get_node_or_null(unique_name) as Sprite2D


## 给地貌 sprite 应用 HexMap block shader 参数。
## 建筑位于地形之上，因此 block_idx/total_height 继续沿用旧的 height + 1 / height + 2。
func _apply_landform_sprite_shader(sprite: Sprite2D, height: int, context: Dictionary) -> void:
	var block_material := context.get("block_material", null) as ShaderMaterial
	if block_material == null:
		return

	sprite.material = block_material.duplicate()
	sprite.set_instance_shader_parameter("block_idx", float(height + 1))
	sprite.set_instance_shader_parameter("total_height", float(height + 2))
	if bool(context.get("is_flat_view", false)):
		sprite.set_instance_shader_parameter("is_flat_view", 1.0)


## 解析传入的地貌类型。
## 空字符串沿用实体自己的 `landform_name`，兼容建造卡和旧地貌脚本。
func _resolve_landform_type(entity: landform, landform_type: String) -> String:
	if landform_type != "":
		return landform_type
	return entity.landform_name


## 判断实体阵营是否为敌方。
## 该结果会返回给 HexMap，让主脚本决定是否 emit `enemy_roster_changed`。
func _is_enemy_landform(entity: landform) -> bool:
	return is_instance_valid(entity) and entity.Attitude == entity.Attitude_Pool.Enemy


## 构造统一返回值。
## `stack` 与 `is_enemy` 供 HexMap 做后续视角同步、信号和交互刷新。
func _make_result(success: bool, stack: Area2D, is_enemy: bool) -> Dictionary:
	return {
		"success": success,
		"stack": stack,
		"is_enemy": is_enemy,
	}
