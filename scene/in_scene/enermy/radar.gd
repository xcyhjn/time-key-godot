class_name radar
extends landform

var underling : Array[radar] = []
var State_Vice_Locked
var State_Main_Locked
var locked : bool

func _init(location_in : Vector2i,battle_in):
	damage_rate = 0.5
	Max_Blood = 100
	super._init(
	"radar", 
	["res://image/enermy/radar/radar.png",
	"res://image/enermy/radar/radar_underling.png"], 
	["res://image/enermy/radar/radar_broken.png"], 
	{
	"chance": 0.3},
	location_in,
	false,
	battle_in)
	Attitude = Attitude_Pool.Enemy
	settlement_reward_type = "shop"
	settlement_reward_label = "商店"
	# 设置时间占位形状为1x2
	willing_pool = {Only_will : Only_Done}
	
	if location_in == Vector2i(-100, -100):
		return
	target = location_in



func Only_will(tile_info : Dictionary):
	pass

func Only_Done(tile_info : Dictionary):
	if Underlings or locked:
		return
	var buffer_pool: Array = []
	var buffer_bool : bool = true
	var buffer_underling
	while underling.size() < 2:
	# 1. 圈定候选池（依据高度规则）
		if landform_rules.has("require_height") and landform_rules["require_height"] != null:
			var valid_heights = landform_rules["require_height"]
		# 从允许的高度中随机挑一个高度层
			var target_h = valid_heights[randi() % valid_heights.size()]
			buffer_pool = GlobalClock.tile_h_pool[target_h].duplicate()
		else:
			# 没有高度限制，全图可放
			buffer_pool = tile_info.keys().duplicate()
			
		# 2. 如果候选池是空的，直接销毁探针返回失败
		if buffer_pool.is_empty():
			return

		# 3. 在候选池中随机抽取并验证
		var coord_index = randi() % buffer_pool.size()
		var test_coord = buffer_pool[coord_index]
	
		# 循环验证，如果该坐标不合法，踢出池子继续抽
		while not buffer_pool.is_empty() and not get_possible_coords(test_coord, tile_info):
			buffer_pool.remove_at(coord_index)
			if buffer_pool.is_empty():
				break
			# 【修复2】：只有在不为空时才计算 modulo，防止除零崩溃
			coord_index = randi() % buffer_pool.size()
			test_coord = buffer_pool[coord_index]
		print("\n \n \n" +str(buffer_pool.is_empty()) + " \n \n \n")
		if buffer_pool.is_empty():
			break
		else:
			buffer_underling = new(test_coord, owner_battle)
			buffer_underling.Underlings = true
			var registered_successfully := false
			if owner_battle.has_method("register_runtime_landform"):
				registered_successfully = owner_battle.register_runtime_landform(test_coord, buffer_underling, buffer_underling.landform_name)
			else:
				tile_info[test_coord]["landform"] = buffer_underling
				tile_info[test_coord]["landform_in"] = buffer_underling
				tile_info[test_coord]["landform_type"] = buffer_underling.landform_name
				owner_battle.add_landform_visual_at(test_coord)
				registered_successfully = true

			if not registered_successfully:
				buffer_underling.queue_free()
				break
			
			if buffer_underling.get("Attitude") == buffer_underling.Attitude_Pool.Enemy:
				buffer_underling.add_to_group("Enemies")
			elif buffer_underling.get("Attitude") == buffer_underling.Attitude_Pool.Middle:
				buffer_underling.add_to_group("Middle")
			underling.append(buffer_underling)
		locked = true
			

func Behavior(Step, info_in, Other, beha, rng):
	locked = (!underling.is_empty())
	if !locked:
		State_Main_Locked = State_Main
		State_Vice_Locked = State_Vice
	# 如果村庄已经变成废墟，则不再执行任何扩张行为
	if State_Main == Main_State_Pool.Broken or Underlings or locked:
		return
	print(step)
	if beha != -1:
		willing = beha
	if will:
		willing_pool.values()[willing].call(info_in)
		will = false
		if willing < willing_pool.size() - 1:
			willing += 1
		else:
			willing = 0
		print(willing_pool.values()[willing])
	else:
		willing_pool.keys()[willing].call(info_in)
		will = true
		print(willing_pool.keys()[willing])

func die() -> void:
	State_Main = Main_State_Pool.Broken
	
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
	if Underlings:
		call_deferred("free")
		HealthBar_free.emit()
	else:
		tex_toggle()
		if is_instance_valid(owner_battle) and owner_battle.has_signal("tile_topology_changed"):
			owner_battle.tile_topology_changed.emit()


func tex_picker():
	if Underlings:
		return landform_tex[1]
	else:
		return landform_tex[0]

func damaged_tex_picker():
	return landform_damaged_tex[0]

func take_damage(amount: int) -> void:
	if State_Vice & Vice_State_Pool.protected != 0 or locked:
		State_Vice &= ~Vice_State_Pool.protected
		return
	# 这里可以播放通用的受伤动画，比如 $AnimationPlayer.play("hurt")
	set_health(HP - amount)
## ==========================================
## ★ 敌人意图接口（供 EnemyIntentResolver 调用）
## ==========================================

## 村庄显式启用意图展示与时间轴意图生成
func is_intent_preview_enabled() -> bool:
	# 说明：
	# - 即使村庄已经 Broken，我们依然允许它参与“地图侧灰态意图展示”，
	#   这样 hover 到废墟时仍可看到“建筑已损毁 / 无可用目标”的说明。
	# - 真正决定是否能进入时间轴生成的是 can_generate_intent()。
	return true


## 返回详细意图文本，供 tooltip 显示
func get_intent_description() -> String:
	return "产生虚影并提供全方位防护"


## 返回地图效果范围，完全复用卡牌 effect_range 的格式
## 村庄扩张只作用于目标中心本身，因此返回单点
func get_intent_effect_range() -> Variant:
	return ["0,0"]


## 返回当前展示用的目标中心格
func get_intent_target_center_coord(hex_map: battle) -> Variant:
	if State_Main == Main_State_Pool.Broken:
		return null
	if not is_instance_valid(hex_map):
		return null
	var array_buffer: Array
	array_buffer.append(location)
	for i in underling:
		array_buffer.append(i.location)
	var possible_targets = array_buffer
	if possible_targets.is_empty():
		return null

	return possible_targets[0]


## 判定当前回合能否生成有效意图
func can_generate_intent(hex_map: battle) -> bool:
	if State_Main == Main_State_Pool.Broken:
		return false
	return get_intent_target_center_coord(hex_map) != null


## 返回当前意图无效原因
func get_intent_invalid_reason(hex_map: battle) -> String:
	if State_Main == Main_State_Pool.Broken:
		return "建筑已损毁"
	if can_generate_intent(hex_map):
		return ""
	return "无可用目标"


## 目标阵营标签，后续可扩展 neutral / ally
func get_intent_target_affiliation() -> String:
	return "enemy"


## 村庄扩张不覆盖自身
func does_intent_include_self(_hex_map: battle) -> bool:
	return false


## 重写意图行动方法，为村庄添加详细意图描述和 effect_range
func get_intent_action(target_tile: Node = null) -> TimelineAction:
	# 字段说明：
	# - "位置": 意图发出者自身所在的六边形坐标，也就是这个建筑“站在哪里”。
	#   这里用的是 village 自己的 location（逻辑坐标，Vector2i）。
	#   它用于标识施法者/发出者本体。
	#   为什么一定要标识施法者：
	#   1. 地图 hover 时，需要知道“到底是谁在发动这个意图”，这样才能高亮建筑本体。
	#   2. tooltip 永远生成在发出者右侧，所以必须知道发出者是谁、它站在哪一格。
	#   3. 时间轴上的意图方块和地图上的建筑是绑定的，需要靠这个“发出者身份”把二者关联起来。
	#   4. 如果建筑死亡 / Broken / 地形变化导致它失效，也要靠这个发出者身份去重判并清除意图。
	#   5. 当多个建筑可能瞄准同一个目标格时，只有记录发出者，系统才能分清“是谁在对这个目标生效”。
	#
	# - "目标": 这次意图实际瞄准或作用的中心地块位置。
	#   这里取的是 target_tile.position（场景里的像素坐标，Vector2）。
	#   它用于标识这次行为“打到哪里/扩到哪里”。
	#
	# 对村庄来说：
	# - "位置" = 村庄自己当前站的格子
	# - "目标" = 村庄准备扩张过去的那一格
	var action_data = {
		"效果": get_intent_description(),
		"类型": landform_name,
		"位置": location, # 发出者自身的位置（逻辑六边形坐标）
		"目标": target_tile.position if target_tile else Vector2.ZERO, # 本次意图瞄准的目标中心（场景像素坐标）
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
