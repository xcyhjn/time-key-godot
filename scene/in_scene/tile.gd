@abstract class_name landform
extends  Node2D

signal Blood_change(Blood)


enum property_pool{
	delete
}
var property : int

var owner_battle: battle
enum Vice_State_Pool {
	protected = 1,
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
var location : Vector2i

var HP : float
var BloodBar
var Max_Blood : float
var Underlings : bool

var tex : Sprite2D

var target : Vector2
var willing : int
var will : bool

var willing_pool : Dictionary

const HEX_DIRS : Array[Vector2i] = [
	Vector2i(1, 0),
	Vector2i(1, -1),
	Vector2i(0, -1),
	Vector2i(-1, 0),
	Vector2i(-1, 1),
	Vector2i(0, 1),
]

enum Attitude_Pool {
	Middle, Enemy
}
var Attitude = Attitude_Pool.Middle


var neighbors : Array[Vector2i]
var step : int

var sheild : int = 0

@export_group("时间占位系统")
@export var timeline_shape_key: String = "1"         # 原始输入的字符串（如 "011"）
@export var timeline_shape_size: Vector2i = Vector2i(1, 1)  # ★ 必须是 Vector2i
var timeline_shape_coords: Array[Vector2i] = []             # ★ 必须是 Vector2i
var _last_parsed_shape_key: String = ""

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
	self.owner_battle = battle_in
	self.neighbors = get_neighbor_coords(location_in)
	self.HP = Max_Blood


func set_rate(rate : float):
	damage_rate = rate

func random_damage() -> void:
	take_damage(randi_range(0, Max_Blood * 0.3))
	

func get_neighbor_coords(center: Vector2i) -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for dir in HEX_DIRS:
		var next = center + dir
		if owner_battle.map_data.keys().has(next):
			result.append(next)
	return result

func get_possible_coords(coord : Vector2i, tile_info : Dictionary) -> bool:
	var c = Vector2i(coord) # 安全转换
	if not tile_info.has(c): return false
	
	var data = tile_info[c]
	var h = data.get("height", 0)
	var terrain = data.get("terrain_type", 0)
	
	if landform_rules.keys().has("require_height") and !landform_rules["require_height"].has(tile_info[coord]["height"]) :
		return false
	if landform_rules.keys().has("require_terrain") and !landform_rules["require_terrain"].has(tile_info[coord]["terrain"]):
		return false
	if !tile_info[coord].has("landform")  or tile_info[coord]["landform"] != null:
		return false
	return true


## 运行期生成/扩张用的简化判定
## 说明:
## - 这个判定不再复用“初始地图生成”阶段的高度/地形/密度规则。
## - 它只关心当前格子是否存在、是否尚未被占据。
## - 用于村庄扩张、祭坛召唤等“回合中途新增地块实体”的逻辑，
##   防止初始生成限制（例如 max_landform_ratio、require_height 等）错误地影响运行期扩张行为。
func can_spawn_runtime_at(coord: Vector2i, tile_info: Dictionary) -> bool:
	var c = Vector2i(coord)
	if not tile_info.has(c):
		return false

	var tile_data = tile_info[c]
	if tile_data == null:
		return false

	if tile_data.has("landform") and tile_data["landform"] != null:
		return false
	if tile_data.has("landform_in") and tile_data["landform_in"] != null:
		return false

	return true
	
func _add_landform_sprite(parent: Node2D, coord: Vector2, height: int, current_step_h: float, tile_scale : float) -> void:
	var tex: Texture2D = null
	if State_Main == Main_State_Pool.Broken:
		if landform_damaged_tex != null and not landform_damaged_tex.is_empty():
			tex = damaged_tex_picker()

	if tex == null:
		if landform_tex != null and not landform_tex.is_empty():
			tex = tex_picker()

	if tex == null:
		return

	var lf_sprite = Sprite2D.new()
	# 使用唯一名称避免命名冲突
	lf_sprite.name = "LandformSprite_%s_%s" % [coord.x, coord.y]
	lf_sprite.texture = tex
	# 【关键修复】：将新创建的精灵引用存入类的成员变量 self.tex
	# 这样 die() 里的 tex_toggle() 才能找到这个节点
	self.tex = lf_sprite
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
	
	# 【修改为】使用动态调用来发射信号，完美绕过编译器的静态检测
	owner_battle.emit_signal("CreateBar", self, Attitude, world_position.x, world_position.y)
	print(str(position) + ": 唤起血条中")
	#random_damage()  该条为血条调试相关


	parent.add_child(lf_sprite)
 
func Behavior(Step, info_in, Other, beha):
	# 默认行为：什么也不做
	# 子类可以重写此方法以实现特定行为
	pass


# ==========================================
# ★ 敌人意图接口默认实现
# ==========================================
# 说明:
# - 这些函数为未来的敌方/中立/友方单位提供统一接口。
# - 默认实现全部是“安全降级”版本，目的不是产生意图，而是保证未实现完整意图逻辑的单位不会报错。
# - 真正需要展示与生成时间轴意图的单位（如 village、祭坛等）应在子类中重写这些函数。

## 当前单位是否启用“敌人意图展示系统”
## 默认返回 false，意味着：
## - 不会生成时间轴意图
## - 地图 hover 时也不会触发新意图系统
func is_intent_preview_enabled() -> bool:
	return false


## 返回详细意图描述文本
## 默认返回一个通用字符串，仅作为兜底。
func get_intent_description() -> String:
	return "暂未配置意图"


## 返回地图效果范围，完全复用卡牌 effect_range 格式
## 默认只返回中心点
func get_intent_effect_range() -> Variant:
	return ["0,0"]


## 返回当前意图的目标中心格
## 默认无目标
func get_intent_target_center_coord(_hex_map: battle) -> Variant:
	return null


## 判断当前是否可以生成有效意图
## 默认返回 false，表示没有可用目标
func can_generate_intent(_hex_map: battle) -> bool:
	return false


## 返回当前意图无效原因
func get_intent_invalid_reason(_hex_map: battle) -> String:
	return "无可用目标"


## 返回目标阵营标签
## 当前默认使用 enemy，未来可扩展为 neutral / ally
func get_intent_target_affiliation() -> String:
	return "enemy"


## 当前意图是否覆盖自身
## 默认 false
func does_intent_include_self(_hex_map: battle) -> bool:
	return false

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


## 核心二进制矩阵解析引擎
func _parse_matrix_shape(matrix_str: String) -> void:
	timeline_shape_coords.clear()
	var rows: Array[String] = []
	var trimmed = matrix_str.strip_edges()
	
	# 智能分行：支持逗号、换行，或者单纯的 "011"
	if trimmed.contains(","):
		for row_text in trimmed.split(",", false):
			rows.append(row_text)
	elif trimmed.contains("\n"):
		for row_text in trimmed.split("\n", false):
			rows.append(row_text)
	elif trimmed.contains(" ") and (trimmed.contains("0") or trimmed.contains("1")):
		for row_text in trimmed.split(" ", false):
			rows.append(row_text)
	else:
		rows.append(trimmed)
	
	if rows.is_empty(): 
		return
	
	# ★ 严格按照你的需求：只寻找 "1" 并录入坐标
	for y in range(rows.size()):
		var row = rows[y].strip_edges()
		for x in range(row.length()):
			if row[x] == "1":
				# ★ 确保装入的是 Vector2i
				timeline_shape_coords.append(Vector2i(x, y))
	
	# 计算该形状占用的最大尺寸 (Size)
	if not timeline_shape_coords.is_empty():
		var max_x = 0
		var max_y = 0
		for coord in timeline_shape_coords:
			max_x = max(max_x, coord.x)
			max_y = max(max_y, coord.y)
		# 尺寸 = 最大坐标 + 1
		timeline_shape_size = Vector2i(max_x + 1, max_y + 1)
	else:
		# 兜底空白
		timeline_shape_size = Vector2i(1, 1)
		timeline_shape_coords.append(Vector2i(0, 0))
		

## 获取时间占位形状坐标 (供 TimelineManager 调用)
func get_intent_shape() -> Array[Vector2i]:
	parse_timeline_shape()  # 幂等性调用，确保形状已解析
	# 确保如果直接在检查器修改了 timeline_shape_key，也能被解析
	if timeline_shape_coords.is_empty():
		_parse_matrix_shape(timeline_shape_key)
	return timeline_shape_coords.duplicate()


## 获取时间占位形状尺寸
func get_timeline_shape_size() -> Vector2:
	parse_timeline_shape()  # 幂等性调用，确保形状已解析
	return timeline_shape_size


func set_health(new_hp: float) -> void:
	HP = clampf(new_hp, 0.0, Max_Blood)
	State_Update()
	Blood_change.emit(HP)
	
	# 更新 damage_rate 基于当前生命值
	if Max_Blood > 0:
		damage_rate = 1.0 - (float(HP) / float(Max_Blood))
		GameLogger.debug("地形血量更新: current_hp=%d, damage_rate=%.2f" % [HP, damage_rate], "landform")


func heal(amount: float) -> void:
	if amount <= 0:
		return
	set_health(HP + amount)


## 重写：受伤逻辑（更新 damage_rate 和 damage 状态）
func take_damage(amount: int) -> void:
	if State_Vice & Vice_State_Pool.protected != 0:
		State_Vice &= ~Vice_State_Pool.protected
		return
	# 这里可以播放通用的受伤动画，比如 $AnimationPlayer.play("hurt")
	set_health(HP - amount)
	GameLogger.debug("地形受伤: amount=%d, current_hp=%d, damage_rate=%.2f" % [amount, HP, damage_rate], "landform")
	


func State_Update():
	if HP <= 0:
		die()
	elif HP < Max_Blood * Capture_rate and HP > 0:
		Captured()
	else: 
		Revived()

func die() -> void:
	GameLogger.info("【地形实体死亡】%s 被摧毁" % landform_name, "landform")
	State_Main = Main_State_Pool.Broken
	
	# 因为上面【修复1】赋值了 self.tex，现在可以直接无缝切换为战损贴图了
	tex_toggle()
	
	damage_rate = 1.0
	
	# 【修复 3】既然已经用 tex_toggle() 切换了贴图，就不需要再通知 HexMap 重新生成整个视觉了
	# 注释掉下面这两行，彻底切断死循环路径
	# if owner_battle and owner_battle.has_method("add_landform_visual_at"):
	# 	owner_battle.add_landform_visual_at(location)
	
	# 【修复 4】删掉 free()！！！
	# 地貌变成“废墟”后，它依然是一个占据格子的实体，只是贴图变了。
	# 如果直接 free() 销毁内存，HexMap 去查这个格子时就会导致 Null 空指针崩溃！
	# free()

	# Broken 虽然不 queue_free，但它的战斗行为/敌人意图应当视作已失效。
	# 这里主动通知 HexMap 触发一次地形拓扑/意图重判。
	if is_instance_valid(owner_battle) and owner_battle.has_signal("tile_topology_changed"):
		owner_battle.tile_topology_changed.emit()

# ==========================================
# ★ 敌人意图接口
# ==========================================

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


## 设置时间占位形状 (供外部调用，如 village.gd 里的 set_timeline_shape("011"))
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
	# 安全检查：如果精灵节点还没创建（或者为空），直接返回
	if tex == null:
		# print("纹理模块为空")
		return
		
	# 根据不同的主状态切换纹理
	if State_Main == Main_State_Pool.Normal or State_Main == Main_State_Pool.Captured:
		# 检查数组是否为空，并且当前纹理是否不是我们要的那个（避免重复赋值）
		if landform_tex.size() > 0:
			if !landform_tex.has(tex.texture):
				tex.texture = tex_picker()
				
	elif State_Main == Main_State_Pool.Broken:
		# 修复点：对于损坏状态，应该使用 landform_damaged_tex 数组
		if landform_damaged_tex.size() > 0:
			if !landform_damaged_tex.has(tex.texture):
				tex.texture = damaged_tex_picker()
		else:
			# 如果万一没有配置损坏贴图，作为兜底，可以使用原图
			if landform_tex.size() > 0:
				tex.texture = tex_picker()
				
## ★ 恢复的幂等性检验函数
func parse_timeline_shape() -> void:
	# 如果当前要求解析的 key 和上次一样，并且坐标数组不是空的，直接跳过（节约性能）
	if _last_parsed_shape_key == timeline_shape_key and not timeline_shape_coords.is_empty():
		return
	
	# 如果没填，给个默认值
	if timeline_shape_key.is_empty():
		timeline_shape_key = "1"
		
	# 调用你写好的解析引擎
	_parse_matrix_shape(timeline_shape_key)
	
	# 记录下来，下次就不用再解析了
	_last_parsed_shape_key = timeline_shape_key

func tex_picker():
	return landform_tex[0]

func damaged_tex_picker():
	return landform_damaged_tex[0]
