# 原文件名: hex_map(地块生成).gd
# 功能: 地块生成与战斗地图管理
extends Node2D
class_name battle
#血条信号测试用
signal CreateBar(landform_in: landform, situation: int, x: float, y: float)
@export_group("Assets")
@export var hex_top_tex: Texture2D
@export var hex_side_tex: Texture2D
@export var block_material: ShaderMaterial
@export var label_bg_tex: Texture2D

@export_group("Grid Generation Settings")
@export var map_generation_mode: int = 0  # 0=扇形战斗地图, 1=圆形随机地图

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

# ==========================================
# 高度视图配置（切换视角后的光柱系统）
# ==========================================
@export_group("高度视图配置")
@export var show_height_pillars: bool = true  # 是否生成光柱
@export var show_height_labels: bool = true   # 是否生成数字
@export var height_view_pillar_length: float = 50.0  # 光柱长度（向上延伸像素）
@export var height_view_pillar_width: float = 5.0    # 光柱粗细
@export var height_view_pillar_color: Color = Color(1.0, 1.0, 1.0, 0.8)  # 光柱颜色（白色带透明度）
@export var height_view_pillar_offset: Vector2 = Vector2(0, 0)  # 光柱相对方块的位置偏移

@export_subgroup("高度数字标签配置")
@export var height_label_font_size: int = 32  # 数字字体大小
@export var height_label_color: Color = Color(1.0, 1.0, 1.0, 1.0)  # 数字颜色
@export var height_label_outline_color: Color = Color(0.0, 0.0, 0.0, 1.0)  # 数字描边颜色
@export var height_label_outline_size: int = 2  # 数字描边粗细
@export var height_label_offset: Vector2 = Vector2(-10, -40)  # 数字位置偏移（相对光柱顶部）
@export var height_label_font: Font  # 数字字体（可选）

@export_subgroup("高度视图Shader配置")
@export var height_view_shader_material: ShaderMaterial  # 高度视图专用shader材质
@export var height_view_hover_speed: float = 1.5  # 鼠标悬浮时光柱上下移动速度
@export var height_view_hover_amplitude: float = 15.0  # 鼠标悬浮时光柱上下移动幅度
@export var height_view_click_highlight_color: Color = Color(1.0, 1.0, 1.0, 0.3)  # 点击时白色高亮颜色
@export var height_view_click_highlight_width: float = 4.0  # 点击时白色边框粗细

# ==========================================
# 地形系统（整合自 node_2d.gd）
# ==========================================
enum TerrainType { BEACH, PLAINS, HILLS, MOUNTAIN }
enum LandformType { NONE, MINE, CAVE, VILLAGE, RUINS }  # 整合自 node_2d.gd，用于兼容性

@export_group("地形系统")
@export var terrain_by_height := {
	1: TerrainType.BEACH,
	2: TerrainType.PLAINS,
	3: TerrainType.PLAINS,
	4: TerrainType.HILLS,
	5: TerrainType.MOUNTAIN,
	6: TerrainType.MOUNTAIN,
}
@export var top_tex_by_terrain: Array[Texture2D]  # 下标用 TerrainType
@export var side_tex_by_terrain: Array[Texture2D]
@export var max_landform_ratio: float = 0.65  # 最多35%格子有地貌（可调）

@export_group("地块生成配额")
#@export var Max_Start_landform = 10 ##初始地块数量
@export var Max_Neutral_landform: int = 10
@export var Max_Enemy_landform: int = 10
# ==========================================
# 地形升降动画配置
# ==========================================
@export_group("地形升降动画 (Elevation Animation)")
@export var ele_anim_duration: float = 0.8  # 升降过程的耗时
@export var ele_shake_intensity: float = 6.0 # 升降前地壳震动的像素幅度
@export var ele_shake_duration: float = 0.3  # 地壳震动的准备时间
@export var ele_trans_type: Tween.TransitionType = Tween.TRANS_ELASTIC # 弹性缓冲，效果最好
@export var ele_ease_type: Tween.EaseType = Tween.EASE_OUT

var landform_pool: Array[Script] = []  # 地貌脚本池，在 _ready 中初始化
var neutral_pool: Array[Script] = []
var enemy_pool: Array[Script] = []

var map_data: Dictionary = { }
# ★ 优化：新增栈缓存字典，方便 O(1) 查找遮挡物
var stack_nodes: Dictionary = { }
var height_view_compressed: bool = false  # 视角切换状态：是否压缩为1格高度
var height_view_hovered_stack: Area2D = null  # 高度视图下鼠标悬浮的地块
var height_view_selected_stack: Area2D = null  # 高度视图下点击选中的地块
var height_view_original_materials: Dictionary = {}  # 存储地块原始材质（用于恢复）
var height_view_pillar_tweens: Dictionary = {}  # 存储光柱动画补间
var is_view_transitioning: bool = false  # ★ 新增：动画状态锁
var is_visuals_locked: bool = false  # ★ 新增：视觉状态锁，防止拖拽时清除高亮和消融效果
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
	y_sort_enabled = true
	# 连接到建筑行为信号
	Signal_Bus.step_next.connect(_on_step_next)
	
	# 打印接收到的信息，方便调试（整合自 node_2d.gd）
	if parts.size() > 1:
		var features = parts[1].split(",") 
		# features 将会是 ["enhance", "combine"]
		for feature in features:
			print("该房间拥有特性:", feature)
	print("[HexMap 场景] 已进入，接收到的事件类型为: ", room_type)
	
	map_root = Node2D.new()
	map_root.y_sort_enabled = true
	add_child(map_root)
	
	#以下为两种方式测试用，可能删除第一个
	## 初始化地貌池
	#landform_pool = [
		#preload("res://scene/in_scene/enermy/village.gd"),
		## 可在此添加更多地貌类型
		#preload("res://scene/in_scene/enermy/blockhouse.gd")
	#]
	#★ 第二版 拆分池子
	neutral_pool = [
		preload("res://scene/in_scene/enermy/background.gd") # 你的中立地形脚本
	]
	
	enemy_pool = [
		preload("res://scene/in_scene/enermy/village.gd"),
		preload("res://scene/in_scene/enermy/blockhouse.gd")
	]
	var screen_size = get_viewport_rect().size
	map_root.position = Vector2(screen_size.x * 0.5, screen_size.y * 0.3)

	# 2. 根据不同的事件类型，初始化不同的视觉效果（可选）
	handle_event_logic()

	build_map_pipeline()
	# 绑定现有高度视图切换按钮
	var height_view_button = get_node_or_null("../../ui/HeightViewToggleButton")
	
	# 双保险：通过当前场景根节点寻找
	if not height_view_button and get_tree().current_scene:
		height_view_button = get_tree().current_scene.get_node_or_null("ui/HeightViewToggleButton")
		
	if height_view_button:
		height_view_button.pressed.connect(_on_height_view_toggle_pressed)
		GameLogger.debug("成功绑定高度视图切换按钮", "HexMap")
	else:
		GameLogger.warning("未找到高度视图切换按钮，请确保按钮节点存在", "HexMap")


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

## 扇形坐标采样器 - 返回满足角度范围的六边形坐标
func _get_fan_coords(radius: int, angle_span: float, center_angle: float = 90.0) -> Array[Vector2i]:
	var coords: Array[Vector2i] = []
	var min_angle = center_angle - (angle_span / 2.0)
	var max_angle = center_angle + (angle_span / 2.0)
	
	for q in range(-radius, radius + 1):
		for r in range(-radius, radius + 1):
			if q == 0 and r == 0:
				continue
			# 六边形轴向坐标距离公式
			var dist = (abs(q) + abs(q + r) + abs(r)) / 2
			if dist <= radius:
				var pixel_pos = _get_hex_pixel_pos(Vector2(q, r))
				var angle_deg = rad_to_deg(pixel_pos.angle())
				if angle_deg < 0:
					angle_deg += 360.0
				if angle_deg >= min_angle and angle_deg <= max_angle:
					coords.append(Vector2i(q, r))
	return coords


## 圆形坐标采样器 - 返回圆形区域内的六边形坐标
func _get_circular_coords(radius: int) -> Array[Vector2i]:
	var coords: Array[Vector2i] = []
	for q in range(-radius, radius + 1):
		for r in range(-radius, radius + 1):
			if q == 0 and r == 0:
				continue
			# 六边形轴向坐标距离公式（确保完美的圆形区域）
			var dist = (abs(q) + abs(q + r) + abs(r)) / 2
			if dist <= radius:
				coords.append(Vector2i(q, r))
	return coords


## 统一数据填充管线 - 将坐标数组转换为地图数据
## @param coords 六边形坐标数组（Vector2i）
## @param shape_type 形状类型："fan" 或 "circular"
## @param shape_params 形状参数（如 angle_span, radius 等）
func _populate_map_data(coords: Array[Vector2i], shape_type: String, shape_params: Dictionary = {}) -> void:
	# 初始化随机数生成器
	var rng = RandomNumberGenerator.new()
	if use_external_seed and received_text != "":
		rng.seed = received_text.hash()
	else:
		rng.randomize()
	
	# 清空现有地图数据
	map_data.clear()
	
	# 特殊处理：扇形地图的核心地块
	if shape_type == "fan":
		# 确保中心坐标 (0,0) 作为 nexus_core（使用 Vector2i 统一键类型）
		var center_coord = Vector2i(0, 0)
		map_data[center_coord] = {
			"height": nexus_height,
			"tier": 3,
			"terrain": "nexus_core",
			"terrain_type": "nexus_core",
			"landform": null
		}
	
	# 遍历所有坐标，填充高度和地形
	for coord_v2i in coords:
		# 使用 Vector2i 作为键，统一坐标类型
		var height: int = 0
		var tier: int = 1
		var terrain = ""  # 可存储 String 或 TerrainType 枚举
		
		# 根据形状类型计算高度和 tier
		match shape_type:
			"fan":
				# 计算距离（六边形轴向坐标距离）
				var q = coord_v2i.x
				var r = coord_v2i.y
				var dist = (abs(q) + abs(q + r) + abs(r)) / 2
				# 根据距离分配 tier
				if dist <= inner_tier_radius:
					tier = 3
				elif dist <= inner_tier_radius + 2:
					tier = 2
				else:
					tier = 1
				# 基于 tier 随机高度
				height = _roll_height_by_tier_with_rng(tier, rng)
				# 地形根据高度直接计算
				terrain = get_terrain_from_height(height)
			
			"circular":
				# 根据房间类型确定高度范围
				var h_min = base_h_min
				var h_max = base_h_max
				if room_type == "battle_elite":
					h_max += elite_h_bonus
				elif room_type == "boss_stage":
					h_max += elite_h_bonus + 1
				# 随机高度
				height = rng.randi_range(h_min, h_max)
				# 根据高度获取地形类型
				terrain = get_terrain_from_height(height)
			
			_:
				push_error("未知的形状类型: " + shape_type)
				continue
		
		# 构建地块数据
		var tile_data = {
			"height": height,
			"terrain": terrain,
			"terrain_type": terrain,
			"landform": null
		}
		
		# 扇形地图额外存储 tier 信息
		if shape_type == "fan":
			tile_data["tier"] = tier
		
		map_data[coord_v2i] = tile_data
	
	GameLogger.debug("地图数据填充完成，形状: " + shape_type + "，坐标数量: " + str(coords.size()), "HexMap")


## 带随机数生成器的 tier 高度滚动（用于可重复生成）
func _roll_height_by_tier_with_rng(tier: int, rng: RandomNumberGenerator) -> int:
	var roll = rng.randf()
	if tier == 3:
		return rng.randi_range(4, 6) if roll < 0.6 else rng.randi_range(2, 3)
	else:
		return rng.randi_range(1, 3) if roll < 0.7 else rng.randi_range(3, 4)


func _generate_fan_map_data():
	# 使用形状发生器模式：坐标采样 + 数据填充
	var coords = _get_fan_coords(fan_radius, fan_angle_span, 90.0)
	_populate_map_data(coords, "fan", {})
	GameLogger.info("扇形地图生成完成，坐标数量: " + str(coords.size()), "HexMap")

func _generate_circular_map_data():
	# 使用形状发生器模式：坐标采样 + 数据填充
	var coords = _get_circular_coords(map_radius)
	_populate_map_data(coords, "circular", {})
	GameLogger.info("圆形地图生成完成，坐标数量: " + str(coords.size()), "HexMap")

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


# ==========================================
# 2. 配额抽取逻辑
# ==========================================

#用_place_from_pool代替

## 假设你在开头定义了 var Max_Start_landform = 10 等变量
#func pick_landform():
	#var landform_in
	#var coord
	#var number = 0
	#
	## 阶段 1：先处理特殊属性池 (确保 property_pool 定义过，这里假定它是一个数组)
	## 你的代码里写了 property_pool，如果不报错说明你定义了。
	#if "property_pool" in self and typeof(self.property_pool) == TYPE_ARRAY and not self.property_pool.is_empty():
		#var max_prop = randi_range(1, max(1, Max_Start_landform - 1))
		#for i in range(max_prop):
			#landform_in = self.property_pool.pick_random()
			#coord = landform_pick(landform_in)
			#if coord != Vector2i(-100, -100):
				#var inst = landform_in.new(coord, self)
				#map_data[coord]["landform"] = inst
				#map_data[coord]["landform_type"] = inst.landform_name # 【修复 3】
				#number += 1
#
	## 阶段 2：保证基础池里的每个地貌至少生成一个（保底机制）
	#for landform_script in landform_pool:
		#coord = landform_pick(landform_script)
		#if coord == Vector2i(-100, -100):
			#GameLogger.warning("地貌保底生成失败，找不到合法位置", "HexMap")
			#continue
			#
		#var inst = landform_script.new(coord, self)
		#map_data[coord]["landform"] = inst
		#map_data[coord]["landform_type"] = inst.landform_name # 【修复 3】
		#number += 1
#
	## 阶段 3：用随机地貌填满剩下的配额
	#var remain = Max_Start_landform - number
	#if remain > 0 and landform_pool.size() > 0:
		#for i in range(remain):
			#landform_in = landform_pool.pick_random()
			#coord = landform_pick(landform_in)
			#if coord == Vector2i(-100, -100):
				#continue
			#var inst = landform_in.new(coord, self)
			#map_data[coord]["landform"] = inst
			#map_data[coord]["landform_type"] = inst.landform_name # 【修复 3】
# ==========================================
# 1. 核心分配逻辑 (严格遵循 35% 密度限制)
# ==========================================
func _assign_terrains_and_enemies():
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	
	# 【修复1】：清空上一局残留的高度池，并增加安全性校验
	if GlobalClock and "tile_h_pool" in GlobalClock:
		for h_key in GlobalClock.tile_h_pool.keys():
			GlobalClock.tile_h_pool[h_key].clear()

	var coords_list = map_data.keys()
	coords_list.shuffle()  # 随机打乱顺序，保证生成位置随机
	
	var valid_tile_count = 0 # 记录真正可以放置地貌的地块数量
	
	for coord in coords_list:
		var data = map_data[coord]
		
		# 跳过 nexus 核心
		if data.has("terrain") and str(data["terrain"]) == "nexus_core":
			continue
			
		valid_tile_count += 1
		
		var height = data["height"]
		var terrain_type = data["terrain_type"]
		
		if terrain_type == null:
			terrain_type = get_terrain_from_height(height)
			data["terrain_type"] = terrain_type
			
		# 填入全局高度池（防报错：如果键不存在则动态创建）
		if GlobalClock and "tile_h_pool" in GlobalClock:
			if not GlobalClock.tile_h_pool.has(height):
				GlobalClock.tile_h_pool[height] = []
			GlobalClock.tile_h_pool[height].append(coord)

	# 【核心机制】：应用 max_landform_ratio (0.35) 全局密度上限
	var absolute_max_landforms = int(valid_tile_count * max_landform_ratio)
	var current_placed = 0
	
	GameLogger.debug("准备放置地貌。可用地块数: %d, 最大容纳量(35%%): %d" % [valid_tile_count, absolute_max_landforms], "HexMap")

	# 优先放置中立地形（森林/雪山等），但不超过设定的最大值，也不超过全局总上限
	var neutral_to_place = min(Max_Neutral_landform, absolute_max_landforms - current_placed)
	current_placed += _place_from_pool(neutral_pool, neutral_to_place)
	
	# 随后放置敌对地形（村庄/碉堡等），利用剩余的总配额
	var enemy_to_place = min(Max_Enemy_landform, absolute_max_landforms - current_placed)
	current_placed += _place_from_pool(enemy_pool, enemy_to_place)
	
	GameLogger.info("地貌放置完毕。总计放置: %d (上限: %d)" % [current_placed, absolute_max_landforms], "HexMap")
# ==========================================
# 2. 通用抽取放置函数 (返回实际放置的数量)
# ==========================================
func _place_from_pool(pool: Array[Script], max_count: int) -> int:
	if pool.is_empty() or max_count <= 0:
		return 0
		
	var number = 0
	
	# 1. 保证池子里的每种地貌至少生成一个（保底机制）
	for landform_script in pool:
		if number >= max_count:
			break
			
		var coord = landform_pick(landform_script)
		if coord != Vector2i(-100, -100):
			var inst = landform_script.new(coord, self)
			map_data[coord]["landform"] = inst
			map_data[coord]["landform_type"] = inst.landform_name
			
			if inst.get("Attitude") == inst.Attitude_Pool.Enemy:
				inst.add_to_group("Enemies")
			elif inst.get("Attitude") == inst.Attitude_Pool.Middle:
				inst.add_to_group("Middle")
				
			number += 1

	# 2. 用随机地貌填满剩下的配额
	var remain = max_count - number
	if remain > 0:
		for i in range(remain):
			var landform_script = pool.pick_random()
			var coord = landform_pick(landform_script)
			if coord != Vector2i(-100, -100):
				var inst = landform_script.new(coord, self)
				map_data[coord]["landform"] = inst
				map_data[coord]["landform_type"] = inst.landform_name
				
				if inst.get("Attitude") == inst.Attitude_Pool.Enemy:
					inst.add_to_group("Enemies")
				elif inst.get("Attitude") == inst.Attitude_Pool.Middle:
					inst.add_to_group("Middle")
					
				number += 1
				
	return number
# ==========================================
# 3. 具体坐标筛选器 (使用探针机制)
# ==========================================
func landform_pick(landform_script: Script) -> Vector2i:
	# 实例化一个“探针”仅用于读取规则
	var buffer = landform_script.new(Vector2i(-100, -100), self)
	var buffer_pool: Array = []
	
	# 1. 圈定候选池（依据高度规则）
	if buffer.landform_rules.has("require_height") and buffer.landform_rules["require_height"] != null:
		var valid_heights = buffer.landform_rules["require_height"]
		# 从允许的高度中随机挑一个高度层
		var target_h = valid_heights[randi() % valid_heights.size()]
		if GlobalClock and "tile_h_pool" in GlobalClock and GlobalClock.tile_h_pool.has(target_h):
			buffer_pool = GlobalClock.tile_h_pool[target_h].duplicate()
	else:
		# 没有高度限制，全图可放
		buffer_pool = map_data.keys().duplicate()
		
	# 2. 如果候选池是空的，直接销毁探针返回失败
	if buffer_pool.is_empty():
		buffer.queue_free() 
		return Vector2i(-100, -100)

	# 3. 在候选池中随机抽取并验证
	var coord_index = randi() % buffer_pool.size()
	var test_coord = buffer_pool[coord_index]
	
	# 循环验证，如果该坐标不合法，踢出池子继续抽
	while not buffer_pool.is_empty() and not buffer.get_possible_coords(test_coord, map_data):
		buffer_pool.remove_at(coord_index)
		if buffer_pool.is_empty():
			break
		# 【修复2】：只有在不为空时才计算 modulo，防止除零崩溃
		coord_index = randi() % buffer_pool.size()
		test_coord = buffer_pool[coord_index]

	# 4. 销毁探针，返回结果
	buffer.queue_free()
	
	if buffer_pool.is_empty():
		return Vector2i(-100, -100)
	else:
		return test_coord
		
## 获取地形顶部纹理（支持 TerrainType 枚举或 String 类型）
func get_top_tex(terrain) -> Texture2D:
	# 处理 String 类型（如 "nexus_core"）
	if typeof(terrain) == TYPE_STRING:
		# 如果是特殊地块，返回默认纹理
		if terrain == "nexus_core":
			return hex_top_tex
		# 尝试将字符串转换为 TerrainType 枚举
		match terrain:
			"BEACH": return get_top_tex(TerrainType.BEACH)
			"PLAINS": return get_top_tex(TerrainType.PLAINS)
			"HILLS": return get_top_tex(TerrainType.HILLS)
			"MOUNTAIN": return get_top_tex(TerrainType.MOUNTAIN)
			_: return hex_top_tex
	
	# 处理 TerrainType 枚举类型
	var terrain_int = terrain as int
	if top_tex_by_terrain.size() > terrain_int and top_tex_by_terrain[terrain_int] != null:
		return top_tex_by_terrain[terrain_int]
	return hex_top_tex

## 获取地形侧面纹理（支持 TerrainType 枚举或 String 类型）
func get_side_tex(terrain) -> Texture2D:
	# 处理 String 类型（如 "nexus_core"）
	if typeof(terrain) == TYPE_STRING:
		# 如果是特殊地块，返回默认纹理
		if terrain == "nexus_core":
			return hex_side_tex
		# 尝试将字符串转换为 TerrainType 枚举
		match terrain:
			"BEACH": return get_side_tex(TerrainType.BEACH)
			"PLAINS": return get_side_tex(TerrainType.PLAINS)
			"HILLS": return get_side_tex(TerrainType.HILLS)
			"MOUNTAIN": return get_side_tex(TerrainType.MOUNTAIN)
			_: return hex_side_tex
	
	# 处理 TerrainType 枚举类型
	var terrain_int = terrain as int
	if side_tex_by_terrain.size() > terrain_int and side_tex_by_terrain[terrain_int] != null:
		return side_tex_by_terrain[terrain_int]
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


func _create_stack_at(coord: Vector2i, data: Dictionary):
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
	# 存储碰撞区引用，用于高度视图模式下的位置调整
	stack_container.set_meta("collision_node", collision)

	# 地貌生成（使用 landform 系统）
	var enemy_instance = null
	
	if data.has("landform") and data["landform"] != null:
		var landform_inst = data["landform"]
		enemy_instance = landform_inst
		enemy_instance.position = Vector2(hitbox_offset_x, top_block_y + hitbox_offset_y - 20)
		stack_container.add_child(enemy_instance)
		
		# 添加 landform 视觉精灵
		landform_inst.attach_visual(stack_container, height, current_step_h, tile_scale)
		
		# ★ 修复1：搜索并赋予地貌动态生成的 Sprite2D 材质
		for child in stack_container.get_children():
			if child is Sprite2D and child.name.begins_with("LandformSprite_"):
				if not sprites_in_stack.has(child):
					if block_material:
						child.material = block_material.duplicate()
						child.set_instance_shader_parameter("block_idx", float(height + 1))
						child.set_instance_shader_parameter("total_height", float(height + 2))
					sprites_in_stack.append(child)
					
		# ★ 修复2：如果敌人是 tscn 实例（如 Grass），提取它内部的 Sprite2D
		for child in enemy_instance.get_children():
			if child is Sprite2D:
				if block_material:
					child.material = block_material.duplicate()
					child.set_instance_shader_parameter("block_idx", float(height + 1))
					child.set_instance_shader_parameter("total_height", float(height + 2))
				# 将内部精灵也加入栈，确保它能获得高度视图的 Shader
				sprites_in_stack.append(child)
				
		landform_inst.owner_battle = self
		# ★ 新增：如果该地貌的态度是敌人，则加入 Enemies 组
		if landform_inst.Attitude == landform_inst.Attitude_Pool.Enemy:
			landform_inst.add_to_group("Enemies")
		GameLogger.debug("生成地貌: %s at %s" % [landform_inst.name, coord], "HexMap")
	
	# 旧的 enemy 系统已废弃，不再支持

	stack_container.set_meta("sprites", sprites_in_stack)
	stack_container.set_meta("height", height)
	stack_container.set_meta("occupant", enemy_instance)

	stack_container.mouse_entered.connect(_on_stack_hover.bind(stack_container, true))
	stack_container.mouse_exited.connect(_on_stack_hover.bind(stack_container, false))
	stack_container.input_event.connect(_on_stack_input.bind(stack_container))


## 局部刷新单个地块的视觉表现（优化性能，避免全局重绘）
## @param coord 六边形坐标（Vector2i）
func refresh_tile_visual(coord: Vector2i) -> void:
	if not map_data.has(coord):
		GameLogger.warning("尝试刷新不存在的地块坐标: " + str(coord), "HexMap")
		return
	
	# 获取地块数据
	var data = map_data[coord]
	
	# 如果已有视觉节点，先移除
	if stack_nodes.has(coord):
		var old_node = stack_nodes[coord]
		if is_instance_valid(old_node):
			old_node.queue_free()
		stack_nodes.erase(coord)
	
	# 重新创建视觉节点（_create_stack_at 使用 Vector2i 坐标）
	_create_stack_at(coord, data)
	GameLogger.debug("地块视觉刷新完成，坐标: " + str(coord), "HexMap")


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
	# 高度视图模式：使用专用点击处理
	if height_view_compressed:
		_on_stack_input_height_view(viewport, event, shape_idx, stack)
		return
	
	# 原始模式：保持原有逻辑
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
	# 视觉状态锁：防止拖拽时清除高亮和消融效果
	if is_visuals_locked:
		return
	
	# 高度视图模式：使用专用悬停处理
	if height_view_compressed:
		_on_stack_hover_height_view(stack, is_entered)
		return
	
	# 原始模式：保持原有逻辑
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
		if is_instance_valid(stack):
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
	# 检查是否存在对应的栈容器和地貌数据（使用 Vector2i 键）
	if not stack_nodes.has(coord):
		return
	if not map_data.has(coord):
		return
	
	var stack_container = stack_nodes[coord]
	var data = map_data[coord]
	
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


## ==========================================
## ★ 视角切换系统：压缩地块高度显示
## ==========================================

## 切换高度视角（压缩为1格高度或恢复原始高度）
func toggle_height_view() -> void:
	# ★ 动画状态锁：防止在动画播放期间重复触发
	if is_view_transitioning:
		GameLogger.warning("视图切换动画正在进行中，忽略重复操作", "HexMap")
		return
	
	# 设置动画状态锁
	is_view_transitioning = true
	GameLogger.debug("开始视图切换动画，锁定状态", "HexMap")
	
	# ★ 先切换视图状态标志，确保后续函数使用正确的状态
	height_view_compressed = not height_view_compressed
	
	# 根据新状态调用相应的动画函数
	if height_view_compressed:
		_compress_to_single_height_view()
	else:
		_restore_original_height_view()
	
	# ★ 延迟解锁：等待动画完成后重置状态锁
	# 最长的动画是0.5秒，设置0.6秒的延迟以确保安全
	await get_tree().create_timer(0.6).timeout
	is_view_transitioning = false
	GameLogger.debug("视图切换动画完成，解锁状态", "HexMap")

## 压缩为单格高度视图（物理坐标扁平化版本）
func _compress_to_single_height_view() -> void:
	GameLogger.info("切换到压缩高度视图（物理坐标扁平化）", "HexMap")
	
	# 清空材质和位置缓存
	height_view_original_materials.clear()
	height_view_hovered_stack = null
	height_view_selected_stack = null
	
	# 计算单格高度步长（用于坐标转换参考）
	var current_step_h = step_height * (tile_scale / REF_SCALE)
	
	for coord in stack_nodes.keys():
		var stack = stack_nodes[coord]
		if not is_instance_valid(stack):
			continue
		
		var sprites = stack.get_meta("sprites") as Array
		var height = stack.get_meta("height") as int
		var occupant = stack.get_meta("occupant") if stack.has_meta("occupant") else null  # 安全获取地貌/敌人实例
		
		# ★ 保存原始材质和位置，并应用高度视图shader
		var stack_sprites_data = []
		for sprite in sprites:
			if not is_instance_valid(sprite):
				stack_sprites_data.append(null)
				continue
			
			# 保存原始材质和位置引用
			var original_material = sprite.material
			var original_position = sprite.position  # 存储完整的Vector2位置
			stack_sprites_data.append({
				"sprite": sprite,
				"original_material": original_material,
				"original_position": original_position
			})
			
			# 应用高度视图shader（因为血条贴图已被收编进 sprites 数组，这里会自动给血条换上高度视图的Shader）
			if height_view_shader_material:
				sprite.material = height_view_shader_material.duplicate()
				# 设置默认shader参数
				sprite.set_instance_shader_parameter("click_highlight", 0.0)
				sprite.set_instance_shader_parameter("click_highlight_width", height_view_click_highlight_width)
				sprite.set_instance_shader_parameter("click_highlight_color", height_view_click_highlight_color)
		
		# ★ 保存地貌/敌人的原始位置（如果存在）
		var occupant_data = null
		if is_instance_valid(occupant):
			occupant_data = {
				"node": occupant,
				"original_position": occupant.position
			}
			
		# ★ 新增：探测并保存血条的原始位置
		var health_bar_data = null
		if is_instance_valid(occupant) and occupant.has_method("get_instance_id"):
			var bar_manager = get_node_or_null("BarManager")
			if bar_manager:
				var hb_node = bar_manager.get_node_or_null("HealthBar_" + str(occupant.get_instance_id()))
				if is_instance_valid(hb_node):
					health_bar_data = {
						"node": hb_node,
						"original_position": hb_node.position
					}
		
		# ★ 保存碰撞区的原始位置（如果存在）
		var collision_data = null
		var collision = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
		if is_instance_valid(collision):
			collision_data = {
				"node": collision,
				"original_position": collision.position
			}
		
		# 保存这个地块的完整原始数据
		height_view_original_materials[stack] = {
			"sprites_data": stack_sprites_data,
			"height": height,
			"occupant_data": occupant_data,
			"collision_data": collision_data,
			"health_bar_data": health_bar_data # 存入血条数据
		}
		
		var top_block_y = -(height - 1) * current_step_h
		var drop_delta = 0.0 - top_block_y  # 正数表示需要向下移动的距离
		
		if height <= 1:
			for sprite_data in stack_sprites_data:
				if sprite_data == null:
					continue
				var sprite = sprite_data["sprite"]
				if not is_instance_valid(sprite):
					continue
				
				# ★ 新增：核心修复点！
				# 只有直接挂在 stack（地块根节点）下的精灵，才允许在这里移动。
				# 因为血条精灵是挂在 hb_node 下的，如果不排除，它会跟着 hb_node 一起被移动两次。
				if sprite.get_parent() != stack:
					continue
				# =======================

				var target_y = sprite.position.y + drop_delta
				if abs(drop_delta) > 0.1:  
					_tween_position_y(sprite, target_y, 0.3)
			
			if occupant_data and is_instance_valid(occupant_data["node"]):
				var occupant_node = occupant_data["node"]
				var target_y = occupant_node.position.y + drop_delta
				if abs(drop_delta) > 0.1:
					_tween_position_y(occupant_node, target_y, 0.3)
					
			# 同步降落血条根节点 (hb_node 会带着它里面的进度条一起下落)
			if health_bar_data and is_instance_valid(health_bar_data["node"]):
				var hb_node = health_bar_data["node"]
				var target_y = hb_node.position.y + drop_delta
				if abs(drop_delta) > 0.1:
					_tween_position_y(hb_node, target_y, 0.3)
			
			if is_instance_valid(collision):
				var target_y = collision.position.y + drop_delta
				if abs(drop_delta) > 0.1:
					_tween_position_y(collision, target_y, 0.3)
			
			_create_height_indicator(stack, height)
			continue
		
		# ==========================================
		# 高度>1的地块处理逻辑
		# ==========================================
		
		for i in range(sprites.size()):
			var sprite = sprites[i]
			if not is_instance_valid(sprite):
				continue
			
			if i < height - 1:  # 侧面地形精灵
				_tween_shader_param_single(sprite, "dissolve_blend", 1.0, 0.5)
			else:  # 顶部地形精灵、地貌实体精灵、血条精灵
				_tween_shader_param_single(sprite, "dissolve_blend", 0.0, 0.1)
				
				# 修复：只有直接挂在 stack 下的精灵单独调整位置
				# 血条精灵的父级是 HealthBar 根节点，不是 stack，所以会跳过这一步，避免双重下落
				if sprite.get_parent() == stack:
					var target_y = sprite.position.y + drop_delta
					_tween_position_y(sprite, target_y, 0.5)
		
		# 移动地貌/敌人
		if is_instance_valid(occupant):
			var target_y = occupant.position.y + drop_delta
			_tween_position_y(occupant, target_y, 0.5)
			
		# ★ 移动血条UI节点
		if is_instance_valid(health_bar_data) and is_instance_valid(health_bar_data["node"]):
			var hb_node = health_bar_data["node"]
			var target_y = hb_node.position.y + drop_delta
			_tween_position_y(hb_node, target_y, 0.5)
		
		# 移动碰撞区
		if is_instance_valid(collision):
			var target_y = collision.position.y + drop_delta
			_tween_position_y(collision, target_y, 0.5)
		
		_create_height_indicator(stack, height)

## 恢复原始高度视图（物理坐标扁平化版本）
func _restore_original_height_view() -> void:
	GameLogger.info("恢复原始高度视图（恢复物理坐标）", "HexMap")
	
	for stack_key in height_view_pillar_tweens.keys():
		var tween = height_view_pillar_tweens[stack_key]
		if is_instance_valid(tween):
			tween.stop()
	height_view_pillar_tweens.clear()
	
	var total_stacks = 0
	var total_sprites = 0
	var total_occupants = 0
	
	for coord in stack_nodes.keys():
		var stack = stack_nodes[coord]
		if not is_instance_valid(stack):
			continue
		
		total_stacks += 1
		
		if height_view_original_materials.has(stack):
			var stack_data = height_view_original_materials[stack] as Dictionary
			var stack_sprites_data = stack_data.get("sprites_data", []) as Array
			var occupant_data = stack_data.get("occupant_data")
			var collision_data = stack_data.get("collision_data")
			var health_bar_data = stack_data.get("health_bar_data") # 取出血条数据
			
			# 1. 恢复精灵
			for sprite_data in stack_sprites_data:
				if sprite_data == null:
					continue
				
				var sprite = sprite_data["sprite"]
				var original_material = sprite_data.get("original_material")
				var original_position = sprite_data.get("original_position", Vector2.ZERO)
				
				if is_instance_valid(sprite):
					total_sprites += 1
					if original_material:
						sprite.material = original_material
					
					var current_y = sprite.position.y
					var target_y = original_position.y
					if abs(current_y - target_y) > 0.1:
						_tween_position_y(sprite, target_y, 0.5)
			
			# 2. 恢复地貌/敌人
			if occupant_data and is_instance_valid(occupant_data["node"]):
				var occupant_node = occupant_data["node"]
				var original_position = occupant_data.get("original_position", Vector2.ZERO)
				var current_y = occupant_node.position.y
				var target_y = original_position.y
				if abs(current_y - target_y) > 0.1:
					total_occupants += 1
					_tween_position_y(occupant_node, target_y, 0.5)
					
			# 3. ★ 恢复血条UI位置
			if health_bar_data and is_instance_valid(health_bar_data["node"]):
				var hb_node = health_bar_data["node"]
				var original_position = health_bar_data.get("original_position", Vector2.ZERO)
				var current_y = hb_node.position.y
				var target_y = original_position.y
				if abs(current_y - target_y) > 0.1:
					_tween_position_y(hb_node, target_y, 0.5)
			
			# 4. 恢复碰撞区
			if collision_data and is_instance_valid(collision_data["node"]):
				var collision_node = collision_data["node"]
				var original_position = collision_data.get("original_position", Vector2.ZERO)
				var current_y = collision_node.position.y
				var target_y = original_position.y
				if abs(current_y - target_y) > 0.1:
					_tween_position_y(collision_node, target_y, 0.5)
		
		var sprites = stack.get_meta("sprites") as Array
		var height = stack.get_meta("height") as int
		
		for sprite in sprites:
			if not is_instance_valid(sprite) or not sprite.material:
				continue
			sprite.set_instance_shader_parameter("dissolve_blend", 0.0)
		
		for sprite in sprites:
			if not is_instance_valid(sprite):
				continue
			_tween_shader_param_single(sprite, "dissolve_blend", 0.0, 0.3)
		
		_remove_height_indicator(stack)
	
	GameLogger.info("恢复完成，总计: " + str(total_stacks) + "个地块, " + str(total_sprites) + "个精灵, " + str(total_occupants) + "个敌人/地貌", "HexMap")
	
	height_view_hovered_stack = null
	height_view_selected_stack = null
	height_view_original_materials.clear()

## 为单个精灵设置shader参数补间（辅助函数）
# ★ 修复：移除强类型限制 (删除了 : Sprite2D)，以兼容 UI 组件 (TextureProgressBar 等)
func _tween_shader_param_single(sprite, param_name: String, target_val: float, duration: float) -> void:
	if not is_instance_valid(sprite) or not sprite.material:
		return
	
	var current_val = sprite.get_instance_shader_parameter(param_name)
	if current_val == null:
		current_val = 0.0
	
	# 创建补间动画
	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tw.tween_method(func(val: float):
		if is_instance_valid(sprite) and sprite.material:
			sprite.set_instance_shader_parameter(param_name, val)
	, current_val, target_val, duration)

## 为任意节点设置Y坐标补间（辅助函数，用于物理坐标扁平化）
# ★ 修复：移除强类型限制 (删除了 : Node2D)，以兼容 UI 组件 (Control 虽然没继承 Node2D 但也有 position)
func _tween_position_y(node, target_y: float, duration: float) -> void:
	if not is_instance_valid(node):
		return
	
	var current_y = node.position.y
	
	# 创建补间动画
	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tw.tween_method(func(y_val: float):
		if is_instance_valid(node):
			node.position.y = y_val
	, current_y, target_y, duration)

## 创建高度指示器（光柱 + 高度标签）使用导出参数
func _create_height_indicator(stack: Area2D, original_height: int) -> void:
	if not is_instance_valid(stack):
		return
	
	_remove_height_indicator(stack)
	
	var sprites = stack.get_meta("sprites") as Array
	if sprites.is_empty():
		return
	
	if original_height <= 0 or original_height - 1 >= sprites.size():
		return
	
	var top_sprite = sprites[original_height - 1]
	if not is_instance_valid(top_sprite):
		return
	
	var pillar_position: Vector2
	if height_view_compressed:
		pillar_position = Vector2(hitbox_offset_x, hitbox_offset_y) + height_view_pillar_offset
	else:
		pillar_position = Vector2(hitbox_offset_x, top_sprite.position.y + hitbox_offset_y) + height_view_pillar_offset

	# --- 修改点 1：控制光柱生成 ---
	if show_height_pillars:
		var line = Line2D.new()
		line.name = "HeightIndicatorLine"
		line.width = height_view_pillar_width
		line.default_color = height_view_pillar_color
		
		var pillar_length = height_view_pillar_length * original_height * 0.5
		var min_pillar_length = height_view_pillar_length * 1.5
		if pillar_length < min_pillar_length:
			pillar_length = min_pillar_length
		
		line.points = PackedVector2Array([Vector2(0, 0), Vector2(0, -pillar_length)])
		line.position = pillar_position
		line.z_index = 1000
		stack.add_child(line)
		stack.set_meta("height_indicator_line", line)
		stack.set_meta("height_view_pillar", line)

	# --- 修改点 2：控制数字标签生成 ---
	if show_height_labels:
		var label = Label.new()
		label.name = "HeightIndicatorLabel"
		label.text = str(original_height)
		
		if height_label_font:
			label.add_theme_font_override("font", height_label_font)
		
		label.add_theme_font_size_override("font_size", height_label_font_size)
		label.add_theme_color_override("font_color", height_label_color)
		label.add_theme_color_override("font_outline_color", height_label_outline_color)
		label.add_theme_constant_override("outline_size", height_label_outline_size)
		
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		label.position = pillar_position + height_label_offset
		label.z_index = 1001
		stack.add_child(label)
		stack.set_meta("height_indicator_label", label)

## 移除高度指示器
func _remove_height_indicator(stack: Area2D) -> void:
	if not is_instance_valid(stack):
		return
	
	if stack.has_meta("height_indicator_line"):
		var line = stack.get_meta("height_indicator_line")
		if is_instance_valid(line):
			line.queue_free()
		stack.set_meta("height_indicator_line", null)
	
	if stack.has_meta("height_indicator_label"):
		var label = stack.get_meta("height_indicator_label")
		if is_instance_valid(label):
			label.queue_free()
		stack.set_meta("height_indicator_label", null)
	
	# 清除光柱引用（如果存在）
	if stack.has_meta("height_view_pillar"):
		stack.set_meta("height_view_pillar", null)


## ==========================================
## ★ 视角切换按钮 UI
## ==========================================



## 按钮按下回调
func _on_height_view_toggle_pressed() -> void:
	toggle_height_view()

## ==========================================
## ★ 高度视图鼠标交互系统
## ==========================================

## 开始光柱浮动动画（鼠标悬停时调用）
func _start_pillar_floating_animation(stack: Area2D) -> void:
	if not height_view_compressed or not is_instance_valid(stack):
		return
	
	# 获取光柱引用
	var pillar = stack.get_meta("height_view_pillar") if stack.has_meta("height_view_pillar") else null
	if not is_instance_valid(pillar):
		GameLogger.debug("光柱引用无效，无法启动浮动动画", "HexMap")
		return
	
	GameLogger.debug("开始光柱浮动动画，幅度: " + str(height_view_hover_amplitude) + "，速度: " + str(height_view_hover_speed), "HexMap")
	
	# 停止现有动画（如果存在）
	if height_view_pillar_tweens.has(stack):
		var existing_tween = height_view_pillar_tweens[stack]
		if is_instance_valid(existing_tween):
			existing_tween.stop()
	
	# 创建新的浮动动画
	var original_y = pillar.position.y
	var tween = create_tween()
	tween.set_loops()
	tween.set_trans(Tween.TRANS_SINE)
	tween.set_ease(Tween.EASE_IN_OUT)
	
	# 上下浮动动画
	tween.tween_property(pillar, "position:y", original_y - height_view_hover_amplitude, height_view_hover_speed / 2.0)
	tween.tween_property(pillar, "position:y", original_y + height_view_hover_amplitude, height_view_hover_speed)
	tween.tween_property(pillar, "position:y", original_y, height_view_hover_speed / 2.0)
	
	# 保存动画引用
	height_view_pillar_tweens[stack] = tween

## 停止光柱浮动动画（鼠标离开时调用）
func _stop_pillar_floating_animation(stack: Area2D) -> void:
	if not height_view_compressed or not is_instance_valid(stack):
		return
	
	# 停止动画
	if height_view_pillar_tweens.has(stack):
		var tween = height_view_pillar_tweens[stack]
		if is_instance_valid(tween):
			tween.stop()
		
		# 恢复原始位置
		var pillar = stack.get_meta("height_view_pillar") if stack.has_meta("height_view_pillar") else null
		if is_instance_valid(pillar):
			var original_y = pillar.position.y
			# 快速平滑返回
			var restore_tween = create_tween()
			restore_tween.set_trans(Tween.TRANS_SINE)
			restore_tween.set_ease(Tween.EASE_IN_OUT)
			restore_tween.tween_property(pillar, "position:y", original_y, 0.2)

## 应用点击高亮效果（点击地块时调用）
func _apply_click_highlight(stack: Area2D) -> void:
	if not height_view_compressed or not is_instance_valid(stack):
		return
	
	# 移除之前选中的地块高亮
	if is_instance_valid(height_view_selected_stack) and height_view_selected_stack != stack:
		_remove_click_highlight(height_view_selected_stack)
	
	# 应用新地块高亮
	height_view_selected_stack = stack
	
	# 通过shader参数应用白色边框
	var sprites = stack.get_meta("sprites") as Array
	for sprite in sprites:
		if not is_instance_valid(sprite) or not sprite.material:
			continue
		
		# 设置点击高亮参数
		sprite.set_instance_shader_parameter("click_highlight", 1.0)
		sprite.set_instance_shader_parameter("click_highlight_width", height_view_click_highlight_width)
		sprite.set_instance_shader_parameter("click_highlight_color", height_view_click_highlight_color)

## 移除点击高亮效果
func _remove_click_highlight(stack: Area2D) -> void:
	if not is_instance_valid(stack):
		return
	
	var sprites = stack.get_meta("sprites") as Array
	for sprite in sprites:
		if not is_instance_valid(sprite) or not sprite.material:
			continue
		
		# 移除点击高亮
		sprite.set_instance_shader_parameter("click_highlight", 0.0)

## 高度视图下的鼠标悬停处理
func _on_stack_hover_height_view(stack: Area2D, is_entered: bool) -> void:
	# 视觉状态锁：防止拖拽时清除高亮和消融效果
	if is_visuals_locked:
		return
	
	if not height_view_compressed:
		return
	
	if is_entered:
		# 鼠标进入：开始光柱浮动动画
		GameLogger.debug("高度视图鼠标进入地块，启动光柱动画", "HexMap")
		height_view_hovered_stack = stack
		_start_pillar_floating_animation(stack)
	else:
		# 鼠标离开：停止光柱浮动动画
		GameLogger.debug("高度视图鼠标离开地块，停止光柱动画", "HexMap")
		if height_view_hovered_stack == stack:
			height_view_hovered_stack = null
		_stop_pillar_floating_animation(stack)

## 高度视图下的鼠标点击处理
func _on_stack_input_height_view(viewport: Node, event: InputEvent, shape_idx: int, stack: Area2D) -> void:
	if not height_view_compressed:
		return
	
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			# 应用点击高亮效果
			_apply_click_highlight(stack)
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			# 右键取消高亮
			if height_view_selected_stack == stack:
				_remove_click_highlight(stack)
				height_view_selected_stack = null


## 统一开启或关闭所有地块的鼠标交互
func set_tiles_interactive(enabled: bool) -> void:
	for stack in stack_nodes.values():
		if is_instance_valid(stack) and stack is Area2D:
			stack.input_pickable = enabled
	GameLogger.debug("地块交互状态已设置为: " + str(enabled), "HexMap")


## 锁定或解锁地块的视觉状态（保留当前的高亮和消融效果）
func set_visuals_locked(locked: bool) -> void:
	is_visuals_locked = locked
	GameLogger.debug("地块视觉状态锁已设置为: " + str(locked), "HexMap")
	# 解锁时，强制清理一下可能残留的错误状态（因为鼠标可能已经移走了）
	if not locked:
		hovered_stacks.clear()
		_update_highlight()

# ==========================================
# ★ 血条 (HealthBuffer) 视觉同步注册系统
# ==========================================
## 当血条生成后，将其视觉组件加入到对应的渲染栈中，以便统一Shader和高亮
func recollect_sprites_for_landform(landform_obj: landform) -> void:
	var coord = landform_obj.location
	# 修复：使用你真实的存储字典 stack_nodes
	if not stack_nodes.has(coord): 
		return
	
	var stack = stack_nodes[coord]
	if not is_instance_valid(stack):
		return
		
	var sprites_list = stack.get_meta("sprites") as Array
	var height = stack.get_meta("height") as int
	
	# 在 BarManager 中寻找属于这个 landform 的血条 (HealthBuffer模式)
	var bar_manager = get_node_or_null("BarManager")
	if bar_manager:
		var bar_node = bar_manager.get_node_or_null("HealthBar_" + str(landform_obj.get_instance_id()))
		if bar_node:
			# 寻找血条内部的所有 Sprite2D 或 TextureRect 并加入精灵列表
			_find_and_register_ui_sprites(bar_node, sprites_list, height)

# ==========================================
# ★ 血条等外部节点的视觉同步注册系统
# ==========================================

## 接收外部节点（如血条），将其内部的贴图加入地块渲染序列
func register_extra_render_node(coord: Vector2i, node: Node) -> void:
	if not stack_nodes.has(coord): 
		GameLogger.warning("注册血条失败：找不到地块坐标 " + str(coord), "HexMap")
		return
	
	var stack = stack_nodes[coord]
	if not is_instance_valid(stack): return
	
	var sprites_list = stack.get_meta("sprites") as Array
	var height = stack.get_meta("height") as int
	
	# 开始递归寻找并注册 UI 贴图
	_find_and_register_ui_sprites(node, sprites_list, height)

## 递归寻找 UI 内部的贴图并加入动画更新队列
func _find_and_register_ui_sprites(node: Node, list: Array, height: int) -> void:
	if node is Sprite2D or node is TextureRect or node is TextureProgressBar:
		if not list.has(node):
			# ★ 核心修复：只传递高度参数并加入队列，绝对不覆盖它本身的 Material！
			# 只要传递了这两个参数，上面的 ui_health_bar.gdshader 就会完美执行悬浮和起伏！
			if node.material:
				node.set_instance_shader_parameter("block_idx", float(height + 1))
				node.set_instance_shader_parameter("total_height", float(height + 2))
			
			list.append(node) 
			GameLogger.debug("🩸 成功将血条组件收编进地块渲染序列: " + node.name, "HexMap")
			
	for child in node.get_children():
		_find_and_register_ui_sprites(child, list, height)

# ==========================================
# ★ 动态地形升降引擎 (支持多组件同步)
# ==========================================
func animate_elevation_change(stack: Area2D, delta_height: int) -> void:
	if delta_height == 0 or not is_instance_valid(stack): return
	
	var old_height = stack.get_meta("height") as int
	var new_height = clampi(old_height + delta_height, 1, 99) # 限制高度下限为1
	var actual_delta = new_height - old_height
	if actual_delta == 0: return

	# 1. 搜集需要一起移动的所有视觉组件（地形、碰撞箱、建筑、血条）
	var moving_parts = []
	var sprites = stack.get_meta("sprites") as Array
	moving_parts.append_array(sprites)
	
	var collision = stack.get_meta("collision_node")
	if is_instance_valid(collision): moving_parts.append(collision)
	
	var occupant = stack.get_meta("occupant")
	if is_instance_valid(occupant): 
		moving_parts.append(occupant)
		# 抓取悬浮在外部的血条
		var bar_manager = get_node_or_null("BarManager")
		if bar_manager:
			var hb_node = bar_manager.get_node_or_null("HealthBar_" + str(occupant.get_instance_id()))
			if is_instance_valid(hb_node): moving_parts.append(hb_node)

	# 2. 更新底层数据字典
	stack.set_meta("height", new_height)
	for coord in stack_nodes.keys():
		if stack_nodes[coord] == stack:
			map_data[coord]["height"] = new_height
			break

	var current_step_h = step_height * (tile_scale / REF_SCALE)
	var y_offset = -(actual_delta * current_step_h) # Godot中向上是负Y方向

	# 3. 动画序列：震动 -> 升降 -> 底层方块增删
	var tw = create_tween()
	
	# 【阶段A：剧烈震动】
	var shake_steps = 6
	var step_time = ele_shake_duration / shake_steps
	var original_positions = {}
	
	for part in moving_parts:
		if is_instance_valid(part):
			original_positions[part] = part.position
	
	for i in range(shake_steps):
		for part in moving_parts:
			if is_instance_valid(part):
				var rand_offset = Vector2(randf_range(-ele_shake_intensity, ele_shake_intensity), 0)
				tw.tween_property(part, "position", original_positions[part] + rand_offset, step_time)
				
	# 【阶段B：平滑升降】
	tw.parallel() # 确保震动后立刻无缝平移
	for part in moving_parts:
		if is_instance_valid(part):
			var target_pos = original_positions[part] + Vector2(0, y_offset)
			tw.parallel().tween_property(part, "position", target_pos, ele_anim_duration)\
				.set_trans(ele_trans_type).set_ease(ele_ease_type)

	# 【阶段C：视觉方块补齐或摧毁】(动画结束瞬间执行)
	tw.tween_callback(func():
		if actual_delta > 0:
			# 抬升：在最底层补充方块
			var coord = stack_nodes.find_key(stack)
			var terrain_type = map_data[coord]["terrain_type"]
			var side_tex = get_side_tex(terrain_type)
			
			for i in range(old_height, new_height):
				var new_sprite = Sprite2D.new()
				new_sprite.texture = side_tex
				new_sprite.centered = false
				new_sprite.offset = Vector2(-256, -400)
				# 位置放在原最底层的下方
				new_sprite.position.y = original_positions[sprites[0]].y + (i - old_height + 1) * current_step_h
				new_sprite.scale = Vector2(tile_scale, tile_scale)
				new_sprite.modulate = Color(0.8, 0.8, 0.8) # 侧边调暗
				
				if block_material:
					new_sprite.material = block_material.duplicate()
					new_sprite.set_instance_shader_parameter("block_idx", float(i))
					
				stack.add_child(new_sprite)
				stack.move_child(new_sprite, 0) # 移入渲染最底层
				sprites.insert(0, new_sprite)
				
		elif actual_delta < 0:
			# 下降：从底层开始摧毁被掩盖的方块
			var remove_count = abs(actual_delta)
			for i in range(remove_count):
				if sprites.size() > 1: # 保护至少剩一层地皮
					var bottom_sprite = sprites[0]
					sprites.pop_front()
					bottom_sprite.queue_free()
					
		# 更新Shader总高度，防止消融特效断层
		for i in range(sprites.size()):
			if is_instance_valid(sprites[i]) and sprites[i].material:
				sprites[i].set_instance_shader_parameter("total_height", float(new_height))
				
		GameLogger.info("⛰️ 地块升降完毕，坐标新高度: " + str(new_height), "HexMap")
	)

# 在 hex_map.gd (class_name battle) 中添加：

## 安全获取地块上的占位实体（地貌或敌人）
func get_entity_at_hex(coord: Vector2i) -> Node:
	if not stack_nodes.has(coord):
		return null
	
	var stack = stack_nodes[coord]
	if is_instance_valid(stack) and stack.has_meta("occupant"):
		var entity = stack.get_meta("occupant")
		if is_instance_valid(entity):
			return entity
	return null

## 检查实体是否存活/有效
func is_entity_alive(entity: Node) -> bool:
	if not is_instance_valid(entity) or entity.is_queued_for_deletion():
		return false
	if entity.has("HP") and entity.HP <= 0:
		return false
	return true
