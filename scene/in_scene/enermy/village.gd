class_name village
extends landform

static var Library : Array[Vector2]
var possible_neighbor : Array[Vector2i]



func _init(location_in : Vector2,battle_in):
	damage_rate = 0.5
	Max_Blood = 100
	super._init(
	"village", 
	["res://image/enermy/villiage/Highest_Building.png"], 
	["res://image/enermy/villiage/Highest_Building_Broken.png"], 
	{"require_height": [1, 2, 3, 4, 5], 
	"chance": 0.3},
	location_in,
	false,
	battle_in)
	Attitude = Attitude_Pool.Enemy
	# 设置时间占位形状为1x2
	set_timeline_shape("011")
	
	target = location_in
	Library.append(location_in)
	willing_pool = {single_expand_unsure_will : single_expand_unsure_done,
					single_expand_sure_will : single_expand_sure_done,
					all_expand_will : all_expand_done}
	if owner_battle != null:
		owner_battle.add_landform_visual_at(target)

func get_possible_neighbor_coords(tile_info, rng: RandomNumberGenerator):
	var result : Array[Vector2i] = []
	for coord in neighbors:
		# 安全检查：确保字典中存在该坐标的键
		var coord_key = Vector2i(coord)  # 转换为 Vector2 作为字典键
		if not tile_info.has(coord_key):
			continue
		var tile_data = tile_info[coord_key]
		
		# ★ 关键修复：
		# 村庄扩张属于“运行期新增地貌”，不应该再复用初始地图生成时的高度/地形/密度限制。
		# 这里改为只检查运行期是否可占据该格。
		if not can_spawn_runtime_at(coord_key, tile_info):
			continue
		
		result.append(coord)
	return result


## ★ 简化检查：无视地形高度，只要地块未被占用即可扩张
func check_can_expand(tile_data: Dictionary) -> bool:
	# 检查是否已有地貌（支持两种键名：landform和landform_in）
	if tile_data.has("landform") and tile_data["landform"] != null:
		return false
	if tile_data.has("landform_in") and tile_data["landform_in"] != null:
		return false
	return true


func single_expand_unsure_will(tile_info : Dictionary, rng : RandomNumberGenerator) -> Vector2:
	if possible_neighbor.is_empty():
		possible_neighbor = get_possible_neighbor_coords(tile_info,rng)
	if possible_neighbor.is_empty():
		target = Vector2(-1, -1)
		return Vector2(-1, -1)
	var buffer : int = randi_range(0, possible_neighbor.size() - 1)
	target = possible_neighbor[buffer]
	return target

func single_expand_unsure_done(tile_info : Dictionary, rng : RandomNumberGenerator) -> void:
	if target == Vector2(-1, -1):
		return
	
	# 安全检查：确保字典中存在该坐标的键
	var target_key = Vector2(target)  # 转换为 Vector2 作为字典键
	if not tile_info.has(target_key):
		return
	var tile_data = tile_info[target_key]
	
	if tile_data != null:
		# ★ 修改：使用简化检查，无视地形高度和地形类型
		if check_can_expand(tile_data):
			var new_village = village.new(target,self.owner_battle)
			# 同时设置两种键名以确保兼容性
			tile_data["landform"] = new_village
			tile_data["landform_in"] = new_village
			owner_battle.add_landform_visual_at(target)
			print("不确定扩张完毕！")
		else:
			if single_expand_unsure_will(tile_info, rng) == Vector2(-1, -1):
				return
			else:
				target = single_expand_unsure_will(tile_info, rng)
				single_expand_unsure_done(tile_info, rng)
	return

func single_expand_sure_will(tile_info : Dictionary, rng : RandomNumberGenerator) -> Vector2:
	if possible_neighbor.is_empty():
		possible_neighbor = get_possible_neighbor_coords(tile_info,rng)
	if possible_neighbor.is_empty():
		target = Vector2(-1 , -1)
		return Vector2(-1 , -1)
	var buffer : int = randi_range(0, possible_neighbor.size() - 1)
	target = possible_neighbor[buffer]
	return target

func single_expand_sure_done( tile_info : Dictionary, rng : RandomNumberGenerator) -> void:
	if target == Vector2(-1 , -1):
		return
	
	# 安全检查：确保字典中存在该坐标的键
	var target_key = Vector2(target)  # 转换为 Vector2 作为字典键
	if not tile_info.has(target_key):
		return
	var tile_data = tile_info[target_key]
	
	if tile_data != null:
		# ★ 修改：使用简化检查，无视地形高度和地形类型
		if check_can_expand(tile_data):
			var new_village = village.new(target,self.owner_battle)
			# 同时设置两种键名以确保兼容性
			tile_data["landform"] = new_village
			tile_data["landform_in"] = new_village
			owner_battle.add_landform_visual_at(target)
			print("确定扩张完毕！")
	return

func all_expand_will(tile_info : Dictionary, rng : RandomNumberGenerator) -> void:
	pass

func all_expand_done(tile_info : Dictionary, rng : RandomNumberGenerator) -> void:
	if possible_neighbor.is_empty():
		return
	for coord in possible_neighbor:
		# 安全检查：确保字典中存在该坐标的键
		var coord_key = Vector2(coord)  # 转换为 Vector2 作为字典键
		if not tile_info.has(coord_key):
			continue
		var tile_data = tile_info[coord_key]
		if tile_data != null:
			# ★ 修改：使用简化检查，无视地形高度和地形类型
			if check_can_expand(tile_data):
				var new_village = village.new(coord,self.owner_battle)
				# 同时设置两种键名以确保兼容性
				tile_data["landform"] = new_village
				tile_data["landform_in"] = new_village
				owner_battle.add_landform_visual_at(coord)
				print("全体扩张完毕！")
	return


func Behavior(Step, info_in, Other, beha):
	# 如果村庄已经变成废墟，则不再执行任何扩张行为
	if State_Main == Main_State_Pool.Broken:
		return
	print(step)
	if beha != -1:
		willing = beha
	var rng = RandomNumberGenerator.new()
	possible_neighbor = get_possible_neighbor_coords(info_in, rng)
	if will:
		willing_pool.values()[willing].call(info_in, rng)
		will = false
		if willing < 2:
			willing += 1
		else:
			willing = 0
		print(willing_pool.values()[willing])
	else:
		willing_pool.keys()[willing].call(info_in, rng)
		will = true
		print(willing_pool.keys()[willing])


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
	return "扩张1：向相邻空地扩建1格"


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

	var possible_targets = get_possible_neighbor_coords(hex_map.map_data, RandomNumberGenerator.new())
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

	GameLogger.debug("创建村庄意图行动: %s, 效果: %s" % [landform_name, action_data["效果"]], "village")
	return action
