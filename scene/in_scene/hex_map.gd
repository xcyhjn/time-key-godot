# 原文件名: hex_map(地块生成).gd
# 功能: 地块生成与战斗地图管理
extends Node2D
class_name battle

signal CreateBar(Landform_in, situation, x, y)

@export_group("Assets")
@export var hex_top_tex: Texture2D
@export var hex_side_tex: Texture2D
@export var block_material: ShaderMaterial
@export var label_bg_tex: Texture2D

@export_group("Grid Generation Settings")
@export var map_generation_mode: int = 1  # 0=扇形战斗地图, 1=圆形随机地图

@export_subgroup("扇形战斗地图设置")
@export var nexus_height: int = 8
@export var fan_radius: int = 6
@export var inner_tier_radius: int = 3
@export var fan_angle_span: float = 150.0

@export_subgroup("圆形随机地图设置")
@export var map_radius: int = 4          # 地图半径：1=7格，2=19格，3=37格...
@export var base_h_min: int = 1
@export var base_h_max: int = 2
@export var elite_h_bonus: int = 1       # battle_elite 时整体高度上浮一点（可调）
@export var use_external_seed: bool = true

@export var max_generation_attempts: int = 20

@export_group("Grid Spacing & Hitbox")
@export var spacing_x: float = 168.24
@export var spacing_y: float = 96.24
@export var step_height: float = 48.0
@export var tile_scale: float = 0.6
@export var hitbox_width: float = 168.0
@export var hitbox_base_height: float = 96.0
@export var hitbox_offset_x: float = -153.6
@export var hitbox_offset_y: float = -240.0

const REF_SCALE: float = 0.6

var property_pool : Array[Script] = []
var Max_Start_landform : int = 5

# ==========================================
# 地形系统（整合自 node_2d.gd）
# ==========================================
enum TerrainType { BEACH, PLAINS, HILLS, MOUNTAIN, NEXUS_CORE = -1 }
enum LandformType { NONE, MINE, CAVE, VILLAGE, RUINS }  # 整合自 node_2d.gd，用于兼容性

@export_group("地形系统")
@export var terrain_by_height := {
	-1 : TerrainType.NEXUS_CORE,
	1: TerrainType.BEACH,
	2: TerrainType.PLAINS,
	3: TerrainType.PLAINS,
	4: TerrainType.HILLS,
	5: TerrainType.MOUNTAIN,
	6: TerrainType.MOUNTAIN,
}
@export var top_tex_by_terrain: Array[Texture2D]  # 下标用 TerrainType
@export var side_tex_by_terrain: Array[Texture2D]
@export var max_landform_ratio: float = 0.35  # 最多35%格子有地貌（可调）

var landform_pool: Array[Script] = []  # 地貌脚本池，在 _ready 中初始化

var map_data: Dictionary = { }
# ★ 优化：新增栈缓存字典，方便 O(1) 查找遮挡物
var stack_nodes: Dictionary = { }

@onready var dim = $"../../ui/DimMenu"

# ==========================================
# 外部事件处理系统（整合自 node_2d.gd）
## 外部事件设置器（整合自 node_2d.gd）
func set_received_text(v: String) -> void:
	received_text = v
	parts = received_text.split("|", false)
	room_type = parts[0] if parts.size() > 0 else ""

	if parts.size() > 1 and parts[1] != "":
		features = parts[1].split(",", false)
	else:
		features = PackedStringArray()


var received_text: String = "" : set = set_received_text
var parts: PackedStringArray = PackedStringArray()
var room_type: String = ""
var features: PackedStringArray = PackedStringArray()

var map_root: Node2D
var hovered_stacks: Array[Area2D] = []
var active_stack: Area2D = null
var selected_stack: Area2D = null  # 记录当前被点击选中的地块
var currently_occluding_stacks: Array[Area2D] = []  # 记录当前处于透明湮灭状态的地块

## 应用外部事件（整合自 node_2d.gd）
func apply_external_event(payload: String) -> void:
	# payload 示例："battle_elite|enhance,combine"
	received_text = payload  # 会触发 setter，自动更新 room_type/features

## 处理事件逻辑（整合自 node_2d.gd）
func handle_event_logic():
	# 这里你可以根据接收到的字符串，修改场景的背景颜色、背景音乐或生成参数
	match room_type:
		"battle_normal":
			print(">> 准备：普通战斗环境")
		"battle_elite":
			print(">> 准备：精英挑战环境（地块可能更高）")
		"boss_stage":
			print(">> 准备：BOSS 战环境")

func _ready():
	await dim.use(dim.mode)
	y_sort_enabled = true
	# 连接到建筑行为信号
	Signal_Bus.step_next.connect(_on_step_next)
	
	# 打印接收到的信息，方便调试（整合自 node_2d.gd）
	if parts.size() > 1:
		var features = parts[1].split(",") 
		# features 将会是 ["enhance", "combine"]
		for feature in features:
			if GlobalClock.landform_property_pool.keys().has(feature):
				print("装载属性：" + feature)
				for landform_in in GlobalClock.landform_property_pool[feature]:
					if !property_pool.has(landform_in):
						property_pool.append(landform_in)
			print("该房间拥有特性:", feature)
	print("[HexMap 场景] 已进入，接收到的事件类型为: ", room_type)
	
	map_root = Node2D.new()
	map_root.y_sort_enabled = true
	add_child(map_root)

	# 初始化地貌池
	landform_pool = [
		preload("res://scene/in_scene/enermy/village.gd")
		# 可在此添加更多地貌类型
	]

	var screen_size = get_viewport_rect().size
	map_root.position = Vector2(screen_size.x * 0.5, screen_size.y * 0.3)

	# 2. 根据不同的事件类型，初始化不同的视觉效果（可选）
	handle_event_logic()

	build_map_pipeline()


func build_map_pipeline():
	var is_valid = false
	var attempts = 0
	while not is_valid and attempts < max_generation_attempts:
		attempts += 1
		map_data.clear()
		_generate_map_data()
		is_valid = _check_map_validity()

	_assign_terrains_and_enemies()
	_render_map()


func _generate_map_data():
	match map_generation_mode:
		0: # 扇形战斗地图
			_generate_fan_map_data()
		1: # 圆形随机地图
			_generate_circular_map_data()
		_:
			push_error("无效的地图生成模式: " + str(map_generation_mode))
			_generate_fan_map_data()  # 默认回退

func _generate_fan_map_data():
	map_data[Vector2(0, 0)] = { "height": nexus_height, "tier": 3, "terrain": TerrainType.NEXUS_CORE }
	var min_angle = 90.0 - (fan_angle_span / 2.0)
	var max_angle = 90.0 + (fan_angle_span / 2.0)

	for q in range(-fan_radius, fan_radius + 1):
		for r in range(-fan_radius, fan_radius + 1):
			if q == 0 and r == 0: continue
			var dist = (abs(q) + abs(q + r) + abs(r)) / 2
			if dist <= fan_radius:
				var pixel_pos = _get_hex_pixel_pos(Vector2(q, r))
				var angle_deg = rad_to_deg(pixel_pos.angle())
				if angle_deg < 0: angle_deg += 360.0
				if angle_deg >= min_angle and angle_deg <= max_angle:
					var tier = 3 if dist <= inner_tier_radius else (2 if dist <= inner_tier_radius + 2 else 1)
					map_data[Vector2(q, r)] = { "height": _roll_height_by_tier(tier), "tier": tier, "terrain": TerrainType.NEXUS_CORE }

func _generate_circular_map_data():
	#push_error(str(GlobalClock.tile_h_pool.keys()))
	# 基于 node_2d.gd 的 generate_map_data 函数逻辑
	map_data.clear()
	
	var rng = RandomNumberGenerator.new()
	if use_external_seed and received_text != "":
		rng.seed = received_text.hash()
	else:
		rng.randomize()

	var h_min = base_h_min
	var h_max = base_h_max
	if room_type == "battle_elite":
		h_max += elite_h_bonus
	elif room_type == "boss_stage":
		h_max += elite_h_bonus + 1

	# 先生成所有高度 + 地形（圆形六边形网格）
	for q in range(-map_radius, map_radius + 1):
		for r in range(-map_radius, map_radius + 1):
			if abs(q + r) <= map_radius:
				var coord = Vector2(q, r)  # 使用 Vector2 保持与 hex_map 的兼容性
				var h = rng.randi_range(h_min, h_max)
				var terrain = get_terrain_from_height(h)

				map_data[coord] = {
					"height": h,
					"terrain": terrain,
					"landform": null
				}
				#push_error(str(GlobalClock.tile_h_pool.keys()))
				if GlobalClock.tile_h_pool.keys().has(h):
					GlobalClock.tile_h_pool[h].append(coord)
					#push_error(str(coord) + "已加入高为" + str(h) + "的数组中\n此数组内含" + str(GlobalClock.tile_h_pool[h]))
	
	
	# 地貌放置逻辑在 _assign_terrains_and_enemies 中处理

func _roll_height_by_tier(tier: int) -> int:
	var roll = randf()
	if tier == 3: return randi_range(4, 6) if roll < 0.6 else randi_range(2, 3)
	else: return randi_range(1, 3) if roll < 0.7 else randi_range(3, 4)


func _check_map_validity() -> bool: return true  # 遮挡由于有了透视效果，不一定需要驳回重成了


## 根据高度获取地形类型
func get_terrain_from_height(h: int) -> TerrainType:
	if terrain_by_height.has(h):
		return terrain_by_height[h]
	# 超出表范围：按区间兜底
	if h <= 1: return TerrainType.BEACH
	if h <= 3: return TerrainType.PLAINS
	if h == 4: return TerrainType.HILLS
	return TerrainType.MOUNTAIN


## 选择合适的地貌（基于高度、地形和随机性）
func pick_landform() :
	
	var landform_in
	var coord
	var number = 0
	if !property_pool.is_empty():
		for i in range(randi_range( 1, Max_Start_landform - 1)):
			landform_in = property_pool.pick_random()
			coord = landform_pick(landform_in)
			if coord != Vector2(-100, -100):
				map_data[coord]["landform"] = landform_in.new(coord, self)
				number += 1
	for i in range(Max_Start_landform - number):
		landform_in = landform_pool.pick_random()
		coord = landform_pick(landform_in)
		if coord == Vector2(-100, -100):
			continue
		map_data[coord]["landform"] = landform_in.new(coord, self)

func landform_pick(landform_in : Script) -> Vector2:
	var buffer =  landform_in.new(Vector2(-100, -100), self)
	var buffer_pool
	var coord
	
	if buffer.landform_rules.keys().has("require_height") and buffer.landform_rules["require_height"] != null:
		var index = randi_range(0,buffer.landform_rules["require_height"].size() - 1)
		buffer_pool = GlobalClock.tile_h_pool[buffer.landform_rules["require_height"][index]].duplicate()
		coord = randi_range(0, buffer_pool.size() - 1)
	else :
		buffer_pool = map_data.keys().duplicate()
		coord = randi_range(0, buffer_pool.size() - 1)
	while !buffer_pool.is_empty() and !buffer.get_possible_coords(map_data[buffer_pool[coord]]["height"], map_data[buffer_pool[coord]]["terrain"], map_data[buffer_pool[coord]]) :
		buffer_pool.pop_at(coord)
		if buffer_pool.is_empty():
			break
		coord = randi_range(0, buffer_pool.size() - 1)
	print(str(buffer_pool))
	if buffer_pool.is_empty():
		buffer.free()
		return Vector2(-100, -100)
	elif buffer.get_possible_coords(map_data[buffer_pool[coord]]["height"], map_data[buffer_pool[coord]]["terrain"], map_data[buffer_pool[coord]]):
		buffer.free()
		return buffer_pool[coord]
	else :
		buffer.free()
		return Vector2(-100, -100)

func _assign_terrains_and_enemies():
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	
	# 第一步：分配地形类型（处理两种地图模式）
	for coord in map_data.keys():
		var data = map_data[coord]
		
		# 跳过nexus核心（仅扇形地图有）
		if data.has("terrain") and data["terrain"] == TerrainType.NEXUS_CORE: 
			continue
		
		var height = data["height"]
		
		# 圆形地图已经设置了terrain字段，扇形地图需要从高度计算
		if map_generation_mode == 1:  # 圆形地图
			# 圆形地图中，terrain字段已经存储了地形类型（来自get_terrain_from_height）
			# 确保terrain_type字段存在
			if data.has("terrain"):
				data["terrain_type"] = data["terrain"]
		else:  # 扇形地图
			var terrain_type = get_terrain_from_height(height)
			data["terrain_type"] = terrain_type
			data["terrain"] = terrain_type  # 保持向后兼容，但使用枚举值
	
	# 第二步：根据地貌密度限制放置地貌
	var coords_list = map_data.keys()
	coords_list.shuffle()  # 随机打乱顺序
	
	var max_landform_count = int(coords_list.size() * max_landform_ratio)
	var placed = 0
	
	for coord in coords_list:
		if placed >= max_landform_count:
			break
		var data = map_data[coord]
		
		# 跳过nexus核心
		if data.has("terrain") and data["terrain"] == TerrainType.NEXUS_CORE:
			continue
		
		var height = data["height"]
		var terrain_type = data["terrain_type"]
	pick_landform()


## 获取地形顶部纹理
func get_top_tex(terrain: TerrainType) -> Texture2D:
	if top_tex_by_terrain.size() > terrain and top_tex_by_terrain[terrain] != null:
		return top_tex_by_terrain[terrain]
	return hex_top_tex

## 获取地形侧面纹理
func get_side_tex(terrain: TerrainType) -> Texture2D:
	if side_tex_by_terrain.size() > terrain and side_tex_by_terrain[terrain] != null:
		return side_tex_by_terrain[terrain]
	return hex_side_tex

## 地形类型转字符串（用于调试）
func terrain_type_to_string(terrain: TerrainType) -> String:
	match terrain:
		TerrainType.BEACH: return "BEACH"
		TerrainType.PLAINS: return "PLAINS"
		TerrainType.HILLS: return "HILLS"
		TerrainType.MOUNTAIN: return "MOUNTAIN"
		_: return "UNKNOWN"


func _render_map():
	stack_nodes.clear()
	for coord in map_data.keys():
		_create_stack_at(coord, map_data[coord])


func _get_hex_pixel_pos(hex_coord: Vector2) -> Vector2:
	var ratio = tile_scale / REF_SCALE
	var screen_x = hex_coord.x * spacing_x * ratio
	var screen_y = hex_coord.y * spacing_y * ratio + (hex_coord.x * spacing_y * ratio * 0.5)
	return Vector2(screen_x, screen_y)


func _create_stack_at(coord: Vector2, data: Dictionary):
	var pos = _get_hex_pixel_pos(coord)
	var height = data["height"]
	var current_step_h = step_height * (tile_scale / REF_SCALE)

	var stack_container = Area2D.new()
	stack_container.position = pos
	map_root.add_child(stack_container)
	stack_nodes[coord] = stack_container  # 记录进字典

	# 获取地形类型，默认 PLAINS
	var terrain_type = data.get("terrain_type", TerrainType.PLAINS)
	var top_tex = get_top_tex(terrain_type)
	var side_tex = get_side_tex(terrain_type)

	var sprites_in_stack = []
	for i in range(height):
		var sprite = Sprite2D.new()
		sprite.texture = top_tex if i == height - 1 else side_tex
		sprite.centered = false
		sprite.offset = Vector2(-256, -400)
		sprite.position.y = -i * current_step_h
		sprite.scale = Vector2(tile_scale, tile_scale)

		if block_material:
			sprite.material = block_material.duplicate()  # 必须 Duplicate 才能独立发光消融
			sprite.set_instance_shader_parameter("block_idx", float(i))
			sprite.set_instance_shader_parameter("total_height", float(height))  # ★ 注入总高度

		if i < height - 1: sprite.modulate = Color(0.8, 0.8, 0.8)
		stack_container.add_child(sprite)
		sprites_in_stack.append(sprite)

	# 碰撞箱生成
	var collision = CollisionPolygon2D.new()
	var w = hitbox_width; var h = hitbox_base_height
	collision.polygon = PackedVector2Array([
		Vector2(-w / 4.0, -h / 2.0), Vector2(w / 4.0, -h / 2.0), Vector2(w / 2.0, 0),
		Vector2(w / 4.0, h / 2.0), Vector2(-w / 4.0, h / 2.0), Vector2(-w / 2.0, 0)
	])
	var top_block_y = -(height - 1) * current_step_h
	collision.position = Vector2(hitbox_offset_x, top_block_y + hitbox_offset_y)
	stack_container.add_child(collision)

	# 地貌生成（使用 landform 系统）
	var enemy_instance = null
	
	if data.has("landform") and data["landform"] != null:
		var landform_inst = data["landform"]
		
		# 直接使用 landform 作为敌人（landform 现在继承 EnemyBase）
		enemy_instance = landform_inst
		enemy_instance.position = Vector2(hitbox_offset_x, top_block_y + hitbox_offset_y - 20)
		stack_container.add_child(enemy_instance)
		
		# 添加 landform 视觉精灵（由 attach_visual 方法处理材质和层级）
		landform_inst.attach_visual(stack_container, height, current_step_h, tile_scale)
		
		# 获取 landform 视觉精灵用于添加到 sprites 元数据并应用shader
		var unique_name = "LandformSprite_%s_%s" % [coord.x, coord.y]
		var landform_sprite = stack_container.get_node_or_null(unique_name)
		if landform_sprite and block_material:
			# 为村庄应用shader材质
			landform_sprite.material = block_material.duplicate()
			# 建筑在地形之上，block_idx增加1以确保悬浮效果触发
			landform_sprite.set_instance_shader_parameter("block_idx", float(height + 1))
			landform_sprite.set_instance_shader_parameter("total_height", float(height + 2))
			sprites_in_stack.append(landform_sprite)
		
		GameLogger.debug("生成地貌: %s at %s" % [landform_inst.name, coord], "HexMap")
	
	# 旧的 enemy 系统已废弃，不再支持

	stack_container.set_meta("sprites", sprites_in_stack)
	stack_container.set_meta("height", height)
	stack_container.set_meta("occupant", enemy_instance)

	stack_container.mouse_entered.connect(_on_stack_hover.bind(stack_container, true))
	stack_container.mouse_exited.connect(_on_stack_hover.bind(stack_container, false))
	stack_container.input_event.connect(_on_stack_input.bind(stack_container))


func get_card_manager() -> Node:
	# 直接查找CardManager节点（HexMap在map下，CardManager在project下）
	var card_manager = get_node_or_null("../../CardManager")  # 从HexMap向上两级到project，查找CardManager子节点
	if card_manager:
		return card_manager
	
	# 备用方案：通过元数据查找
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		return tree_root.get_meta("card_manager")
	
	# 回退到当前场景元数据
	var scene_root = get_tree().current_scene
	if scene_root and scene_root.has_meta("card_manager"):
		return scene_root.get_meta("card_manager")
	
	return null


# ==========================================
# ★ 选中逻辑与技能施放
# ==========================================
func _on_stack_input(viewport: Node, event: InputEvent, shape_idx: int, stack: Area2D):
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			var cm = get_card_manager()
			if not cm: return

			# 1. 触发选中 Shader
			if is_instance_valid(selected_stack) and selected_stack != stack:
				_tween_shader_param(selected_stack, "is_selected_blend", 0.0, 0.1)
			selected_stack = stack
			_tween_shader_param(selected_stack, "is_selected_blend", 1.0, 0.1)

			# 2. 触发卡牌施放
			var active_card = cm.get("current_selected_card")
			if active_card != null and active_card.has_method("play_card"):
				active_card.play_card(stack)
				# 施放完毕后取消选中
				_tween_shader_param(selected_stack, "is_selected_blend", 0.0, 0.1)
				selected_stack = null

		elif event.button_index == MOUSE_BUTTON_RIGHT:
			# 右键取消选中卡牌
			_cancel_card_selection()


# ==========================================
# ★ 悬浮与遮挡检测逻辑
# ==========================================
func _on_stack_hover(stack: Area2D, is_entered: bool):
	if is_entered:
		if not hovered_stacks.has(stack): hovered_stacks.append(stack)
	else:
		hovered_stacks.erase(stack)
	_update_highlight()


func _update_highlight():
	hovered_stacks = hovered_stacks.filter(func(s): return is_instance_valid(s))
	var front_stack: Area2D = null
	var max_y = -INF

	for stack in hovered_stacks:
		if stack.global_position.y > max_y:
			max_y = stack.global_position.y
			front_stack = stack

	if active_stack != front_stack:
		if is_instance_valid(active_stack):
			_tween_shader_param(active_stack, "highlight_blend", 0.0, 0.15)
			# 立即清除条件地块效果（无动画）
			_clear_conditional_effects_immediate(active_stack)

		active_stack = front_stack
		if is_instance_valid(active_stack):
			_tween_shader_param(active_stack, "highlight_blend", 1.0, 0.15)
			# 应用条件地块效果
			_apply_conditional_effects(active_stack)

		# ★ 触发遮挡计算
		_update_occlusion(active_stack)

# ==========================================
# ★ 新增：条件地块效果系统
# ==========================================


## 判断地块是否是卡牌的有效目标
func _is_stack_valid_target(stack: Area2D) -> bool:
	var cm = get_card_manager()
	if not cm: return false

	var selected_card = cm.get("current_selected_card")
	if not selected_card: return false

	# 这里需要根据卡牌的效果来判断地块是否有效
	# 暂时先返回true作为测试
	return true


## 应用条件地块效果
func _apply_conditional_effects(stack: Area2D) -> void:
	if not is_instance_valid(stack): return

	var cm = get_card_manager()
	if not cm: return

	var selected_card = cm.get("current_selected_card")
	if not selected_card:
		# 没有选中卡牌，只显示普通悬浮效果
		_clear_conditional_effects(stack)
		return

	# 判断地块是否是有效目标
	var is_valid = _is_stack_valid_target(stack)

	# 应用Shader效果
	var sprites = stack.get_meta("sprites") as Array
	if sprites.is_empty() or not sprites[0].material: return

	for sprite in sprites:
		if sprite.material:
			if is_valid:
				# 有效目标地块：变白效果
				sprite.material.set_shader_parameter("is_valid_target", true)
				sprite.material.set_shader_parameter("is_invalid_target", false)
			else:
				# 无效目标地块：灰色"无效果"显示
				sprite.material.set_shader_parameter("is_valid_target", false)
				sprite.material.set_shader_parameter("is_invalid_target", true)


## 清除条件地块效果
func _clear_conditional_effects(stack: Area2D) -> void:
	if not is_instance_valid(stack): return

	var sprites = stack.get_meta("sprites") as Array
	if sprites.is_empty() or not sprites[0].material: return

	for sprite in sprites:
		if sprite.material:
			sprite.material.set_shader_parameter("is_valid_target", false)
			sprite.material.set_shader_parameter("is_invalid_target", false)


## 立即清除条件地块效果（无动画，直接设置）
func _clear_conditional_effects_immediate(stack: Area2D) -> void:
	if not is_instance_valid(stack): return

	var sprites = stack.get_meta("sprites") as Array
	if sprites.is_empty() or not sprites[0].material: return

	for sprite in sprites:
		if sprite.material:
			sprite.material.set_shader_parameter("is_valid_target", false)
			sprite.material.set_shader_parameter("is_invalid_target", false)


## 右键取消选中卡牌
func _cancel_card_selection() -> void:
	GameLogger.info("在地块上右键取消卡牌选中", "HexMap")

	# 通知卡牌管理器取消选中
	var cm = get_card_manager()
	if cm:
		cm.deselect_card()

	# 清除地块条件效果
	update_all_stack_conditional_effects()

	# 清除选中地块的Shader效果
	if is_instance_valid(selected_stack):
		_tween_shader_param(selected_stack, "is_selected_blend", 0.0, 0.2)
		selected_stack = null

	GameLogger.info("卡牌选中已取消", "HexMap")


## 更新所有地块的条件效果（在卡牌选中状态变化时调用）
func update_all_stack_conditional_effects() -> void:
	var cm = get_card_manager()
	if not cm: return

	var selected_card = cm.get("current_selected_card")

	# 遍历所有地块
	for stack in stack_nodes.values():
		if not is_instance_valid(stack): continue

		var sprites = stack.get_meta("sprites") as Array
		if sprites.is_empty() or not sprites[0].material: continue

		if selected_card:
			# 有选中卡牌，应用条件效果
			var is_valid = _is_stack_valid_target(stack)
			for sprite in sprites:
				if sprite.material:
					if is_valid:
						sprite.material.set_shader_parameter("is_valid_target", true)
						sprite.material.set_shader_parameter("is_invalid_target", false)
					else:
						sprite.material.set_shader_parameter("is_valid_target", false)
						sprite.material.set_shader_parameter("is_invalid_target", true)
		else:
			# 没有选中卡牌，清除条件效果
			for sprite in sprites:
				if sprite.material:
					sprite.material.set_shader_parameter("is_valid_target", false)
					sprite.material.set_shader_parameter("is_invalid_target", false)


# ==========================================
# ★ 核心：动态湮灭遮挡物
# ==========================================
func _update_occlusion(target_stack: Area2D):
	# 1. 恢复之前被湮灭的柱子 (倒放)
	for stack in currently_occluding_stacks:
		if is_instance_valid(stack) and stack != target_stack:
			_tween_shader_param(stack, "dissolve_blend", 0.0, 0.15)
	currently_occluding_stacks.clear()

	if not is_instance_valid(target_stack): return

	var t_pos = target_stack.position
	var t_h = target_stack.get_meta("height")
	var current_step_h = step_height * (tile_scale / REF_SCALE)
	var t_top_y = t_pos.y - (t_h - 1) * current_step_h

	# 2. 遍历判断谁挡在前面
	for stack in stack_nodes.values():
		if stack == target_stack: continue
		var s_pos = stack.position

		# 物理上在前面 (Y轴靠下，Godot中越向下Y越大越晚渲染)
		if s_pos.y > t_pos.y:
			# X轴有重叠 (视觉上有交集)
			if abs(s_pos.x - t_pos.x) < hitbox_width * 0.8:
				var s_h = stack.get_meta("height")
				var s_top_y = s_pos.y - (s_h - 1) * current_step_h

				# 如果前方方块的顶部 高于 后方方块的底部(视线被阻挡)
				if s_top_y < t_pos.y:
					currently_occluding_stacks.append(stack)
					# 触发 0.25秒的极速湮灭！保留20%可见度（0.8）以便点击
					_tween_shader_param(stack, "dissolve_blend", 0.8, 0.25)


# ==========================================
# 通用 Shader Tween 控制器
# ==========================================
func _tween_shader_param(stack: Area2D, param_name: String, target_val: float, duration: float):
	if not is_instance_valid(stack): return
	var sprites = stack.get_meta("sprites") as Array
	if sprites.is_empty() or not sprites[0].material: return

	# 杀掉旧的动画防止冲突
	var meta_key = "tween_" + param_name
	if stack.has_meta(meta_key):
		var old_tw = stack.get_meta(meta_key)
		if is_instance_valid(old_tw) and old_tw.is_valid(): old_tw.kill()

	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	stack.set_meta(meta_key, tw)

	var current_val = sprites[0].get_instance_shader_parameter(param_name)
	if current_val == null: current_val = 0.0

	tw.tween_method(func(val: float):
		for s in sprites:
			if is_instance_valid(s) and s.material:
				s.set_instance_shader_parameter(param_name, val)
	, current_val, target_val, duration)


## 添加地貌视觉（用于地形实体死亡或损坏时更新视觉）
func add_landform_visual_at(coord: Vector2i) -> void:
	# 将 Vector2i 转换为 Vector2（用于字典键）
	var coord_v2 = Vector2(coord)
	
	# 检查是否存在对应的栈容器和地貌数据
	if not stack_nodes.has(coord_v2):
		return
	if not map_data.has(coord_v2):
		return
	
	var stack_container = stack_nodes[coord_v2]
	var data = map_data[coord_v2]
	
	# 获取地貌实例
	var landform_inst = data.get("landform")
	if landform_inst == null:
		return
	
	var height = data["height"]
	var current_step_h = step_height * (tile_scale / REF_SCALE)
	
	# 调用地貌的 attach_visual 方法重新创建视觉精灵
	landform_inst.attach_visual(stack_container, height, current_step_h, tile_scale)
	
	# 获取新创建的 landform 视觉精灵并应用shader
	var unique_name = "LandformSprite_%s_%s" % [coord.x, coord.y]
	var landform_sprite = stack_container.get_node_or_null(unique_name)
	if landform_sprite and block_material:
		# 为新生成的村庄应用shader材质
		landform_sprite.material = block_material.duplicate()
		# 建筑在地形之上，block_idx增加1以确保悬浮效果触发
		landform_sprite.set_instance_shader_parameter("block_idx", float(height + 1))
		landform_sprite.set_instance_shader_parameter("total_height", float(height + 2))
		
		# 将 landform 精灵添加到 sprites 元数据中，以便后续效果应用
		var sprites = stack_container.get_meta("sprites") as Array
		if not sprites.has(landform_sprite):
			sprites.append(landform_sprite)
			stack_container.set_meta("sprites", sprites)
	
	GameLogger.debug("更新地貌视觉: %s at %s" % [landform_inst.name, coord], "HexMap")


## 地形名称转换函数（整合自 node_2d.gd）
func terrain_name(t: int) -> String:
	match t:
		TerrainType.BEACH: return "BEACH"
		TerrainType.PLAINS: return "PLAINS"
		TerrainType.HILLS: return "HILLS"
		TerrainType.MOUNTAIN: return "MOUNTAIN"
		_: return "UNK"

## 地貌名称转换函数（整合自 node_2d.gd）
func landform_name(l: int) -> String:
	match l:
		LandformType.NONE: return "NONE"
		LandformType.MINE: return "MINE"
		LandformType.CAVE: return "CAVE"
		LandformType.VILLAGE: return "VILLAGE"
		LandformType.RUINS: return "RUINS"
		_: return "UNK"

## 对地块应用伤害（整合自 node_2d.gd）
func apply_damage_to_tile(coord: Vector2i, new_damage: int) -> void:
	# 将 Vector2i 转换为 Vector2（用于字典键）
	var coord_v2 = Vector2(coord)
	if not map_data.has(coord_v2):
		return
	map_data[coord_v2]["damage"] = clamp(new_damage, 0, 2)

## 刷新地貌视觉（整合自 node_2d.gd）
func refresh_landform_visual(coord: Vector2i) -> void:
	# 重新生成整个地图（简单实现）
	build_map_pipeline()

## 处理回合结束时的建筑行为
func _on_step_next(step: int, behavior: int) -> void:
	GameLogger.info("触发建筑行为: step=%d, behavior=%d" % [step, behavior], "HexMap")
	
	# 遍历所有地块，触发建筑的 Behavior 方法
	for coord_v2 in map_data.keys():
		var data = map_data[coord_v2]
		if data.has("landform") and data["landform"] != null:
			var landform_inst = data["landform"]
			if landform_inst.has_method("Behavior"):
				# 调用建筑的 Behavior 方法
				landform_inst.Behavior(step, map_data, null, behavior)
				GameLogger.debug("执行建筑行为: %s at %s" % [landform_inst.name, coord_v2], "HexMap")

## 未处理的输入事件（整合自 node_2d.gd）
func _unhandled_input(event):
	if event.is_action_pressed("ui_cancel"): # 按下 Esc
		# 这里可以写返回地图的代码
		pass
