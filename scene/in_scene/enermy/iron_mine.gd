class_name iron_mine
extends landform

static var Library : Array[Vector2i]

func _init(location_in : Vector2i,battle_in):
	damage_rate = 0.5
	Max_Blood = 100
	random_damage()
	super._init(
	"iron_mine", 
	["res://image/enermy/iron_mine/iron_mine.png"], 
	["res://image/enermy/iron_mine/iron_mine_damaged.png"], 
	{ 
	"chance": 0.3},
	location_in,
	false,
	battle_in)
	# 矿井作为局外锻造奖励入口，需要被收获系统识别为敌方建筑，并在结算时打开 craft 奖励场景。
	Attitude = Attitude_Pool.Enemy
	settlement_reward_type = "craft"
	settlement_reward_label = "锻造奖励"
	willing_pool = {}
	if location_in == Vector2i(-100, -100):
		return
	Library.append(location_in)
	if owner_battle != null:
		owner_battle.add_landform_visual_at(target)
	neighbors = get_neighbor_coords(location_in)

func Behavior(Step, info_in, Other, beha, rng):
	if State_Main == Main_State_Pool.Broken:
		return
	#print(step)
	if beha != -1:
		willing = beha
	for neighbor in neighbors:
		if not info_in.has(neighbor):
			continue
		if info_in[neighbor].keys().has("landform_in") and  info_in[neighbor]["landform_in"] != null:
			if info_in[neighbor]["landform_in"].will:
				info_in[neighbor]["landform_in"].willing_pool.values()[info_in[neighbor]["landform_in"].willing].call(info_in, rng)



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
		return "铁矿：使周围地貌行动加一次"


## 返回地图效果范围。
## 这里改为显式偏移数组写法，避免阅读时还要额外换算“半径1”。
## 这 7 个偏移分别对应：
## - 自身中心格
## - 六边形网格的六个相邻方向
func get_intent_effect_range() -> Variant:
	return [
		"0,0",
		"1,0",
		"1,-1",
		"0,-1",
		"-1,0",
		"-1,1",
		"0,1"
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
	var action_data: Dictionary = _get_intent_action_data_builder().build_standard_action_data(
		get_intent_description(),
		landform_name,
		location,
		target_tile,
		get_intent_effect_range(),
		get_intent_invalid_reason(owner_battle)
	)

	var action = TimelineAction.new(
		TimelineAction.Type.ENEMY,
		self,
		target_tile,
		get_intent_shape(),
		Color(0.8, 0.2, 0.2, 0.8),
		action_data
	)

	return action
