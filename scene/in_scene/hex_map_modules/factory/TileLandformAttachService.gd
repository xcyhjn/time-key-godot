class_name TileLandformAttachService
extends RefCounted


## TileLandformAttachService 负责初始建图阶段的地貌挂接。
## 它精准搬运 HexMap 旧 `_create_stack_at()` 中的地貌处理：
## - 把 landform 实例挂到 stack 下。
## - 设置地貌在顶层碰撞锚点附近的位置。
## - 调用地貌自己的 attach_visual()。
## - 收编 LandformSprite_* 和地貌子 Sprite2D。
## - 给地貌 sprite 复制 block_material 并写入旧 shader 参数。
## - 设置 owner_battle，并按旧逻辑只给敌方加入 Enemies 分组。
## 运行时新增地貌仍由 RuntimeLandformRegistrar 负责；本模块只服务初始建图路径。


## 挂接一个地貌实例，并返回 HexMap 需要写回 metadata 的状态。
## 如果 tile_data 里没有有效 landform，则只原样返回基础 sprites，并把 occupant 置空。
func attach(coord: Vector2i, tile_data: Dictionary, stack: Area2D, base_sprites: Array, config: Dictionary) -> Dictionary:
	var sprites: Array = base_sprites.duplicate()
	var entity: landform = _get_landform(tile_data)
	if not is_instance_valid(entity):
		return _make_result(null, sprites)

	_position_and_parent_entity(entity, stack, config)
	entity.attach_visual(
		stack,
		int(config["height"]),
		float(config["current_step_h"]),
		float(config["tile_scale"])
	)
	_collect_stack_landform_sprites(stack, sprites, config)
	_collect_entity_child_sprites(entity, sprites, config)
	entity.owner_battle = config.get("owner_battle", null)
	_apply_initial_group(entity)

	return _make_result(entity, sprites)


## 从地块数据中读取初始地貌实例。
## 这里不写 map_data，保持本次拆分只迁移节点挂接和视觉收编。
func _get_landform(tile_data: Dictionary) -> landform:
	var value: Variant = tile_data.get("landform", null)
	if value is landform and is_instance_valid(value):
		return value
	return null


## 设置地貌实例位置并挂到 stack 下。
## 位置公式保持旧逻辑：碰撞锚点加 landform_instance_offset。
func _position_and_parent_entity(entity: landform, stack: Area2D, config: Dictionary) -> void:
	entity.position = Vector2(
		float(config["hitbox_offset_x"]),
		float(config["top_block_y"]) + float(config["hitbox_offset_y"])
	) + (config["landform_instance_offset"] as Vector2)
	stack.add_child(entity)


## 收编 attach_visual() 在 stack 下创建的 `LandformSprite_*`。
## 旧逻辑只扫描这个命名前缀；这里保持一致，避免收进其他辅助节点。
func _collect_stack_landform_sprites(stack: Area2D, sprites: Array, config: Dictionary) -> void:
	for child in stack.get_children():
		if child is Sprite2D and child.name.begins_with("LandformSprite_"):
			_append_landform_sprite(child, sprites, config)


## 收编地貌实例自己的子 Sprite2D。
## 某些地貌脚本会把可视节点挂在自身下，而不是直接挂到 stack 下。
func _collect_entity_child_sprites(entity: landform, sprites: Array, config: Dictionary) -> void:
	for child in entity.get_children():
		if child is Sprite2D:
			_append_landform_sprite(child, sprites, config)


## 将一个地貌 sprite 加入 sprites 缓存，并应用旧 shader 参数。
## 建筑位于地形之上，因此 block_idx/total_height 沿用旧的 height + 1 / height + 2。
func _append_landform_sprite(sprite: Sprite2D, sprites: Array, config: Dictionary) -> void:
	if sprites.has(sprite):
		return
	var block_material := config.get("block_material", null) as ShaderMaterial
	if block_material:
		sprite.material = block_material.duplicate()
		sprite.set_instance_shader_parameter("block_idx", float(int(config["height"]) + 1))
		sprite.set_instance_shader_parameter("total_height", float(int(config["height"]) + 2))
		if bool(config.get("is_flat_view", false)):
			sprite.material.set_instance_shader_parameter("is_flat_view", 1.0)
	sprites.append(sprite)


## 初始建图阶段只保留旧行为：敌方加入 Enemies 分组。
## 中立地貌不在这里补 Middle 分组，避免和旧 `_create_stack_at()` 行为发生差异。
func _apply_initial_group(entity: landform) -> void:
	if entity.Attitude == entity.Attitude_Pool.Enemy:
		entity.add_to_group("Enemies")


## 构造返回状态。
## HexMap 继续负责把 occupant、sprites 写入 stack metadata。
func _make_result(entity: landform, sprites: Array) -> Dictionary:
	return {
		"occupant": entity,
		"sprites": sprites,
	}
