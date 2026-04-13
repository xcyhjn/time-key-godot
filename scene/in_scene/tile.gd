@abstract class_name landform
extends  Node2D

signal Blood_change(Blood)


enum property_pool{
	delete
}
var property : int

var owner_battle: battle
enum Vice_State_Pool {
	Flag1 = 1,
	Flag2 = 1 << 1,
	Flag3 = 1 << 2
}

enum Main_State_Pool {
	Normal, Captured, Broken
}

var Capture_rate : float = 0.1
var State_Main : int
var State_Vice : int

@export_group("地貌基本信息")
@export var landform_name : String

@export_group("地貌生成规则")
@export var landform_rules  : Dictionary

@export_group("地貌贴图接口")
@export var landform_tex : Array[Texture2D]              # 索引 = LandformType
@export var landform_damaged_tex : Array[Texture2D]      # 索引 = LandformType（损坏态）

var damage_rate : float = 0
var location : Vector2

var HP : float
var BloodBar
var Max_Blood : float
var Underlings : bool

var tex : Sprite2D

var target : Vector2
var willing : int
var will : bool

var willing_pool : Dictionary

const HEX_DIRS := [
	Vector2(1, 0),
	Vector2(1, -1),
	Vector2(0, -1),
	Vector2(-1, 0),
	Vector2(-1, 1),
	Vector2(0, 1),
]

enum Attitude_Pool {
	Middle, Enemy
}
var Attitude = Attitude_Pool.Middle

var possible_behaviour : Array[Callable]

var neighbors : Array[Vector2]
var step : int


@export_group("时间占位系统")
@export var timeline_shape_key: String = "1x1"  # 形状键名，如 "1x1", "1x2", "2x2"
@export var timeline_shape_size: Vector2 = Vector2(1, 1)  # 形状尺寸
var timeline_shape_coords: Array[Vector2] = []  # 形状坐标数组
var _last_parsed_shape_key: String = ""  # 上次解析的形状键，用于幂等性检查

func _init(name_in : String, tex_in : Array[String], damaged_tex_in : Array[String], rules_in : Dictionary, location_in : Vector2, is_Underlings : bool,battle_in) -> void:
	if tex_in.is_empty() or damaged_tex_in.is_empty():
		push_error("无效的图像接口")
		return
		
	for tex in tex_in:
		landform_tex.append(ResourceLoader.load(tex, "Texture2D"))
	for damaged_tex in damaged_tex_in:
		landform_damaged_tex.append(ResourceLoader.load(damaged_tex, "Texture2D"))
	self.landform_name = name_in
	self.location = location_in
	self.landform_rules = rules_in
	self.Underlings = is_Underlings
	self.neighbors = get_neighbor_coords(location_in)
	self.owner_battle = battle_in
	self.HP = Max_Blood


func set_rate(rate : float):
	damage_rate = rate

func random_damage() -> void:
	take_damage(randi_range(0, Max_Blood * 0.3))
	

func get_neighbor_coords(center: Vector2) -> Array[Vector2]:
	var result: Array[Vector2] = []
	for dir in HEX_DIRS:
		var next = center + dir
		if abs(next[1] + next[0]) <= 4 and abs(next[1]) <= 4 and abs(next[0]) <= 4:
			result.append(next)
	return result

func get_possible_coords(h: int, terrain: int, tile_info : Dictionary) -> bool:
	if landform_rules.keys().has("require_height") and !landform_rules["require_height"].has(h) :
		return false
	if landform_rules.keys().has("require_terrain") and !landform_rules["require_terrain"].has(terrain):
		return false
	if !tile_info.has("landform")  or tile_info["landform"] != null:
		return false
	return true

func _add_landform_sprite(parent: Node2D, coord: Vector2, height: int, current_step_h: float, tile_scale : float) -> void:
	var tex: Texture2D = null
	if State_Main == Main_State_Pool.Broken:
		if landform_damaged_tex != null and not landform_damaged_tex.is_empty():
			tex = landform_damaged_tex[0]

	if tex == null:
		if landform_tex != null and not landform_tex.is_empty():
			tex = landform_tex[0]

	if tex == null:
		return

	var lf_sprite = Sprite2D.new()
	# 使用唯一名称避免命名冲突
	lf_sprite.name = "LandformSprite_%s_%s" % [coord.x, coord.y]
	lf_sprite.texture = tex
	lf_sprite.centered = false
	lf_sprite.scale = Vector2(tile_scale, tile_scale)
	# 移除硬编码的z_index，让父容器控制渲染层级
	# lf_sprite.z_index = 999
	
	var top_y = -((height - 1) * current_step_h)
	var tex_size = tex.get_size()

	# 额外上抬
	var extra_lift = 70.0 * tile_scale

	lf_sprite.position = Vector2(
		-tex_size.x * 0.5 * tile_scale,
		top_y - tex_size.y * tile_scale - extra_lift
	)
	
	var world_position = parent.global_position + lf_sprite.position
	
	# 动态计算血条位置（适应项目大小变化）
	var viewport_size = get_viewport().get_visible_rect().size
	
	owner_battle.CreateBar.emit(self, Attitude, world_position.x, world_position.y)
	print(str(position) + ": 唤起血条中")
	random_damage()


	parent.add_child(lf_sprite)
 
func Behavior(Step, info_in, Other, beha):
	# 默认行为：什么也不做
	# 子类可以重写此方法以实现特定行为
	pass

func set_battle(battle_in : battle):
	owner_battle = battle_in
	return self

func attach_visual(parent: Node2D, height: int, current_step_h: float, tile_scale: float) -> void:
	if parent == null:
		return

	# 防止重复添加 - 使用新的命名模式
	var unique_name = "LandformSprite_%s_%s" % [location.x, location.y]
	var old_node = parent.get_node_or_null(unique_name)
	if old_node != null:
		old_node.queue_free()

	_add_landform_sprite(parent, location, height, current_step_h, tile_scale)


## 解析矩阵形状
## 格式: 逗号分隔 "010,111,010" 或 换行分隔 "010\n111\n010"
## 占用字符: 1
## 空字符: 0 或其他
func _parse_matrix_shape(matrix_str: String) -> bool:
	var rows: Array[String] = []
	var trimmed = matrix_str.strip_edges()
	
	if trimmed.contains(","):
		# 逗号分隔格式
		for row in trimmed.split(","):
			var clean = row.strip_edges()
			if not clean.is_empty():
				rows.append(clean)
	else:
		# 换行分隔格式
		for line in trimmed.split("\n"):
			var clean = line.strip_edges()
			if not clean.is_empty():
				rows.append(clean)
	
	if rows.is_empty():
		return false
	
	# 检查行长度一致
	var width = rows[0].length()
	for i in range(1, rows.size()):
		if rows[i].length() != width:
			return false
	
	# 解析坐标
	var parsed_shape: Array[Vector2] = []
	for y in range(rows.size()):
		var row = rows[y]
		for x in range(row.length()):
			if row[x] == "1":
				parsed_shape.append(Vector2(x, y))
	
	if parsed_shape.is_empty():
		return false
	
	# 计算包围盒（Bounding Box）
	# 包围盒是包含形状所有占用单元格的最小矩形区域
	# 作用：
	# 1. 确定形状的实际尺寸（宽度、高度）
	# 2. 检查形状是否适合时间轴网格（12x3）
	# 3. 为形状在时间轴上的放置提供定位参考
	# 4. 确保视觉显示时形状正确对齐
	# 
	# 计算方法：遍历所有占用坐标，找到最小/最大的X和Y值
	# 初始值设为极大值（最小坐标）和极小值（最大坐标）
	var min_x = 1000  # 最小X坐标（最左侧）
	var min_y = 1000  # 最小Y坐标（最上方）
	var max_x = -1000 # 最大X坐标（最右侧）
	var max_y = -1000 # 最大Y坐标（最下方）
	
	for coord in parsed_shape:
		min_x = min(min_x, coord.x)  # 更新最小X
		max_x = max(max_x, coord.x)  # 更新最大X
		min_y = min(min_y, coord.y)  # 更新最小Y
		max_y = max(max_y, coord.y)  # 更新最大Y
	
	# 包围盒尺寸计算：宽度 = 最大X - 最小X + 1，高度 = 最大Y - 最小Y + 1
	# 注意：坐标从0开始，所以需要+1来得到实际的单元格数量
	timeline_shape_size = Vector2(max_x - min_x + 1, max_y - min_y + 1)
	timeline_shape_coords = parsed_shape
	
	# 尺寸警告
	if timeline_shape_size.y > 3:
		GameLogger.warning("矩阵形状高度 %d 超出网格最大高度3" % timeline_shape_size.y, "Tile")
	
	if timeline_shape_size.x > 12:
		GameLogger.warning("矩阵形状宽度 %d 超出网格最大宽度12" % timeline_shape_size.x, "Tile")
	
	return true

## 解析时间占位形状（统一矩阵格式）
func parse_timeline_shape() -> void:
	# 幂等性检查：如果已经解析过当前形状键，则跳过
	if _last_parsed_shape_key == timeline_shape_key and not timeline_shape_coords.is_empty():
		return
	
	if timeline_shape_key.is_empty():
		timeline_shape_key = "1"  # 默认1x1矩阵
	
	var trimmed_key = timeline_shape_key.strip_edges()
	
	# 尝试解析矩阵
	if _parse_matrix_shape(trimmed_key):
		_last_parsed_shape_key = timeline_shape_key
		return
	
	# 矩阵解析失败：使用默认1x1
	GameLogger.warning("无效的矩阵格式: %s，使用默认1x1" % timeline_shape_key, "Tile")
	timeline_shape_key = "1"
	_parse_matrix_shape("1")
	_last_parsed_shape_key = timeline_shape_key



## 获取时间占位形状坐标
func get_timeline_shape_coords() -> Array[Vector2]:
	parse_timeline_shape()  # 幂等性调用，确保形状已解析
	return timeline_shape_coords


## 获取时间占位形状尺寸
func get_timeline_shape_size() -> Vector2:
	parse_timeline_shape()  # 幂等性调用，确保形状已解析
	return timeline_shape_size


## 重写：受伤逻辑（更新 damage_rate 和 damage 状态）
func take_damage(amount: int) -> void:
	# 这里可以播放通用的受伤动画，比如 $AnimationPlayer.play("hurt")
	HP = 0 if HP - amount < 0 else HP - amount
	State_Update()
	Blood_change.emit(HP)
	
	# 更新 damage_rate 基于当前生命值
	if Max_Blood > 0:
		damage_rate = 1.0 - (float(HP) / float(Max_Blood))
		GameLogger.debug("地形受伤: amount=%d, current_hp=%d, damage_rate=%.2f" % [amount, HP, damage_rate], "landform")
	


func State_Update():
	if HP <= 0:
		die()
	elif HP < Max_Blood * Capture_rate and HP > 0:
		Captured()
	else: 
		Revived()

## 重写：死亡逻辑（标记为损坏状态）
func die() -> void:
	GameLogger.info("【地形实体死亡】%s 被摧毁" % landform_name, "landform")
	State_Main = Main_State_Pool.Broken
	tex_toggle()
	
	damage_rate = 1.0
	
	# 如果有 owner_battle，通知更新视觉
	if owner_battle and owner_battle.has_method("add_landform_visual_at"):
		owner_battle.add_landform_visual_at(location)
	
	# 调用父类死亡逻辑
	free()


# ==========================================
# ★ 敌人意图接口
# ==========================================

## 获取敌人的时间轴形状（实现 enemy_intent_manager 接口）
func get_intent_shape() -> Array[Vector2i]:
	parse_timeline_shape()  # 幂等性调用，确保形状已解析
	GameLogger.debug("获取地形意图形状: %s, 坐标: %s" % [timeline_shape_key, timeline_shape_coords], "landform")
	return timeline_shape_coords.duplicate()


## 获取敌人的意图行动（实现 enemy_intent_manager 接口）
func get_intent_action(target_tile: Node = null) -> TimelineAction:
	# 创建时间轴行动 - 使用正确的构造函数参数
	var action_data = {
		"效果": "地形实体行动",
		"类型": landform_name,
		"位置": location,
		"目标": target_tile.position if target_tile else Vector2.ZERO
	}
	
	var action = TimelineAction.new(
		TimelineAction.Type.ENEMY,  # p_type
		self,                       # p_source
		target_tile,                # p_target
		get_intent_shape(),         # p_coords
		Color(0.8, 0.2, 0.2, 0.8), # p_color
		action_data                 # p_data
	)
	
	GameLogger.debug("创建地形意图行动: %s" % landform_name, "landform")
	return action


## 设置时间占位形状
func set_timeline_shape(shape_key: String) -> void:
	timeline_shape_key = shape_key
	parse_timeline_shape()
	GameLogger.debug("设置地形时间占位形状: %s -> %s" % [shape_key, timeline_shape_coords], "landform")

func Revived():
	State_Main = Main_State_Pool.Normal
	tex_toggle()

func Captured():
	State_Main = Main_State_Pool.Captured
	tex_toggle()

func tex_toggle():
	if tex == null:
		print("纹理模块为空")
		return
		
	if State_Main == Main_State_Pool.Normal or State_Main == Main_State_Pool.Captured:
		if !landform_tex.has(tex):
			tex.texture = landform_tex[0]
	elif State_Main == Main_State_Pool.Broken:
		if !landform_damaged_tex.has(tex):
			tex.texture = landform_tex[0]
