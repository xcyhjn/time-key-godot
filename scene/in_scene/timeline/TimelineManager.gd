class_name TimelineManager
extends Node

const TimelineEnemyIntentPrioritySelectorScript = preload("res://scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentPrioritySelector.gd")
const TimelineEnemyIntentTargetResolverScript = preload("res://scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentTargetResolver.gd")
const TimelineEnemyIntentCandidateCollectorScript = preload("res://scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentCandidateCollector.gd")

# 时间轴网格使用0-based索引系统（与GDScript数组索引一致）
# X轴：时间轴位置，有效索引 0-11（共12个位置）
# Y轴：并发层，有效索引 0-2（共3层）
# 注意：显示给玩家的UI可能是1-based（1-12，1-3），但内部代码使用0-based索引
const GRID_WIDTH: int = 12
const GRID_HEIGHT: int = 3

# 核心网格字典：键为 Vector2i(x,y)，值为 TimelineAction
# 使用字典而不是二维数组，查找和删除更加灵活高效
var grid: Dictionary = { }
var _enemy_intent_priority_selector = null
var _enemy_intent_target_resolver = null
var _enemy_intent_candidate_collector = null
@export var max_enemy_intents_per_turn: int = 5  # 每回合最多允许多少个敌人排入时间轴
@export_group("敌人意图优先级")
## 没有声明 intent_priority / get_intent_priority() 的敌人使用这个默认优先级。
@export var default_enemy_intent_priority: int = 0
## 同优先级内是否随机抽取。开启后会先筛掉当前时间轴放不下的候选，再随机选一个。
@export var randomize_same_priority_enemy_intents: bool = true
## 是否把最终优先级写入 TimelineAction.action_data，方便 hover、调试面板或后续效果读取。
@export var write_intent_priority_to_action_data: bool = true

# ==========================================
# ★ 修复报错：声明用于记录当前悬浮行动的变量
# ==========================================
var hovered_action: TimelineAction = null

signal action_placed(action: TimelineAction)
signal action_executed(action: TimelineAction)
signal timeline_cleared
signal action_hovered_changed(action: TimelineAction, is_hovering: bool)


func _get_enemy_intent_priority_selector():
	if _enemy_intent_priority_selector == null:
		_enemy_intent_priority_selector = TimelineEnemyIntentPrioritySelectorScript.new()
	return _enemy_intent_priority_selector


func _get_enemy_intent_target_resolver():
	if _enemy_intent_target_resolver == null:
		_enemy_intent_target_resolver = TimelineEnemyIntentTargetResolverScript.new()
	return _enemy_intent_target_resolver


func _get_enemy_intent_candidate_collector():
	if _enemy_intent_candidate_collector == null:
		_enemy_intent_candidate_collector = TimelineEnemyIntentCandidateCollectorScript.new()
	return _enemy_intent_candidate_collector


# ==========================================
# ★ 占位与校验逻辑
# ==========================================


# 检查一个具体的形状在指定原点是否合法（不越界、不重叠）
func is_placement_valid(shape_coords: Array[Vector2i], origin: Vector2i) -> bool:
		#shape_coords, origin, GRID_WIDTH, GRID_HEIGHT, grid.size()
	#], "TimelineManager")
	
	# 验证origin本身是否在网格范围内（额外安全检查）
	if origin.x < 0 or origin.x >= GRID_WIDTH or origin.y < 0 or origin.y >= GRID_HEIGHT:
			#origin, GRID_WIDTH, GRID_HEIGHT
		#], "TimelineManager")
		return false
	
	# 检查形状边界是否适合网格
	if not shape_coords.is_empty():
		var min_x = shape_coords[0].x
		var max_x = shape_coords[0].x
		var min_y = shape_coords[0].y
		var max_y = shape_coords[0].y
		for offset in shape_coords:
			if offset.x < min_x:
				min_x = offset.x
			if offset.x > max_x:
				max_x = offset.x
			if offset.y < min_y:
				min_y = offset.y
			if offset.y > max_y:
				max_y = offset.y
		# 检查X轴边界
		if origin.x + min_x < 0 or origin.x + max_x >= GRID_WIDTH:
				#origin.x, min_x, max_x, GRID_WIDTH
			#], "TimelineManager")
			return false
		# 检查Y轴边界
		if origin.y + min_y < 0 or origin.y + max_y >= GRID_HEIGHT:
				#origin.y, min_y, max_y, GRID_HEIGHT
			#], "TimelineManager")
			return false
	
	for offset in shape_coords:
		var target_pos = origin + offset
			#offset, target_pos, origin, offset
		#], "TimelineManager")

		# 1. 越界检测
		if target_pos.x < 0 or target_pos.x >= GRID_WIDTH or target_pos.y < 0 or target_pos.y >= GRID_HEIGHT:
				#target_pos, GRID_WIDTH, GRID_HEIGHT
			#], "TimelineManager")
			return false

		# 2. 碰撞/重叠检测
		if grid.has(target_pos):
			var occupant = grid.get(target_pos)
			var occupant_info = str(occupant)
			if is_instance_valid(occupant) and occupant.has_method("get_absolute_coords"):
				# 这是一个TimelineAction对象
				var action_type = "UNKNOWN"
				var type_value = occupant.get("type")
				if type_value != null:
					if type_value == TimelineAction.Type.ENEMY:
						action_type = "ENEMY"
					elif type_value == TimelineAction.Type.PLAYER:
						action_type = "PLAYER"
				var origin_pos = occupant.get("origin_grid_pos")
				occupant_info = "TimelineAction(type=" + action_type + ", origin=" + (str(origin_pos) if origin_pos != null else "N/A") + ")"
				#target_pos, occupant_info
			#], "TimelineManager")
			return false
	return true


# 尝试放置一个行动到时间轴
func place_action(action: TimelineAction, origin: Vector2i) -> bool:
		#origin, "PLAYER" if action.type == TimelineAction.Type.PLAYER else "ENEMY"
	#], "TimelineManager")
	
	if not is_placement_valid(action.shape_coords, origin):
		return false

	action.origin_grid_pos = origin
	var abs_coords = action.get_absolute_coords()
		#abs_coords, grid.size()
	#], "TimelineManager")

	# 占据所有网格点
	for coord in abs_coords:
		grid[coord] = action
	action_placed.emit(action)
	if action.type == TimelineAction.Type.PLAYER:
		Signal_Bus.emit_timeline_action_added(action)
	return true

# ==========================================
# ★ 结算逻辑 (需求 4)
# ==========================================
# 结算整个时间轴，从左到右(时间)，从上到下(并发)
func resolve_timeline() -> void:
	# === 时间币结算逻辑保持不变 ===
	var total_slots = GRID_WIDTH * GRID_HEIGHT
	var occupied_slots = grid.size()
	var empty_slots = total_slots - occupied_slots
	
	if empty_slots > 0 and GlobalTimecoin:
		GlobalTimecoin.add_from_timeline(empty_slots)

	# 用于记录已经结算过的多格行动，防止巨型卡牌被结算多次
	var processed_actions: Array[TimelineAction] = []

	# 外层循环：时间列 (X轴，代表时间的流动)
	for x in range(GRID_WIDTH):
		var has_action_in_this_column = false
		
		# 内层循环：并发层 (Y轴)
		for y in range(GRID_HEIGHT):
			var pos = Vector2i(x, y)

			if grid.has(pos):
				var action: TimelineAction = grid[pos]

				# 防止占多格的行动被重复结算
				if not processed_actions.has(action):
					processed_actions.append(action)
					has_action_in_this_column = true
					
					action_executed.emit(action)
					
					# ★ 核心改动：把行动丢给 EffectProcessor 结算，并阻塞等待动画表现完成
					await EffectProcessor.process_action(action, get_tree())

					# 结算完毕后，立刻清理它在网格上占据的所有格子
					var all_occupied = action.get_absolute_coords()
					for occupied_pos in all_occupied:
						grid.erase(occupied_pos)
		
		# ★ 表现优化：如果这一列（这一秒）发生了行动，额外稍微停顿一下，体现出时间轴从左到右的“推进感”
		if has_action_in_this_column:
			await get_tree().create_timer(0.2).timeout


	# 彻底清空兜底
	grid.clear()

	# 强制清除高亮状态
	if hovered_action != null:
		action_hovered_changed.emit(hovered_action, false)
		hovered_action = null

	timeline_cleared.emit()
# ==========================================
# ★ 敌方 AI 辅助寻位与生成
# ==========================================


# ==========================================
# ★ 随机寻位算法 (收集所有合法空位并抽奖)
# ==========================================
func find_random_available_spot(shape_coords: Array[Vector2i]) -> Vector2i:
	if shape_coords.is_empty():
		return Vector2i(-1, -1)
	
	var valid_spots: Array[Vector2i] = []
	
		#shape_coords, GRID_WIDTH, GRID_HEIGHT
	#], "TimelineManager")
	
	# 网格适配优化：计算形状的X和Y轴范围，减少不必要的原点测试
	var min_shape_x = shape_coords[0].x
	var max_shape_x = shape_coords[0].x
	var min_shape_y = shape_coords[0].y
	var max_shape_y = shape_coords[0].y
	for coord in shape_coords:
		if coord.x < min_shape_x:
			min_shape_x = coord.x
		if coord.x > max_shape_x:
			max_shape_x = coord.x
		if coord.y < min_shape_y:
			min_shape_y = coord.y
		if coord.y > max_shape_y:
			max_shape_y = coord.y
	
	var shape_width = max_shape_x - min_shape_x + 1
	var shape_height = max_shape_y - min_shape_y + 1
		#min_shape_x, max_shape_x, shape_width, min_shape_y, max_shape_y, shape_height
	#], "TimelineManager")
	
	# 网格适配：根据形状尺寸确定可能的原点范围
	# 时间轴网格宽度为12（0-11），原点x必须满足：0 ≤ x + min_shape_x 且 x + max_shape_x ≤ 11
	# 时间轴网格高度为3（0-2），原点y必须满足：0 ≤ y + min_shape_y 且 y + max_shape_y ≤ 2
	var min_valid_x = max(0, -min_shape_x)  # 确保 x + min_shape_x ≥ 0
	var max_valid_x = min(GRID_WIDTH - 1, GRID_WIDTH - 1 - max_shape_x)  # 确保 x + max_shape_x ≤ 11
	var min_valid_y = max(0, -min_shape_y)  # 确保 y + min_shape_y ≥ 0
	var max_valid_y = min(GRID_HEIGHT - 1, GRID_HEIGHT - 1 - max_shape_y)  # 确保 y + max_shape_y ≤ 2
	
	
	if min_valid_x > max_valid_x:
		return Vector2i(-1, -1)
	
	if min_valid_y > max_valid_y:
		return Vector2i(-1, -1)

	# 遍历可能的网格位置（使用优化后的范围）
	for x in range(min_valid_x, max_valid_x + 1):
		for y in range(min_valid_y, max_valid_y + 1):
			var test_origin = Vector2i(x, y)
			# 如果这个位置能放下该形状，就存进备选库
			if is_placement_valid(shape_coords, test_origin):
				valid_spots.append(test_origin)
			else:
				pass

		#valid_spots.size(), valid_spots
	#], "TimelineManager")
	
	# 如果有备选空位，使用 Godot 自带的强大 pick_random() 随机挑一个
	if valid_spots.size() > 0:
		var selected = valid_spots.pick_random()
		return selected

	return Vector2i(-1, -1)  # 彻底没位置了


# ==========================================
# ★ 敌方意图生成 (优先级分层 + 同级可放置随机抽取)
# ==========================================
func generate_enemy_intents(enemies_on_board: Array):
	var current_intent_count = 0
	var hex_map: battle = _get_current_hex_map()
	var candidates: Array[Dictionary] = _collect_enemy_intent_candidates(enemies_on_board, hex_map)
	var priority_values: Array[int] = _get_sorted_priority_values(candidates)

	# 优先级越高越先处理。
	# 同级内每轮都会先筛出“当前时间轴还能放下”的候选，再随机抽取一个，
	# 这样高优先级不会被低优先级抢位置，同优先级又不会因为数组顺序变得死板。
	for priority in priority_values:
		var same_priority_candidates: Array[Dictionary] = _filter_candidates_by_priority(candidates, priority)
		while current_intent_count < max_enemy_intents_per_turn:
			var placement: Dictionary = _pick_placeable_candidate(same_priority_candidates)
			if placement.is_empty():
				break

			var candidate: Dictionary = placement["candidate"]
			var enemy: Node = candidate["enemy"] as Node
			var spot: Vector2i = placement["spot"]
			var target_tile: Node = _resolve_intent_target_tile(enemy, hex_map)
			var action: TimelineAction = enemy.get_intent_action(target_tile)
			_apply_intent_priority_to_action(action, int(candidate["priority"]))

			if place_action(action, spot):
				current_intent_count += 1
				same_priority_candidates.erase(candidate)
			else:
				# 理论上 _pick_placeable_candidate 已经过合法性检查。
				# 这里保留兜底，避免极端情况下同一个候选反复尝试。
				same_priority_candidates.erase(candidate)


func _get_current_hex_map() -> battle:
	if get_tree().current_scene == null:
		return null
	return get_tree().current_scene.get_node_or_null("map/HexMap") as battle


## 收集所有“本回合可以进入时间轴生成”的敌方意图候选。
## 这里只做协议检查、目标有效性检查和形状缓存，不直接写入时间轴。
func _collect_enemy_intent_candidates(enemies_on_board: Array, hex_map: battle) -> Array[Dictionary]:
	return _get_enemy_intent_candidate_collector().collect_candidates(
		enemies_on_board,
		hex_map,
		Callable(self, "_get_enemy_intent_priority")
	)


## 读取敌人优先级。
## 优先使用 get_intent_priority()，其次读取 intent_priority 变量，最后回退到默认值。
func _get_enemy_intent_priority(enemy: Node) -> int:
	return _get_enemy_intent_priority_selector().get_enemy_intent_priority(enemy, default_enemy_intent_priority)


## 返回从高到低排序后的优先级列表。
func _get_sorted_priority_values(candidates: Array[Dictionary]) -> Array[int]:
	return _get_enemy_intent_priority_selector().get_sorted_priority_values(candidates)


func _filter_candidates_by_priority(candidates: Array[Dictionary], priority: int) -> Array[Dictionary]:
	return _get_enemy_intent_priority_selector().filter_candidates_by_priority(candidates, priority)


## 同级内先筛出当前能放进时间轴的候选，再随机/顺序选择一个。
## 返回值包含 candidate 与已算好的 spot，避免重复寻位。
func _pick_placeable_candidate(candidates: Array[Dictionary]) -> Dictionary:
	return _get_enemy_intent_priority_selector().pick_placeable_candidate(
		candidates,
		Callable(self, "find_random_available_spot"),
		randomize_same_priority_enemy_intents
	)


## 将地图上的“目标中心格”映射成 stack，确保时间轴意图与地图侧目标一致。
func _resolve_intent_target_tile(enemy: Node, hex_map: battle) -> Node:
	return _get_enemy_intent_target_resolver().resolve_target_tile(enemy, hex_map)


## 把生成时读取到的优先级写入 action，便于后续调试或展示层读取。
func _apply_intent_priority_to_action(action: TimelineAction, priority: int) -> void:
	if action == null:
		return
	action.intent_priority = priority
	if write_intent_priority_to_action_data:
		action.action_data["intent_priority"] = priority


## 重判当前时间轴中的敌人意图是否合法
## 触发时机:
## - 地块升降
## - 地块销毁
## - 敌人名单变化
##
## 作用:
## - 若某个敌人意图从“原本有效”变成“当前无效”，则让时间轴对应方块暗淡后移除
## - 这样可以满足你提出的“回合中途被打消意图”的需求
func revalidate_enemy_intents(hex_map: battle, timeline_ui: Control = null) -> void:
	if not is_instance_valid(hex_map):
		return

	var processed: Dictionary = {}
	var invalid_actions: Array[TimelineAction] = []

	for action in grid.values():
		if action == null or action.type != TimelineAction.Type.ENEMY:
			continue

		var action_id = action.get_instance_id()
		if processed.has(action_id):
			continue
		processed[action_id] = true

		var source = action.source_node
		if not is_instance_valid(source):
			invalid_actions.append(action)
			continue

		if source.has_method("is_intent_preview_enabled") and not source.is_intent_preview_enabled():
			invalid_actions.append(action)
			continue

		if source.has_method("can_generate_intent") and not source.can_generate_intent(hex_map):
			invalid_actions.append(action)

	if invalid_actions.is_empty():
		return

	for action in invalid_actions:
		remove_action_with_fade(action, timeline_ui, "enemy_intent_invalid")


## 从时间轴中移除某个行动，并让 UI 播放“变暗、透明、轻微下落”的失效动画。
## 这个接口专门用于回合结算以外的中途移除，例如：
## - 敌人意图丢失目标
## - 释放者死亡或意图接口关闭
## - 后续卡牌效果主动打消某个时间占位
##
## 注意：回合结束 resolve_timeline() 的正常结算路径不走这里，避免破坏原本从左到右的结算节奏。
func remove_action_with_fade(action: TimelineAction, timeline_ui: Control = null, reason: String = "") -> void:
	if not is_instance_valid(action):
		return

	if timeline_ui != null:
		if timeline_ui.has_method("animate_action_removal"):
			timeline_ui.animate_action_removal(action, reason)
		elif timeline_ui.has_method("animate_enemy_intent_removal"):
			timeline_ui.animate_enemy_intent_removal(action)

	for coord in action.get_absolute_coords():
		if grid.get(coord) == action:
			grid.erase(coord)

	if hovered_action == action:
		action_hovered_changed.emit(action, false)
		hovered_action = null


func _on_block_hovered(action: TimelineAction):
	hovered_action = action
	# 向上层（战斗主控）发射信号
	action_hovered_changed.emit(action, true)


func _on_block_exited():
	if hovered_action != null:
		# 发射取消高亮的信号
		action_hovered_changed.emit(hovered_action, false)
		hovered_action = null


## 清除网格中的所有占用
func clear_grid() -> void:
	grid.clear()
	hovered_action = null
	timeline_cleared.emit()

## 调试函数：打印网格状态
func debug_print_grid() -> void:
	for pos in grid.keys():
		var action = grid[pos]
		var info = str(pos) + ": "
		if is_instance_valid(action) and action.has_method("get_absolute_coords"):
			var action_type = "UNKNOWN"
			var type_value = action.get("type")
			if type_value != null:
				if type_value == TimelineAction.Type.ENEMY:
					action_type = "ENEMY"
				elif type_value == TimelineAction.Type.PLAYER:
					action_type = "PLAYER"
			var origin_pos = action.get("origin_grid_pos")
			info += "TimelineAction(type=" + action_type + ", origin=" + (str(origin_pos) if origin_pos != null else "N/A") + ")"
		else:
			info += str(action)
