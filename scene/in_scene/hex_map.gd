# 原文件名: hex_map(地块生成).gd
# 功能: 地块生成与战斗地图管理
extends Node2D
class_name battle

const ENEMY_INTENT_FRAME_TEXTURE: Texture2D = preload("res://image/texture/hexagon_frame.png")
const ENEMY_INTENT_TARGET_SHADER: Shader = preload("res://shaders/enemy_intent_target_ripple.gdshader")
#血条信号测试用
signal CreateBar(landform_in: landform, situation: int, x: float, y: float)
signal enemy_roster_changed
signal tile_topology_changed
@export_group("Assets")
@export var hex_top_tex: Texture2D
@export var hex_side_tex: Texture2D
@export var block_material: ShaderMaterial
@export var label_bg_tex: Texture2D

@export_group("敌人意图地图表现")
## 地图意图 overlay 统一使用的框体贴图。推荐使用透明背景的六边形描边。
@export var enemy_intent_frame_tex: Texture2D
## 目标波纹 overlay 使用的独立 shader。若不手动指定，运行时自动回退到默认 shader。
@export var enemy_intent_target_shader: Shader
## 地图意图 overlay 的整体缩放倍率，1.0 为紧贴 hitbox，略大于 1 可以让边框更清晰。
@export var enemy_intent_overlay_scale: float = 1.18
## 地图意图 overlay 的层级，确保高于普通地块贴图与建筑贴图。
@export var enemy_intent_overlay_z_index: int = 200
## 施法者高亮描边粗细。逻辑来源于原 hex shader 的 highlight_width。
@export var enemy_intent_source_highlight_width: float = 4.0
## 目标波纹的运动速度。
@export var enemy_intent_target_ripple_speed: float = 3.2
## 目标波纹的密度（越高波纹越密）。
@export var enemy_intent_target_ripple_density: float = 20.0
## 目标波纹最小透明度。
@export var enemy_intent_target_min_alpha: float = 0.2
## 目标波纹最大透明度。
@export var enemy_intent_target_max_alpha: float = 0.95

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
@export var ele_anim_duration: float = 0.4  # 升降过程的耗时
@export var ele_shake_intensity: float = 6.0 # 升降前地壳震动的像素幅度
@export var ele_shake_duration: float = 0.2  # 地壳震动的准备时间
@export var ele_trans_type: Tween.TransitionType = Tween.TRANS_ELASTIC # 弹性缓冲，效果最好
@export var ele_ease_type: Tween.EaseType = Tween.EASE_OUT
@export var elevation_move_distance: float = 48.0 #地块升降高度控制
# ★ 新增：控制物理补块（侧面贴图）生成时的垂直间距
@export var filler_block_spacing: float = 48.0

@export_group("高度限制设置")
@export var max_height: int = 7  ## 超过此高度地块会崩塌
@export var min_height: int = 0  ## 低于此高度地块会湮灭

# ==========================================
# ★ 视觉状态机定义
# ==========================================
enum TileVisualState {
	IDLE, HOVER_TARGET_VALID, HOVER_TARGET_INVALID, AOE_RANGE
}

# ★ 新增：全局视角状态机
enum MapViewState { 
	VIEW_3D, 
	VIEW_FLAT 
}
var current_view_state: MapViewState = MapViewState.VIEW_3D

var current_aoe_stacks: Array[Area2D] = []
var landform_pool: Array[Script] = []  # 地貌脚本池，在 _ready 中初始化
var neutral_pool: Array[Script] = []
var enemy_pool: Array[Script] = []

var map_data: Dictionary = { }
# ★ 优化：新增栈缓存字典，方便 O(1) 查找遮挡物
var stack_nodes: Dictionary = { }
var height_view_original_materials: Dictionary = {} #优化后字典，上面那个可能可以删除 TODO 
#var height_view_compressed: bool = false  # 视角切换状态：是否压缩为1格高度
var height_view_hovered_stack: Area2D = null  # 高度视图下鼠标悬浮的地块
var height_view_selected_stack: Area2D = null  # 高度视图下点击选中的地块
#var height_view_original_materials: Dictionary = {}  # 存储地块原始材质（用于恢复）
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
var current_enemy_intent_source_stack: Area2D = null
var current_enemy_intent_target_stacks: Array[Area2D] = []
var current_enemy_intent_source_restore_state: Dictionary = {}

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
	if enemy_intent_frame_tex == null:
		enemy_intent_frame_tex = ENEMY_INTENT_FRAME_TEXTURE
	if enemy_intent_target_shader == null:
		enemy_intent_target_shader = ENEMY_INTENT_TARGET_SHADER
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
		#preload("res://scene/in_scene/enermy/blockhouse.gd")
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
	enemy_roster_changed.emit()


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
	stack_nodes[coord] = stack_container

	var terrain_type = data.get("terrain_type", TerrainType.PLAINS)
	var top_tex = get_top_tex(terrain_type)
	var side_tex = get_side_tex(terrain_type)

	var sprites_in_stack = []
	for i in range(height):
		var sprite = Sprite2D.new()
		sprite.texture = top_tex if i == height - 1 else side_tex
		sprite.centered = false
		sprite.offset = Vector2(-256, -400)
		sprite.scale = Vector2(tile_scale, tile_scale)

		if block_material:
			sprite.material = block_material.duplicate()
			sprite.set_instance_shader_parameter("block_idx", float(i))
			sprite.set_instance_shader_parameter("total_height", float(height))
		
		# ★ 核心修复：生成方块时，严格判断当前是否处于平铺视图
		if current_view_state == MapViewState.VIEW_FLAT:
			if sprite.material:
				sprite.material.set_instance_shader_parameter("is_flat_view", 1.0)
			if i < height - 1:
				sprite.visible = false
				sprite.modulate.a = 0.0
			else:
				sprite.position.y = 0.0
		else:
			# 3D 视图正常生成
			sprite.position.y = -i * current_step_h
			if i < height - 1: sprite.modulate = Color(1.0, 1.0, 1.0)

		stack_container.add_child(sprite)
		sprites_in_stack.append(sprite)

	var collision = CollisionPolygon2D.new()
	var w = hitbox_width
	var h = hitbox_base_height
	collision.polygon = PackedVector2Array([
		Vector2(-w / 4.0, -h / 2.0), Vector2(w / 4.0, -h / 2.0), Vector2(w / 2.0, 0),
		Vector2(w / 4.0, h / 2.0), Vector2(-w / 4.0, h / 2.0), Vector2(-w / 2.0, 0)
	])
	
	# ★ 核心修复：如果是平铺视角，碰撞格必须贴地
	var top_block_y = 0.0 if current_view_state == MapViewState.VIEW_FLAT else -(height - 1) * current_step_h
	collision.position = Vector2(hitbox_offset_x, top_block_y + hitbox_offset_y)
	stack_container.add_child(collision)
	stack_container.set_meta("collision_node", collision)

	var enemy_instance = null
	if data.has("landform") and data["landform"] != null:
		var landform_inst = data["landform"]
		enemy_instance = landform_inst
		enemy_instance.position = Vector2(hitbox_offset_x, top_block_y + hitbox_offset_y - 20)
		stack_container.add_child(enemy_instance)
		
		landform_inst.attach_visual(stack_container, height, current_step_h, tile_scale)
		
		for child in stack_container.get_children():
			if child is Sprite2D and child.name.begins_with("LandformSprite_"):
				if not sprites_in_stack.has(child):
					if block_material:
						child.material = block_material.duplicate()
						child.set_instance_shader_parameter("block_idx", float(height + 1))
						child.set_instance_shader_parameter("total_height", float(height + 2))
						if current_view_state == MapViewState.VIEW_FLAT:
							child.material.set_instance_shader_parameter("is_flat_view", 1.0)
					sprites_in_stack.append(child)
					
		for child in enemy_instance.get_children():
			if child is Sprite2D:
				if block_material:
					child.material = block_material.duplicate()
					child.set_instance_shader_parameter("block_idx", float(height + 1))
					child.set_instance_shader_parameter("total_height", float(height + 2))
					if current_view_state == MapViewState.VIEW_FLAT:
						child.material.set_instance_shader_parameter("is_flat_view", 1.0)
				sprites_in_stack.append(child)
				
		landform_inst.owner_battle = self
		if landform_inst.Attitude == landform_inst.Attitude_Pool.Enemy:
			landform_inst.add_to_group("Enemies")
	
	stack_container.set_meta("sprites", sprites_in_stack)
	stack_container.set_meta("height", height)
	stack_container.set_meta("occupant", enemy_instance)

	stack_container.mouse_entered.connect(_on_stack_hover.bind(stack_container, true))
	stack_container.mouse_exited.connect(_on_stack_hover.bind(stack_container, false))
	stack_container.input_event.connect(_on_stack_input.bind(stack_container))
	
	# 如果处于平铺视角，补齐顶部的数字标签
	if current_view_state == MapViewState.VIEW_FLAT:
		_create_height_indicator(stack_container, height)

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
	# CardManager 由 ui/Main(in_scene.gd) 在运行时创建，统一从 MainBoard 获取。
	var main_board = get_tree().get_first_node_in_group("MainBoard")
	if main_board and main_board.get("manager_instance"):
		return main_board.manager_instance
	
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
# ★ 统一的点击输入处理 (支持 3D & 平铺视图)
# ==========================================
func _on_stack_input(viewport: Node, event: InputEvent, shape_idx: int, stack: Area2D):
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			_handle_tile_click(stack)
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			_cancel_card_selection()

## 处理地块左键点击：无论何种视图，逻辑统一下沉
func _handle_tile_click(stack: Area2D) -> void:
	if not is_instance_valid(stack): return
	
	var cm = get_card_manager()
	if not cm: return
	
	var active_card = cm.get("current_selected_card")
	
	# 如果手中有卡牌，尝试打出卡牌
	if is_instance_valid(active_card):
		if active_card.has_method("play_card"):
			active_card.play_card(stack)
			_clear_all_aoe_highlights()
			selected_stack = null
	else:
		# 如果手中没有卡牌，无论平铺还是3D，统一使用状态机高亮
		if is_instance_valid(selected_stack) and selected_stack != stack:
			change_tile_state(selected_stack, TileVisualState.IDLE)
		selected_stack = stack
		change_tile_state(selected_stack, TileVisualState.HOVER_TARGET_VALID)
# ==========================================
# ★ 悬浮与多地块 AOE 遮罩检测
# ==========================================
func _on_stack_hover(stack: Area2D, is_entered: bool):
	if is_visuals_locked: return
	
	# 高度平铺视图的光柱动画保留
	if current_view_state == MapViewState.VIEW_FLAT:
		if is_entered:
			height_view_hovered_stack = stack
			_start_pillar_floating_animation(stack)
		else:
			if height_view_hovered_stack == stack:
				height_view_hovered_stack = null
			_stop_pillar_floating_animation(stack)
			
	# 不管是哪种视图，我们都要处理卡牌范围的高亮
	if is_entered:
		if not hovered_stacks.has(stack): hovered_stacks.append(stack)
	else:
		hovered_stacks.erase(stack)
		
	_update_highlight()
	
	var intent_controller = get_node_or_null("../../ui/TimelineSystem/EnemyIntentManager")
	if intent_controller and intent_controller.has_method("handle_map_stack_hover"):
		intent_controller.handle_map_stack_hover(stack, is_entered)
func _update_highlight():
	hovered_stacks = hovered_stacks.filter(func(s): return is_instance_valid(s))
	
	var cm = get_card_manager()
	var active_card = cm.get("current_selected_card") if cm else null
	
	# 获取主场景的引用，用于操作 UI
	var main_board = get_tree().get_first_node_in_group("MainBoard")
	
	# ★ 需求：未选中卡牌前，绝不触发高亮和Shader
	if not is_instance_valid(active_card):
		_clear_all_aoe_highlights()
		_clear_occlusion_effects()
		active_stack = null
		# 安全隐藏 UI
		if main_board and is_instance_valid(main_board.get("cursor_tooltip")):
			main_board.cursor_tooltip.hide()
		return

	# 寻找最高处的堆叠地块作为中心点
	var front_stack: Area2D = null
	var max_y = -INF
	for stack in hovered_stacks:
		if stack.global_position.y > max_y:
			max_y = stack.global_position.y
			front_stack = stack

	# 如果悬停中心发生了变化，更新整片 AOE 区域
	if active_stack != front_stack:
		active_stack = front_stack
		if is_instance_valid(active_stack):
			_update_aoe_display(active_card, active_stack, main_board)
			# 触发动态遮挡
			_update_occlusion(active_stack)
		else:
			_clear_all_aoe_highlights()
			
func _clear_all_aoe_highlights() -> void:
	# ★ 核心修复 2：极其严格地清理所有被状态机接管的高亮残留
	for stack in current_aoe_stacks:
		change_tile_state(stack, TileVisualState.IDLE)
	current_aoe_stacks.clear()
	
	if is_instance_valid(selected_stack):
		change_tile_state(selected_stack, TileVisualState.IDLE)
		selected_stack = null
		
	if is_instance_valid(active_stack):
		change_tile_state(active_stack, TileVisualState.IDLE)
		active_stack = null
		
## 计算范围并驱动状态机 (所有范围内地块享受同等高亮)
func _update_aoe_display(card: Control, center_stack: Area2D, main_board: Node) -> void:
	var new_aoe_stacks: Array[Area2D] = []
	var center_coord = stack_nodes.find_key(center_stack)
	
	if center_coord != null and card.has_method("get_absolute_effect_range"):
		var range_coords = card.get_absolute_effect_range(center_coord)
		
		# 收集受影响的真实地块
		for coord in range_coords:
			if stack_nodes.has(coord) and is_instance_valid(stack_nodes[coord]):
				new_aoe_stacks.append(stack_nodes[coord])
	
	# 1. 状态卸载：旧的范围内有，但新范围内没有的地块，恢复平静
	for stack in current_aoe_stacks:
		if not new_aoe_stacks.has(stack):
			change_tile_state(stack, TileVisualState.IDLE)
			
	# 2. 状态加载：判断中心点是否合法，决定全境是亮金边还是亮灰边
	var is_valid = _is_stack_valid_target(center_stack)
	var target_state = TileVisualState.HOVER_TARGET_VALID if is_valid else TileVisualState.HOVER_TARGET_INVALID
	
	for stack in new_aoe_stacks:
		# ★ 核心改动：不再区分 center_stack，范围内的地块全部赋予相同的最高级高亮
		change_tile_state(stack, target_state)
			
	# 3. 记录当前列表
	current_aoe_stacks = new_aoe_stacks
	
	# 4. 更新 Tooltip 文本位置
	if main_board and main_board.has_method("update_target_selection_hover"):
		main_board.update_target_selection_hover(center_stack, card)
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
	## 原有方法是为了兼容旧版 custom_card.gd 的调用。
	## 既然现在全面使用了状态机，我们只需要在这里强制清空所有高亮即可，防止残留。
	_clear_all_aoe_highlights()
	_clear_occlusion_effects()
	clear_enemy_intent_preview(false)
	
# ==========================================
# ★ 核心：动态湮灭遮挡物
# ==========================================
func _clear_occlusion_effects() -> void:
	for stack in stack_nodes.values():
		if is_instance_valid(stack):
			_tween_shader_param(stack, "dissolve_blend", 0.0, 0.12)
	currently_occluding_stacks.clear()


## 敌人意图地图预览入口
## 作用:
## - 根据 EnemyIntentData 在地图上同时显示：
##   1. 施法者本体高亮
##   2. 目标格波纹高亮
## - 这里不做 tooltip，tooltip 由 EnemyIntentPresentationController 统一处理。
func show_enemy_intent_preview(intent_data: EnemyIntentData, source_color: Color, target_color: Color) -> void:
	clear_enemy_intent_preview(false)

	if intent_data == null:
		return

	if stack_nodes.has(intent_data.source_coord):
		var source_stack = stack_nodes[intent_data.source_coord]
		if is_instance_valid(source_stack):
			current_enemy_intent_source_stack = source_stack
			_show_source_intent_highlight(source_stack, source_color)

	var seen_targets: Dictionary = {}
	for coord in intent_data.target_coords:
		if seen_targets.has(coord):
			continue
		seen_targets[coord] = true
		if not stack_nodes.has(coord):
			continue
		var target_stack = stack_nodes[coord]
		if not is_instance_valid(target_stack):
			continue
		current_enemy_intent_target_stacks.append(target_stack)
		_show_target_intent_overlay(target_stack, target_color)


## 清除当前地图上的敌人意图预览
## @param restore_card_hover
## - true: 清完之后重新刷新卡牌选中高亮
## - false: 只清理敌人意图，不打断当前其他流程
func clear_enemy_intent_preview(restore_card_hover: bool = true) -> void:
	if is_instance_valid(current_enemy_intent_source_stack):
		_restore_source_intent_highlight(current_enemy_intent_source_stack)

	for stack in current_enemy_intent_target_stacks:
		if is_instance_valid(stack):
			_hide_intent_overlay(stack, "EnemyIntentTargetOverlay")

	current_enemy_intent_source_stack = null
	current_enemy_intent_target_stacks.clear()

	if restore_card_hover:
		_update_highlight()


## 显示“施法者”专用高亮 overlay
## 施法者意图高亮（直接复用地块原本的高亮 shader 逻辑）
## 说明:
## - 按你的要求，这里不再额外挂一张外框贴图。
## - 而是直接修改当前地块 stack 中已有 sprite 的 instance shader 参数，
##   使用和原地块高亮同一套思路：
##   - highlight_color
##   - highlight_blend
##   - is_selected_blend
func _show_source_intent_highlight(stack: Area2D, color: Color) -> void:
	if not is_instance_valid(stack):
		return

	var sprites = stack.get_meta("sprites") as Array
	if sprites == null or sprites.is_empty():
		return

	current_enemy_intent_source_restore_state.clear()
	current_enemy_intent_source_restore_state["stack"] = stack
	current_enemy_intent_source_restore_state["values"] = []

	for sprite in sprites:
		if not is_instance_valid(sprite) or not sprite.material:
			continue
		current_enemy_intent_source_restore_state["values"].append({
			"sprite": sprite,
			"highlight_color": sprite.get_instance_shader_parameter("highlight_color"),
			"highlight_blend": sprite.get_instance_shader_parameter("highlight_blend"),
			"is_selected_blend": sprite.get_instance_shader_parameter("is_selected_blend")
		})
		sprite.set_instance_shader_parameter("highlight_color", color)
		sprite.set_instance_shader_parameter("highlight_blend", 1.0)
		sprite.set_instance_shader_parameter("is_selected_blend", 1.0)


## 显示“目标地块”专用波纹 overlay
func _show_target_intent_overlay(stack: Area2D, color: Color) -> void:
	var overlay = _ensure_intent_overlay(stack, "EnemyIntentTargetOverlay", enemy_intent_target_shader)
	if overlay == null:
		return
	_update_intent_overlay_transform(stack, overlay)
	overlay.visible = true
	if overlay.material:
		overlay.material.set_shader_parameter("ripple_color", color)


## 隐藏指定类型的敌人意图 overlay
func _hide_intent_overlay(stack: Area2D, overlay_name: String) -> void:
	if not is_instance_valid(stack):
		return
	var overlay = stack.get_node_or_null(overlay_name)
	if overlay and overlay is Sprite2D:
		overlay.visible = false


func _restore_source_intent_highlight(stack: Area2D) -> void:
	if not is_instance_valid(stack):
		return
	if current_enemy_intent_source_restore_state.is_empty():
		return
	if current_enemy_intent_source_restore_state.get("stack") != stack:
		return

	var restore_values = current_enemy_intent_source_restore_state.get("values", [])
	for item in restore_values:
		var sprite = item["sprite"]
		if not is_instance_valid(sprite) or not sprite.material:
			continue
		sprite.set_instance_shader_parameter("highlight_color", item["highlight_color"])
		sprite.set_instance_shader_parameter("highlight_blend", item["highlight_blend"])
		sprite.set_instance_shader_parameter("is_selected_blend", item["is_selected_blend"])

	current_enemy_intent_source_restore_state.clear()


## 确保某个地块拥有指定名称的敌人意图 overlay。
## 如果不存在则动态创建，并挂载独立 shader material。
func _ensure_intent_overlay(stack: Area2D, overlay_name: String, shader: Shader) -> Sprite2D:
	if not is_instance_valid(stack):
		return null

	var overlay = stack.get_node_or_null(overlay_name) as Sprite2D
	if overlay == null:
		overlay = Sprite2D.new()
		overlay.name = overlay_name
		overlay.texture = enemy_intent_frame_tex
		overlay.centered = true
		overlay.z_index = enemy_intent_overlay_z_index
		overlay.visible = false

		var material = ShaderMaterial.new()
		material.shader = shader
		overlay.material = material
		stack.add_child(overlay)

	_update_intent_overlay_transform(stack, overlay)
	_configure_intent_overlay_material(overlay_name, overlay.material)
	return overlay


## 根据当前地块 hitbox 和视角状态，更新 overlay 的位置和缩放。
## 说明:
## - overlay 总是贴着当前地块顶部碰撞区域，而不是直接贴着最底层 sprite。
func _update_intent_overlay_transform(stack: Area2D, overlay: Sprite2D) -> void:
	var collision = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
	if is_instance_valid(collision):
		overlay.position = collision.position
	else:
		overlay.position = Vector2(hitbox_offset_x, hitbox_offset_y)

	var tex_size = enemy_intent_frame_tex.get_size() if enemy_intent_frame_tex != null else Vector2.ONE
	if tex_size.x > 0 and tex_size.y > 0:
		overlay.scale = Vector2(
			(hitbox_width / tex_size.x) * enemy_intent_overlay_scale,
			(hitbox_base_height / tex_size.y) * enemy_intent_overlay_scale
		)


## 将导出的敌人意图地图表现参数写入对应的 shader material。
## 说明:
## - SourceOverlay 和 TargetOverlay 使用不同 shader，因此参数不同。
func _configure_intent_overlay_material(overlay_name: String, material: Material) -> void:
	if not (material is ShaderMaterial):
		return

	var shader_material = material as ShaderMaterial
	if overlay_name == "EnemyIntentSourceOverlay":
		shader_material.set_shader_parameter("highlight_blend", 1.0)
		shader_material.set_shader_parameter("is_selected_blend", 1.0)
		shader_material.set_shader_parameter("highlight_width", enemy_intent_source_highlight_width)
	elif overlay_name == "EnemyIntentTargetOverlay":
		shader_material.set_shader_parameter("ripple_speed", enemy_intent_target_ripple_speed)
		shader_material.set_shader_parameter("ripple_density", enemy_intent_target_ripple_density)
		shader_material.set_shader_parameter("min_alpha", enemy_intent_target_min_alpha)
		shader_material.set_shader_parameter("max_alpha", enemy_intent_target_max_alpha)


func _update_occlusion(target_stack: Area2D):
	# ★ 核心修复 1：在平铺视角下，彻底禁用防遮挡机制！
	if current_view_state == MapViewState.VIEW_FLAT: 
		return

	# 1. 恢复之前被湮灭的柱子 (倒放)
	_clear_occlusion_effects()

	if not is_instance_valid(target_stack): return

	var t_pos = target_stack.position
	var t_h = target_stack.get_meta("height")
	var current_step_h = step_height * (tile_scale / REF_SCALE)
	var t_top_y = t_pos.y - (t_h - 1) * current_step_h

	# 2. 遍历判断谁挡在前面
	for stack in stack_nodes.values():
		if stack == target_stack: continue
		var s_pos = stack.position

		if s_pos.y > t_pos.y:
			if abs(s_pos.x - t_pos.x) < hitbox_width * 0.8:
				var s_h = stack.get_meta("height")
				var s_top_y = s_pos.y - (s_h - 1) * current_step_h

				if s_top_y < t_pos.y:
					currently_occluding_stacks.append(stack)
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
	
	if landform_inst.Attitude == landform_inst.Attitude_Pool.Enemy:
		enemy_roster_changed.emit()
	
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
## 切换视角状态机
func toggle_height_view() -> void:
	if is_view_transitioning: return
	is_view_transitioning = true
	clear_enemy_intent_preview(false)
	
	if current_view_state == MapViewState.VIEW_3D:
		current_view_state = MapViewState.VIEW_FLAT
		_compress_to_single_height_view()
	else:
		current_view_state = MapViewState.VIEW_3D
		_restore_original_height_view()
		
	await get_tree().create_timer(0.4).timeout
	is_view_transitioning = false
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

func _tween_position_y(node, target_y: float, duration: float) -> void:
	if not is_instance_valid(node): return
	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tw.tween_property(node, "position:y", target_y, duration)
	
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
	if current_view_state == MapViewState.VIEW_FLAT:
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
		# 平铺视角数字仅用于显示，绝不能拦截地块的鼠标输入。
		label.mouse_filter = Control.MOUSE_FILTER_IGNORE
		label.focus_mode = Control.FOCUS_NONE
		
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

## 按钮按下回调
func _on_height_view_toggle_pressed() -> void:
	toggle_height_view()

## ==========================================
## ★ 高度视图鼠标交互系统
## ==========================================

## 开始光柱浮动动画（鼠标悬停时调用）
func _start_pillar_floating_animation(stack: Area2D) -> void:
	if not current_view_state == MapViewState.VIEW_FLAT or not is_instance_valid(stack):
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
	if not current_view_state == MapViewState.VIEW_FLAT or not is_instance_valid(stack):
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
	
	if locked:
		# 进入时间占位放置阶段时，保留高亮，但必须释放防遮挡消融，避免地块“消失”。
		_clear_occlusion_effects()
		clear_enemy_intent_preview(false)
		var main_board = get_tree().get_first_node_in_group("MainBoard")
		if main_board and is_instance_valid(main_board.get("cursor_tooltip")):
			main_board.cursor_tooltip.hide()
	else:
		# 解锁时同样主动释放消融，并强制刷新一次，防止鼠标不动导致 Shader 幽灵卡死。
		_clear_occlusion_effects()
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

## 增强版：处理地块升降（融合了安全偏移逻辑）
func animate_elevation_change(stack: Area2D, delta_height: int) -> void:
	if delta_height == 0 or not is_instance_valid(stack): return
	
	while is_instance_valid(stack) and stack.has_meta("is_animating") and stack.get_meta("is_animating"):
		await get_tree().process_frame 
		
	if not is_instance_valid(stack): return
	stack.set_meta("is_animating", true)

	var old_height = stack.get_meta("height") as int
	var new_height = old_height + delta_height
	var coord = stack_nodes.find_key(stack)
	if coord == null: 
		stack.set_meta("is_animating", false)
		return
		
	if new_height > max_height or new_height <= min_height:
		await _perform_tile_destruction(stack, coord)
		return
		
	new_height = clampi(new_height, 1, 99)
	var actual_delta = new_height - old_height
	if actual_delta == 0: 
		stack.set_meta("is_animating", false)
		return

	# --- 统一数据更新 ---
	stack.set_meta("height", new_height)
	map_data[coord]["height"] = new_height
	if GlobalClock and "tile_h_pool" in GlobalClock:
		if GlobalClock.tile_h_pool.has(old_height): GlobalClock.tile_h_pool[old_height].erase(coord)
		if not GlobalClock.tile_h_pool.has(new_height): GlobalClock.tile_h_pool[new_height] = []
		GlobalClock.tile_h_pool[new_height].append(coord)

	var sprites = stack.get_meta("sprites") as Array
	var terrain_type = map_data[coord]["terrain_type"]
	var side_tex = get_side_tex(terrain_type)
	var current_step_h = filler_block_spacing * (tile_scale / REF_SCALE)

	# ==========================================
	# ★ 平铺视图分支：拦截 3D 动画，采用纯数字反馈，并精准更新后台 3D 缓存！
	# ==========================================
	if current_view_state == MapViewState.VIEW_FLAT:
		var cached_data = height_view_original_materials.get(stack)
		
		# ★ 核心修复：更新现有组件的3D坐标缓存，完美保留UI和地貌的局部相对偏移
		var delta_y = -(actual_delta * elevation_move_distance)
		if cached_data:
			for s_data in cached_data["sprites_data"]:
				s_data["original_position"].y += delta_y
			if cached_data["occupant_data"]: cached_data["occupant_data"]["original_position"].y += delta_y
			if cached_data["collision_data"]: cached_data["collision_data"]["original_position"].y += delta_y
			if cached_data["health_bar_data"]: cached_data["health_bar_data"]["original_position"].y += delta_y

		if actual_delta > 0:
			var orig_bottom_y = 0.0
			if cached_data and not cached_data["sprites_data"].is_empty():
				# 逆向推导出移动前的最底层 3D 坐标
				orig_bottom_y = cached_data["sprites_data"][0]["original_position"].y - delta_y

			for k in range(actual_delta):
				var new_sprite = Sprite2D.new()
				new_sprite.texture = side_tex
				new_sprite.centered = false
				new_sprite.offset = Vector2(-256, -400)
				
				new_sprite.visible = false 
				new_sprite.modulate.a = 0.0 
				new_sprite.position.y = 0.0 
				new_sprite.scale = Vector2(tile_scale, tile_scale)
				
				if block_material:
					new_sprite.material = block_material.duplicate()
					new_sprite.material.set_shader_parameter("is_flat_view", 1.0)
					
				stack.add_child(new_sprite)
				stack.move_child(new_sprite, k) 
				sprites.insert(k, new_sprite)
				
				# ★ 暗中存入字典：为切回 3D 预留数据
				if cached_data:
					var new_3d_y = orig_bottom_y - (k * current_step_h)
					cached_data["sprites_data"].insert(k, {"sprite": new_sprite, "original_material": new_sprite.material, "original_position": Vector2(new_sprite.position.x, new_3d_y)})
					
		elif actual_delta < 0:
			for i in range(abs(actual_delta)):
				if sprites.size() > 1: 
					var bottom_sprite = sprites[0]
					sprites.pop_front()
					bottom_sprite.queue_free()
					if cached_data: cached_data["sprites_data"].pop_front()

		for i in range(sprites.size()):
			if is_instance_valid(sprites[i]) and sprites[i].material:
				sprites[i].set_instance_shader_parameter("block_idx", float(i))
				sprites[i].set_instance_shader_parameter("total_height", float(new_height))

		# 纯二维数字弹跳动画
		var label = stack.get_meta("height_indicator_label") if stack.has_meta("height_indicator_label") else null
		if is_instance_valid(label):
			label.text = str(new_height)
			var tw_label = create_tween().set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
			tw_label.tween_property(label, "scale", Vector2(1.8, 1.8), 0.2)
			tw_label.parallel().tween_property(label, "modulate", Color(1.0, 0.2, 0.2), 0.2) 
			tw_label.tween_property(label, "scale", Vector2.ONE, 0.3)
			tw_label.parallel().tween_property(label, "modulate", Color.WHITE, 0.3)
			await tw_label.finished

		stack.set_meta("is_animating", false)
		return
	# ==========================================
	# ★ 3D 视图分支：安全相对移动
	# ==========================================
	var moving_parts = []
	moving_parts.append_array(sprites)
	var collision = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
	if is_instance_valid(collision): moving_parts.append(collision)
	
	var occupant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
	if is_instance_valid(occupant): 
		moving_parts.append(occupant)
		var bar_manager = get_node_or_null("BarManager")
		if bar_manager:
			var hb_node = bar_manager.get_node_or_null("HealthBar_" + str(occupant.get_instance_id()))
			# ★ 防二次移动：血条父级不是 stack_nodes 时才能独立移动
			if is_instance_valid(hb_node) and hb_node.get_parent() not in moving_parts: 
				moving_parts.append(hb_node)

	var y_offset_movement = -(actual_delta * current_step_h)
	var original_positions = {}
	for part in moving_parts:
		if is_instance_valid(part): original_positions[part.get_instance_id()] = part.position

	var tw = create_tween().set_parallel(true).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	for part in moving_parts:
		if is_instance_valid(part) and part.get_parent() not in moving_parts:
			var p_id = part.get_instance_id()
			var target_pos = original_positions[p_id] + Vector2(0, y_offset_movement)
			tw.tween_property(part, "position:x", original_positions[p_id].x + randf_range(-5,5), 0.1)
			tw.chain().tween_property(part, "position", target_pos, ele_anim_duration).set_trans(ele_trans_type).set_ease(ele_ease_type)

	tw.chain().tween_callback(func():
		if actual_delta > 0:
			for k in range(actual_delta):
				var new_sprite = Sprite2D.new()
				new_sprite.texture = side_tex
				new_sprite.centered = false
				new_sprite.offset = Vector2(-256, -400)
				# 新泥块总是插在底部，坐标依序为 0, -48, -96
				new_sprite.position.y = -(k) * current_step_h
				new_sprite.scale = Vector2(tile_scale, tile_scale)
				new_sprite.modulate = Color(1.0, 1.0, 1.0)
				if block_material: new_sprite.material = block_material.duplicate()
				stack.add_child(new_sprite)
				stack.move_child(new_sprite, k) 
				sprites.insert(k, new_sprite)
		elif actual_delta < 0:
			for i in range(abs(actual_delta)):
				if sprites.size() > 1: 
					var bottom_sprite = sprites[0]
					sprites.pop_front()
					bottom_sprite.queue_free()

		for i in range(sprites.size()):
			if is_instance_valid(sprites[i]) and sprites[i].material:
				sprites[i].set_instance_shader_parameter("block_idx", float(i))
				sprites[i].set_instance_shader_parameter("total_height", float(new_height))
	)
	
	await tw.finished
	if is_instance_valid(stack): stack.set_meta("is_animating", false)
	tile_topology_changed.emit()
	
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

## 执行毁灭流程
func _perform_tile_destruction(stack: Area2D, coord: Vector2i) -> void:
	var sprites = stack.get_meta("sprites") as Array
	GameLogger.info("交由 VFXManager 播放地块毁灭动画: " + str(coord), "HexMap")
	
	# ★ 核心解耦：等待 VFXManager 的表现播完
	await VFXManager.play_tile_destruction_vfx(sprites, get_tree())
	
	# 表现播完后，执行逻辑抹除
	if is_instance_valid(stack):
		stack.queue_free()
	
	stack_nodes.erase(coord)
	map_data.erase(coord)
	
	var occupant = stack.get_meta("occupant")
	if is_instance_valid(occupant):
		occupant.queue_free()
	
	enemy_roster_changed.emit()
	tile_topology_changed.emit()
	GameLogger.info("地块已从地图彻底抹除: " + str(coord), "HexMap")

# ==========================================
# ★ 状态机 Shader 驱动引擎
# ==========================================
func change_tile_state(stack: Area2D, new_state: TileVisualState) -> void:
	if not is_instance_valid(stack): return
	
	var current_state = stack.get_meta("visual_state") if stack.has_meta("visual_state") else TileVisualState.IDLE
	if current_state == new_state: return
	
	stack.set_meta("visual_state", new_state)
	
	# 定义各种状态的 Shader 参数目标值
	var target_highlight_blend = 0.0
	var target_selected_blend = 0.0
	var highlight_color = Color(1, 1, 1, 1)
	
	match new_state:
		TileVisualState.IDLE:
			target_highlight_blend = 0.0
			target_selected_blend = 0.0
			
		TileVisualState.HOVER_TARGET_VALID:
			target_highlight_blend = 1.0
			target_selected_blend = 1.0  # 开启白描边
			highlight_color = Color(1.0, 0.8, 0.0, 1.0) # 金色核心
			
		TileVisualState.HOVER_TARGET_INVALID:
			target_highlight_blend = 1.0
			target_selected_blend = 1.0
			highlight_color = Color(0.5, 0.5, 0.5, 1.0) # 灰色无效
			
		TileVisualState.AOE_RANGE:
			target_highlight_blend = 0.7  # 范围边缘不升起那么高，或者用0.7表示波及
			target_selected_blend = 0.0   # 边缘不加描边
			highlight_color = Color(0.6, 0.8, 1.0, 1.0) # 淡蓝色波及范围

	# 将状态压入 Tween
	var meta_key = "tween_state_machine"
	if stack.has_meta(meta_key):
		var old_tw = stack.get_meta(meta_key)
		if is_instance_valid(old_tw) and old_tw.is_valid(): old_tw.kill()
		
	var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	stack.set_meta(meta_key, tw)
	
	var sprites = stack.get_meta("sprites") as Array
	for s in sprites:
		if is_instance_valid(s) and s.material:
			# 颜色可以直接赋值，无需渐变（节省性能且更干脆）
			s.set_instance_shader_parameter("highlight_color", highlight_color)
			
			# 只有 blend 值使用 Tween 过渡，确保丝滑
			var cur_h_blend = s.get_instance_shader_parameter("highlight_blend")
			var cur_s_blend = s.get_instance_shader_parameter("is_selected_blend")
			if cur_h_blend == null: cur_h_blend = 0.0
			if cur_s_blend == null: cur_s_blend = 0.0
			
			tw.tween_method(func(val: float): if is_instance_valid(s): s.set_instance_shader_parameter("highlight_blend", val), cur_h_blend, target_highlight_blend, 0.15)
			tw.tween_method(func(val: float): if is_instance_valid(s): s.set_instance_shader_parameter("is_selected_blend", val), cur_s_blend, target_selected_blend, 0.15)

## 压缩为平铺视图 (利用缓存精准归位)
func _compress_to_single_height_view() -> void:
	GameLogger.info("状态机切换 -> 平铺视图", "HexMap")
	var current_step_h = filler_block_spacing * (tile_scale / REF_SCALE)
	height_view_original_materials.clear()
	
	for coord in stack_nodes.keys():
		var stack = stack_nodes[coord]
		if not is_instance_valid(stack): continue
		
		var sprites = stack.get_meta("sprites") as Array
		var height = stack.get_meta("height") as int
		
		# 1. 精确记录所有原始位置，用于恢复
		var stack_sprites_data = []
		for s in sprites:
			if is_instance_valid(s):
				stack_sprites_data.append({"sprite": s, "original_position": s.position})
				if s.material: s.set_instance_shader_parameter("is_flat_view", 1.0)

		var occupant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
		var occ_data = {"node": occupant, "original_position": occupant.position} if is_instance_valid(occupant) else null
		
		var collision = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
		var col_data = {"node": collision, "original_position": collision.position} if is_instance_valid(collision) else null
		
		var hb_data = null
		var bar_manager = get_node_or_null("BarManager")
		if bar_manager and is_instance_valid(occupant):
			var hb = bar_manager.get_node_or_null("HealthBar_" + str(occupant.get_instance_id()))
			if is_instance_valid(hb): hb_data = {"node": hb, "original_position": hb.position}

		# 写入缓存字典
		height_view_original_materials[stack] = {
			"sprites_data": stack_sprites_data,
			"occupant_data": occ_data,
			"collision_data": col_data,
			"health_bar_data": hb_data
		}

		# 2. 执行下落压平动画 (计算需要下落的高度差)
		var drop_delta = (height - 1) * current_step_h
		
		for i in range(sprites.size()):
			var s = sprites[i]
			if not is_instance_valid(s): continue
			if i < height - 1:
				# 侧面土块：渐隐，随后关闭渲染
				var tw = create_tween()
				tw.tween_property(s, "modulate:a", 0.0, 0.3)
				tw.tween_callback(func(): s.visible = false)
			else:
				# 顶部方块：落到地表
				if s.get_parent() == stack:
					_tween_position_y(s, s.position.y + drop_delta, 0.3)

		if is_instance_valid(occupant): _tween_position_y(occupant, occupant.position.y + drop_delta, 0.3)
		if is_instance_valid(collision): _tween_position_y(collision, collision.position.y + drop_delta, 0.3)
		if hb_data and is_instance_valid(hb_data["node"]): _tween_position_y(hb_data["node"], hb_data["node"].position.y + drop_delta, 0.3)

		_create_height_indicator(stack, height)

## 恢复 3D 视图 (直接从字典中精准读取坐标)
func _restore_original_height_view() -> void:
	GameLogger.info("恢复 3D 视图", "HexMap")
	
	for coord in stack_nodes.keys():
		var stack = stack_nodes[coord]
		if not is_instance_valid(stack): continue
		
		if height_view_original_materials.has(stack):
			var cached = height_view_original_materials[stack]
			
			# 1. 恢复地形方块
			for s_data in cached["sprites_data"]:
				var s = s_data["sprite"]
				if is_instance_valid(s):
					if s.material: s.set_instance_shader_parameter("is_flat_view", 0.0)
					if s.get_parent() == stack:
						_tween_position_y(s, s_data["original_position"].y, 0.3)
			
			# 2. 恢复外挂组件
			if cached["occupant_data"] and is_instance_valid(cached["occupant_data"]["node"]):
				_tween_position_y(cached["occupant_data"]["node"], cached["occupant_data"]["original_position"].y, 0.3)
			if cached["collision_data"] and is_instance_valid(cached["collision_data"]["node"]):
				_tween_position_y(cached["collision_data"]["node"], cached["collision_data"]["original_position"].y, 0.3)
			if cached["health_bar_data"] and is_instance_valid(cached["health_bar_data"]["node"]):
				_tween_position_y(cached["health_bar_data"]["node"], cached["health_bar_data"]["original_position"].y, 0.3)

		# 3. 侧边方块恢复可见
		var sprites = stack.get_meta("sprites") as Array
		var height = stack.get_meta("height") as int
		for i in range(sprites.size()):
			var s = sprites[i]
			if is_instance_valid(s) and i < height - 1:
				s.visible = true
				var tw = create_tween()
				tw.tween_property(s, "modulate:a", 1.0, 0.3)

		_remove_height_indicator(stack)
		
	height_view_original_materials.clear()
