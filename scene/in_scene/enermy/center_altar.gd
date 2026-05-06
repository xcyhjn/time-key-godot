class_name center_altar
extends landform


var rivet_land : String = ""
var timeline_buffer = "111111"


func _init(location_in : Vector2i,battle_in):
	damage_rate = 0.5
	Max_Blood = 100
	super._init(
	"center altar", 
	["res://image/enermy/center_altar/center_altar.png"], 
	["res://image/enermy/center_altar/broken_center_altar.png"], 
	{"require_height": [1, 2, 3, 4, 5], 
	"chance": 0.3},
	location_in,
	false,
	battle_in)
	Attitude = Attitude_Pool.Enemy
	# 中心祭坛的扩张意图需要最高调度权，确保它先于普通敌人占用时间轴。
	intent_priority = 999
	settlement_reward_type = "shop"
	settlement_reward_label = "商店"
	willing_pool = {Only_will : Only_Done}
	target = location_in

func get_possible_coords(coord : Vector2i, tile_info : Dictionary) -> bool:
	if landform_rules.keys().has("require_height") and !landform_rules["require_height"].has(tile_info[coord]["height"]) :
		return false
	if landform_rules.keys().has("require_terrain") and !landform_rules["require_terrain"].has(tile_info[coord]["terrain"]):
		return false
	if !tile_info[coord].has("landform")  or tile_info[coord]["landform"] != null:
		return false
	neighbors = get_neighbor_coords(coord)
	var bool_buffer = false
	for neighbor in neighbors:
		# ★★★ 核心修复：必须先检查地图字典中是否存在这个邻居坐标！★★★
		if tile_info.has(neighbor):
			# 只有确定坐标存在，才能安全地访问 tile_info[neighbor]
			if tile_info[neighbor].has("landform") and tile_info[neighbor]["landform"] != null and (tile_info[neighbor]["landform"].landform_name == "forest" or tile_info[neighbor]["landform"].landform_name == "pond"):
				bool_buffer = true
				rivet_land = tile_info[neighbor]["landform"].landform_name
				break # 优化：只要找到周围哪怕一格有森林，就直接跳出循环，节省性能
	return bool_buffer

func Only_will(tile_info : Dictionary):
	set_timeline_shape(timeline_buffer + "," + timeline_buffer + "," + timeline_buffer)

func Only_Done(tile_info : Dictionary):
	set_timeline_shape(timeline_buffer + "," + timeline_buffer + "," + timeline_buffer)
	if timeline_buffer.length() < 12:
		timeline_buffer += "111111"

func Behavior(Step, info_in, Other, beha, rng):
	if beha != -1:
		willing = beha
	if will:
		willing_pool.values()[willing].call(info_in)
		will = false
		if willing < willing_pool.size():
			willing += 1
		else:
			willing = 0
	else:
		willing_pool.keys()[willing].call(info_in)
		will = true
	


## ==========================================
## ★ 敌人意图接口（供 TimelineManager / EnemyIntentResolver / PresentationController 调用）
## ==========================================
## 这一组函数就是“敌人要接入意图系统”时必须实现的标准协议。
## 只要按这个格式实现，现有系统就会自动接上：
## - 时间轴意图生成
## - 地图 hover -> tooltip 与范围高亮
## - 时间轴 hover -> 地图高亮与 tooltip

## 显式启用祭坛的敌人意图展示系统。
func is_intent_preview_enabled() -> bool:
	return true


## 返回祭坛意图的说明文本。
## 这里尽量让文本直接对应真实效果，保证 hover tooltip 能看懂。
func get_intent_description() -> String:
	return "快速扩张，几乎挤满时间轴"


## 返回地图效果范围。
## 这里改为显式偏移数组写法，避免阅读时还要额外换算“半径1”。
## 这 1 个偏移分别对应：
## - 自身中心格
func get_intent_effect_range() -> Variant:
	return [
		"0,0"
	]


## 祭坛的目标中心就是它自己所在的位置。
## 这样地图高亮和 tooltip 锚点都会以祭坛本体为中心展开。
func get_intent_target_center_coord(hex_map: battle) -> Variant:
	if State_Main == Main_State_Pool.Broken:
		return null
	if not is_instance_valid(hex_map):
		return null
	if not hex_map.map_data.has(location):
		return null
	return location


## 判断当前回合祭坛能否生成意图。
## 对祭坛来说，只要它没损毁且仍然存在于地图上，就可以稳定生成意图。
func can_generate_intent(hex_map: battle) -> bool:
	if State_Main == Main_State_Pool.Broken:
		return false
	if not is_instance_valid(hex_map):
		return false
	return hex_map.map_data.has(location)


## 返回当前意图无效原因，用于 tooltip 第二行红字提示。
func get_intent_invalid_reason(hex_map: battle) -> String:
	if State_Main == Main_State_Pool.Broken:
		return landform_name + "已损毁"
	if not is_instance_valid(hex_map):
		return "地图无效"
	if not hex_map.map_data.has(location):
		return landform_name + "位置无效"
	return ""


## 目标阵营标签。
## 虽然祭坛是范围型辅助/影响类效果，但它属于敌方侧的意图，因此继续标记为 enemy。
func get_intent_target_affiliation() -> String:
	return "enemy"


## 祭坛的意图范围明确包含自身中心格。
func does_intent_include_self(_hex_map: battle) -> bool:
	return true


## 重写时间轴意图行动，给祭坛补齐标准 action_data。
## 这一份数据会同时服务于：
## - 时间轴放置
## - 地图 hover 展示
## - 敌人意图解析器生成 EnemyIntentData
func get_intent_action(target_tile: Node = null) -> TimelineAction:
	var action_data = {
		"效果": get_intent_description(),
		"类型": landform_name,
		"位置": location,
		"目标": target_tile.position if target_tile else Vector2.ZERO,
		"effect_range": get_intent_effect_range(),
		"invalid_reason": get_intent_invalid_reason(owner_battle)
	}

	var action = TimelineAction.new(
		TimelineAction.Type.ENEMY,
		self,
		target_tile,
		get_intent_shape(),
		Color(0.8, 0.2, 0.2, 0.8),
		action_data
	)

	return action
