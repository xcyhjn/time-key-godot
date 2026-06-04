class_name TileStackFactory
extends RefCounted


## TileStackFactory 负责创建单个基础地块栈。
## 本模块只处理 Area2D 容器、基础地块 Sprite、碰撞体和碰撞 metadata；
## 地貌实例挂接、阵营分组、血条、输入信号和高度数字仍由 HexMap 旧流程处理。


## 创建一个基础地块栈，并返回 HexMap 后续流程需要的状态包。
## config 由 HexMap 构造，所有 Inspector 可调项仍留在 HexMap，不在工厂内新增导出变量。
func create_stack(config: Dictionary) -> Dictionary:
	var height := int(config["height"])
	var current_step_h := float(config["current_step_h"])
	var is_flat_view := bool(config["is_flat_view"])
	var stack_container := _create_stack_container(config)
	var sprites_in_stack := _create_base_sprites(stack_container, height, current_step_h, is_flat_view, config)
	var top_block_y := _get_top_block_y(height, current_step_h, is_flat_view)
	var collision := _create_collision_node(top_block_y, config)

	stack_container.add_child(collision)
	stack_container.set_meta("collision_node", collision)

	return {
		"stack": stack_container,
		"sprites": sprites_in_stack,
		"height": height,
		"current_step_h": current_step_h,
		"top_block_y": top_block_y,
		"collision": collision,
	}


## 创建 Area2D 容器并挂到地图根节点下。
## stack_nodes 字典仍由 HexMap 写入，这样地图拓扑状态不会分散到工厂里。
func _create_stack_container(config: Dictionary) -> Area2D:
	var stack_container := Area2D.new()
	stack_container.position = config["position"] as Vector2
	var map_root := config["map_root"] as Node
	map_root.add_child(stack_container)
	return stack_container


## 创建基础地块柱子的所有 Sprite2D。
## 顶层使用 top_texture，其余层使用 side_texture；平铺视图下只显示顶层。
func _create_base_sprites(
	stack_container: Area2D,
	height: int,
	current_step_h: float,
	is_flat_view: bool,
	config: Dictionary
) -> Array[Sprite2D]:
	var sprites_in_stack: Array[Sprite2D] = []
	for layer_index in range(height):
		var sprite := _create_base_sprite(layer_index, height, current_step_h, is_flat_view, config)
		stack_container.add_child(sprite)
		sprites_in_stack.append(sprite)
	return sprites_in_stack


## 创建单层地块 Sprite。
## 这里保留旧偏移、缩放和 shader 参数写法，避免本次拆分改变地块外观。
func _create_base_sprite(
	layer_index: int,
	height: int,
	current_step_h: float,
	is_flat_view: bool,
	config: Dictionary
) -> Sprite2D:
	var sprite := Sprite2D.new()
	sprite.texture = _get_layer_texture(layer_index, height, config)
	sprite.centered = false
	sprite.offset = Vector2(-256, -400)
	sprite.scale = Vector2(float(config["tile_scale"]), float(config["tile_scale"]))

	var block_material := config["block_material"] as ShaderMaterial
	if block_material:
		sprite.material = block_material.duplicate()
		sprite.set_instance_shader_parameter("block_idx", float(layer_index))
		sprite.set_instance_shader_parameter("total_height", float(height))

	_apply_base_sprite_view_state(sprite, layer_index, height, current_step_h, is_flat_view)
	return sprite


## 根据层级选择顶部或侧面纹理。
## 顶层是玩家看到的六边形表面，下方层是柱体侧面。
func _get_layer_texture(layer_index: int, height: int, config: Dictionary) -> Texture2D:
	if layer_index == height - 1:
		return config["top_texture"] as Texture2D
	return config["side_texture"] as Texture2D


## 应用 3D/平铺视图下的基础层位置。
## 平铺视图只显示顶层；3D 视图按 current_step_h 把每层向上堆叠。
func _apply_base_sprite_view_state(
	sprite: Sprite2D,
	layer_index: int,
	height: int,
	current_step_h: float,
	is_flat_view: bool
) -> void:
	if is_flat_view:
		if sprite.material:
			sprite.material.set_instance_shader_parameter("is_flat_view", 1.0)
		if layer_index < height - 1:
			sprite.visible = false
			sprite.modulate.a = 0.0
		else:
			sprite.position.y = 0.0
		return

	sprite.position.y = -float(layer_index) * current_step_h
	if layer_index < height - 1:
		sprite.modulate = Color(1.0, 1.0, 1.0)


## 计算顶层方块的 y 坐标。
## 地貌和碰撞体都需要贴在顶层位置；平铺视图下顶层固定贴地。
func _get_top_block_y(height: int, current_step_h: float, is_flat_view: bool) -> float:
	if is_flat_view:
		return 0.0
	return -float(height - 1) * current_step_h


## 创建地块碰撞多边形。
## 多边形点、z_index 和偏移仍由 HexMap 传入，工厂只负责实例化节点并写入位置。
func _create_collision_node(top_block_y: float, config: Dictionary) -> CollisionPolygon2D:
	var collision := CollisionPolygon2D.new()
	collision.polygon = config["hitbox_polygon"]
	collision.z_as_relative = false
	collision.z_index = int(config["hitbox_z_index"])
	collision.position = Vector2(
		float(config["hitbox_offset_x"]),
		top_block_y + float(config["hitbox_offset_y"])
	)
	return collision
