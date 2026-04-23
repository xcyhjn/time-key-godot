class_name TimelineManager
extends Node

# 时间轴网格使用0-based索引系统（与GDScript数组索引一致）
# X轴：时间轴位置，有效索引 0-11（共12个位置）
# Y轴：并发层，有效索引 0-2（共3层）
# 注意：显示给玩家的UI可能是1-based（1-12，1-3），但内部代码使用0-based索引
const GRID_WIDTH: int = 12
const GRID_HEIGHT: int = 3

# 核心网格字典：键为 Vector2i(x,y)，值为 TimelineAction
# 使用字典而不是二维数组，查找和删除更加灵活高效
var grid: Dictionary = { }
@export var max_enemy_intents_per_turn: int = 5  # 每回合最多允许多少个敌人排入时间轴

# ==========================================
# ★ 修复报错：声明用于记录当前悬浮行动的变量
# ==========================================
var hovered_action: TimelineAction = null

signal action_placed(action: TimelineAction)
signal action_executed(action: TimelineAction)
signal timeline_cleared
signal action_hovered_changed(action: TimelineAction, is_hovering: bool)

# ==========================================
# ★ 占位与校验逻辑
# ==========================================


# 检查一个具体的形状在指定原点是否合法（不越界、不重叠）
func is_placement_valid(shape_coords: Array[Vector2i], origin: Vector2i) -> bool:
	#GameLogger.debug("TimelineManager.is_placement_valid: shape_coords=%s, origin=%s, grid_size=%d x %d, 当前网格占用数=%d" % [
		#shape_coords, origin, GRID_WIDTH, GRID_HEIGHT, grid.size()
	#], "TimelineManager")
	
	# 验证origin本身是否在网格范围内（额外安全检查）
	if origin.x < 0 or origin.x >= GRID_WIDTH or origin.y < 0 or origin.y >= GRID_HEIGHT:
		#GameLogger.warning("is_placement_valid: origin=%s本身超出网格范围！GRID_WIDTH=%d, GRID_HEIGHT=%d" % [
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
			#GameLogger.warning("is_placement_valid: 形状X轴范围超出网格! origin.x=%d, 形状X范围[%d, %d], GRID_WIDTH=%d" % [
				#origin.x, min_x, max_x, GRID_WIDTH
			#], "TimelineManager")
			return false
		# 检查Y轴边界
		if origin.y + min_y < 0 or origin.y + max_y >= GRID_HEIGHT:
			#GameLogger.warning("is_placement_valid: 形状Y轴范围超出网格! origin.y=%d, 形状Y范围[%d, %d], GRID_HEIGHT=%d" % [
				#origin.y, min_y, max_y, GRID_HEIGHT
			#], "TimelineManager")
			return false
	
	for offset in shape_coords:
		var target_pos = origin + offset
		#GameLogger.debug("检查坐标: offset=%s, target_pos=%s, 计算: %s + %s" % [
			#offset, target_pos, origin, offset
		#], "TimelineManager")

		# 1. 越界检测
		if target_pos.x < 0 or target_pos.x >= GRID_WIDTH or target_pos.y < 0 or target_pos.y >= GRID_HEIGHT:
			#GameLogger.warning("越界检测失败: target_pos=%s超出网格范围 (GRID_WIDTH=%d, GRID_HEIGHT=%d)" % [
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
			#GameLogger.warning("碰撞检测失败: target_pos=%s已被占用，当前占用者: %s" % [
				#target_pos, occupant_info
			#], "TimelineManager")
			return false

	GameLogger.debug("放置验证通过: shape_coords=%s, origin=%s, 所有坐标在网格范围内且无碰撞" % [
		shape_coords, origin
	], "TimelineManager")
	return true


# 尝试放置一个行动到时间轴
func place_action(action: TimelineAction, origin: Vector2i) -> bool:
	#GameLogger.debug("place_action: 开始放置行动，origin=%s, type=%s" % [
		#origin, "PLAYER" if action.type == TimelineAction.Type.PLAYER else "ENEMY"
	#], "TimelineManager")
	
	if not is_placement_valid(action.shape_coords, origin):
		#GameLogger.warning("place_action: 验证失败，无法放置", "TimelineManager")
		return false

	action.origin_grid_pos = origin
	var abs_coords = action.get_absolute_coords()
	#GameLogger.debug("place_action: 绝对坐标=%s, 网格当前占用数=%d" % [
		#abs_coords, grid.size()
	#], "TimelineManager")

	# 占据所有网格点
	for coord in abs_coords:
		grid[coord] = action
		#GameLogger.debug("place_action: 占据坐标 %s" % coord, "TimelineManager")

	GameLogger.info("place_action: 行动放置成功！origin=%s, 行动ID=%s" % [
		origin, str(action.get_instance_id()) if is_instance_valid(action) else "无效"
	], "TimelineManager")
	action_placed.emit(action)
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
		GameLogger.info("💰 回合结算前：检测到 %d 个空位，已发放时间币" % empty_slots, "TimelineManager")

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
					
					GameLogger.debug("正在执行时间轴 [%d, %d] 的行动..." % [x, y], "TimelineManager")
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

	GameLogger.info("回合结算完毕，时代值即将 +1！", "TimelineManager")

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
		#GameLogger.warning("find_random_available_spot: 形状坐标数组为空", "TimelineManager")
		return Vector2i(-1, -1)
	
	var valid_spots: Array[Vector2i] = []
	
	#GameLogger.debug("find_random_available_spot: 寻找形状 %s 的可用位置，网格范围 %d x %d" % [
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
	#GameLogger.debug("形状范围: X[%d, %d](宽度=%d), Y[%d, %d](高度=%d)" % [
		#min_shape_x, max_shape_x, shape_width, min_shape_y, max_shape_y, shape_height
	#], "TimelineManager")
	
	# 网格适配：根据形状尺寸确定可能的原点范围
	# 时间轴网格宽度为12（0-11），原点x必须满足：0 ≤ x + min_shape_x 且 x + max_shape_x ≤ 11
	# 时间轴网格高度为3（0-2），原点y必须满足：0 ≤ y + min_shape_y 且 y + max_shape_y ≤ 2
	var min_valid_x = max(0, -min_shape_x)  # 确保 x + min_shape_x ≥ 0
	var max_valid_x = min(GRID_WIDTH - 1, GRID_WIDTH - 1 - max_shape_x)  # 确保 x + max_shape_x ≤ 11
	var min_valid_y = max(0, -min_shape_y)  # 确保 y + min_shape_y ≥ 0
	var max_valid_y = min(GRID_HEIGHT - 1, GRID_HEIGHT - 1 - max_shape_y)  # 确保 y + max_shape_y ≤ 2
	
	#GameLogger.debug("可能的原点范围: X[%d, %d], Y[%d, %d]" % [min_valid_x, max_valid_x, min_valid_y, max_valid_y], "TimelineManager")
	
	if min_valid_x > max_valid_x:
		#GameLogger.warning("find_random_available_spot: 形状宽度 %d 不适合网格宽度 %d" % [shape_width, GRID_WIDTH], "TimelineManager")
		return Vector2i(-1, -1)
	
	if min_valid_y > max_valid_y:
		#GameLogger.warning("find_random_available_spot: 形状高度 %d 不适合网格高度 %d" % [shape_height, GRID_HEIGHT], "TimelineManager")
		return Vector2i(-1, -1)

	# 遍历可能的网格位置（使用优化后的范围）
	for x in range(min_valid_x, max_valid_x + 1):
		for y in range(min_valid_y, max_valid_y + 1):
			var test_origin = Vector2i(x, y)
			#GameLogger.debug("测试原点: %s, 形状偏移量: %s" % [test_origin, shape_coords], "TimelineManager")
			# 如果这个位置能放下该形状，就存进备选库
			if is_placement_valid(shape_coords, test_origin):
				valid_spots.append(test_origin)
				#GameLogger.debug("原点 %s 验证通过" % test_origin, "TimelineManager")
			else:
				#GameLogger.debug("原点 %s 验证失败" % test_origin, "TimelineManager")
				pass

	#GameLogger.debug("find_random_available_spot: 找到 %d 个可用位置: %s" % [
		#valid_spots.size(), valid_spots
	#], "TimelineManager")
	
	# 如果有备选空位，使用 Godot 自带的强大 pick_random() 随机挑一个
	if valid_spots.size() > 0:
		var selected = valid_spots.pick_random()
		#GameLogger.debug("随机选择原点: %s" % selected, "TimelineManager")
		return selected

	#GameLogger.warning("find_random_available_spot: 未找到可用位置", "TimelineManager")
	return Vector2i(-1, -1)  # 彻底没位置了


# ==========================================
# ★ 敌方意图生成 (加入最大上限与调用敌人自身 AI)
# ==========================================
func generate_enemy_intents(enemies_on_board: Array):
	var current_intent_count = 0
	var hex_map = get_tree().current_scene.get_node_or_null("map/HexMap") if get_tree().current_scene else null

	# 为了防止每次生成的顺序固定，我们先把敌人列表打乱
	enemies_on_board.shuffle()

	for enemy in enemies_on_board:
		# 1. 检查是否达到了时间轴拥挤上限
		if current_intent_count >= max_enemy_intents_per_turn:
			#GameLogger.warning("⚠️ 时间轴已满载，不再生成额外敌方意图！", "TimelineManager")
			break

		if not is_instance_valid(enemy): continue

		# 2. 检查这个敌人有没有编写意图接口 (比如刚才写的 GrassEnemy)
		if enemy.has_method("get_intent_shape") and enemy.has_method("get_intent_action"):
			# 只有显式启用“敌人意图展示系统”的单位，才参与新意图链路。
			# 这样别的敌人实体即使还没填接口，也只会安全跳过。
			if enemy.has_method("is_intent_preview_enabled") and not enemy.is_intent_preview_enabled():
				continue
			# 如果当前回合没有合法目标，则时间轴阶段直接跳过它。
			# 但地图 hover 侧依然可以由展示控制器显示“无可用目标”的灰态提示。
			if is_instance_valid(hex_map) and enemy.has_method("can_generate_intent") and not enemy.can_generate_intent(hex_map):
				GameLogger.debug("敌人当前无合法目标，跳过时间轴意图生成: %s" % enemy.name, "TimelineManager")
				continue

			GameLogger.info("生成敌人意图: 敌人=%s, 类型=%s" % [enemy.name, enemy.get_class()], "TimelineManager")
			var shape = enemy.get_intent_shape()
			GameLogger.info("敌人形状: " + str(shape), "TimelineManager")

			# 调用新的随机寻位算法
			var spot = find_random_available_spot(shape)

			if spot != Vector2i(-1, -1):
				var target_tile = null
				# 将地图上的“目标中心格”真正映射成 stack，确保时间轴意图与地图侧目标一致。
				if is_instance_valid(hex_map) and enemy.has_method("get_intent_target_center_coord"):
					var target_coord = enemy.get_intent_target_center_coord(hex_map)
					if target_coord != null and hex_map.stack_nodes.has(target_coord):
						target_tile = hex_map.stack_nodes[target_coord]

				# 呼叫敌人自身，生成完整的 TimelineAction 数据
				var action = enemy.get_intent_action(target_tile)

				# 写入时间轴！
				place_action(action, spot)
				current_intent_count += 1
				#GameLogger.info("👾 敌人 %s 成功在随机位置 %s 部署了意图！" % [enemy.name, spot], "TimelineManager")
			else:
				#GameLogger.warning("⚠️ 找不到能放下形状 %s 的空位了！" % shape, "TimelineManager")
				pass


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
		if timeline_ui and timeline_ui.has_method("animate_enemy_intent_removal"):
			timeline_ui.animate_enemy_intent_removal(action)

		for coord in action.get_absolute_coords():
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
	#GameLogger.info("清除时间轴网格，当前占用数: " + str(grid.size()), "TimelineManager")
	grid.clear()
	hovered_action = null
	timeline_cleared.emit()

## 调试函数：打印网格状态
func debug_print_grid() -> void:
	GameLogger.info("=== 时间轴网格状态 ===", "TimelineManager")
	GameLogger.info("网格占用总数: " + str(grid.size()), "TimelineManager")
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
		GameLogger.info(info, "TimelineManager")
	GameLogger.info("=== 网格状态结束 ===", "TimelineManager")
