extends Control

@export_group("Grid Settings")
@export var slot_size: float = 40.0  # 格子大小，应与DragShapeController的slot_size一致
@export var spacing: float = 2.0  # 格子间距，应与DragShapeController的spacing一致
@export var grid_width: int = 12
@export var grid_height: int = 3

@export_group("Animation Settings")
@export var expanded_scale: Vector2 = Vector2(1.5, 1.5)
@export var expanded_offset_y: float = 150.0  # 放大后向下移动的距离
@export var expanded_offset_x: float = 0.0  # 放大后水平移动的距离
@export var anim_duration: float = 0.3

@export_group("Layout Settings")
@export var offset_left_debug: float = 490.0  # 左侧偏移，用于调试布局
@export var offset_top_debug: float = 0.0  # 顶部偏移，用于调试布局
@export var offset_right_debug: float = 1278.0  # 右侧偏移，用于调试布局
@export var offset_bottom_debug: float = 211.0  # 底部偏移，用于调试布局
@export var apply_offset_on_ready: bool = true  # 是否在_ready时应用offset设置

@export_group("卡牌遮罩设置")
@export var mask_color: Color = Color(0.75, 0.75, 0.75, 0.6)  # 浅灰色半透明遮罩
@export var mask_layer: int = 100  # 遮罩层级，低于时间轴但高于其他内容

@onready var grid_background = $GridBackground
@onready var shape_layer = $ShapeLayer
var background_mask: ColorRect  # 遮罩节点（运行时创建）

var original_pos: Vector2
var original_scale: Vector2
var is_expanded: bool = false
var hovered_action: TimelineAction = null
var grid_cells: Dictionary = { }  # 存储网格单元引用，键：Vector2i，值：ColorRect

# 信号定义
signal grid_cell_clicked(grid_pos: Vector2i, is_right_click: bool)
signal grid_cell_right_clicked(grid_pos: Vector2i, is_right_click: bool)
signal grid_cell_hovered(grid_pos: Vector2i, is_hovering: bool)

# 如果需要跨脚本调用，可以在这里注入 manager 引用
var timeline_manager: TimelineManager


## 查找TimelineManager节点
func _find_timeline_manager() -> TimelineManager:
	# 方法1：通过场景树查找
	var tree = get_tree()
	if tree:
		# 查找所有TimelineManager节点
		var managers = tree.get_nodes_in_group("TimelineManager")
		if not managers.is_empty():
			return managers[0] as TimelineManager
		
		# 通过节点名查找
		var found = tree.root.find_child("TimelineManager", true, false)
		if found:
			return found as TimelineManager
	
	# 方法2：通过父节点查找
	var parent = get_parent()
	while parent:
		if parent is TimelineManager:
			return parent as TimelineManager
		parent = parent.get_parent()
	
	# 方法3：通过全局查找
	for node in get_tree().get_nodes_in_group("managers"):
		if node is TimelineManager:
			return node as TimelineManager
	
	GameLogger.warning("未找到TimelineManager节点", "TimelineUI")
	return null


## 应用调试用的offset设置
func _apply_offset_settings() -> void:
	if not apply_offset_on_ready:
		GameLogger.debug("跳过offset设置应用（apply_offset_on_ready为false）", "TimelineUI")
		return
	
	# 应用offset设置到Control节点
	GameLogger.debug("应用offset调试设置: left=" + str(offset_left_debug) + 
		", top=" + str(offset_top_debug) + 
		", right=" + str(offset_right_debug) + 
		", bottom=" + str(offset_bottom_debug), "TimelineUI")
	
	# 设置offset属性
	offset_left = offset_left_debug
	offset_top = offset_top_debug
	offset_right = offset_right_debug
	offset_bottom = offset_bottom_debug
	
	# 验证布局是否有效
	if offset_bottom <= offset_top:
		GameLogger.warning("offset_bottom <= offset_top，控件高度可能为负或零！", "TimelineUI")
		GameLogger.warning("当前值: offset_top=" + str(offset_top) + ", offset_bottom=" + str(offset_bottom), "TimelineUI")
	
	if offset_right <= offset_left:
		GameLogger.warning("offset_right <= offset_left，控件宽度可能为负或零！", "TimelineUI")
		GameLogger.warning("当前值: offset_left=" + str(offset_left) + ", offset_right=" + str(offset_right), "TimelineUI")


func _ready():
	# 应用调试用的offset设置
	_apply_offset_settings()
	
	original_pos = position
	original_scale = scale

	# 设置时间轴层级，确保在卡牌之下但在其他UI之上
	z_index = 100

	# 创建遮罩节点（如果不存在）
	_create_background_mask()
	GameLogger.debug("遮罩创建完成，background_mask: " + str(background_mask), "TimelineUI")

	_init_background_grid()
	
	# 自动查找TimelineManager（如果未手动设置）
	if not timeline_manager:
		timeline_manager = _find_timeline_manager()
		if timeline_manager:
			GameLogger.debug("找到TimelineManager: " + timeline_manager.name, "TimelineUI")
		else:
			GameLogger.warning("未找到TimelineManager，高亮信号可能无法发送", "TimelineUI")

	# 连接TimelineManager信号以更新时间轴显示
	if timeline_manager:
		timeline_manager.action_placed.connect(_on_action_placed)
		timeline_manager.timeline_cleared.connect(_on_timeline_cleared)


## 创建背景遮罩（参考RewardManager的BackgroundMask）
func _create_background_mask() -> void:
	# 如果已经存在遮罩节点，直接返回
	if background_mask:
		return

	# 创建遮罩节点
	GameLogger.debug("开始创建遮罩节点", "TimelineUI")
	background_mask = ColorRect.new()
	background_mask.name = "BackgroundMask"

	# 使用预设全屏锚点（自动填充全屏）
	background_mask.anchors_preset = Control.PRESET_FULL_RECT
	background_mask.grow_horizontal = Control.GROW_DIRECTION_BOTH
	background_mask.grow_vertical = Control.GROW_DIRECTION_BOTH

	background_mask.color = mask_color
	background_mask.z_index = mask_layer  # 设置遮罩层级
	background_mask.mouse_filter = Control.MOUSE_FILTER_IGNORE  # 遮罩不拦截鼠标事件
	background_mask.visible = false  # 初始隐藏

	# 添加到场景中（作为第一个子节点，位于最底层）
	add_child(background_mask)
	move_child(background_mask, 0)

	GameLogger.debug("时间轴遮罩已创建（使用全屏锚点），颜色: " + str(mask_color) + "，层级: " + str(mask_layer) + "，可见: " + str(background_mask.visible), "TimelineUI")


func _init_background_grid():
	grid_background.columns = grid_width  # 如果用 GridContainer，设置列数为12
	grid_background.add_theme_constant_override("h_separation", spacing)
	grid_background.add_theme_constant_override("v_separation", spacing)

	grid_cells.clear()

	for i in range(grid_width * grid_height):
		var slot = Panel.new()
		slot.custom_minimum_size = Vector2(slot_size, slot_size)
		
		# 创建默认样式：半透明深灰色背景，无边框
		var default_style = StyleBoxFlat.new()
		default_style.bg_color = Color(0.2, 0.2, 0.2, 0.5)
		default_style.set_border_width_all(0)
		slot.add_theme_stylebox_override("panel", default_style)

		# 启用鼠标交互
		slot.mouse_filter = Control.MOUSE_FILTER_PASS

		# 连接鼠标事件
		slot.gui_input.connect(_on_grid_cell_gui_input.bind(i))
		slot.mouse_entered.connect(_on_grid_cell_mouse_entered.bind(i))
		slot.mouse_exited.connect(_on_grid_cell_mouse_exited.bind(i))

		grid_background.add_child(slot)

		# 计算并存储网格坐标
		var grid_x = i % grid_width
		var grid_y = i / grid_width
		var grid_pos = Vector2i(grid_x, grid_y)
		grid_cells[grid_pos] = slot


# ==========================================
# ★ UI 绘制 (需求 2: 独立方格且有黑色描边)
# ==========================================
func _on_action_placed(action: TimelineAction):
	var shape_container = Control.new()
	shape_layer.add_child(shape_container)

	# 将这个容器与具体的 Action 绑定，方便悬浮检测
	shape_container.set_meta("action_ref", action)

	# 为所有方块设置统一的黑框样式
	var style = StyleBoxFlat.new()
	style.bg_color = action.color
	style.set_border_width_all(2)
	style.border_color = Color.BLACK  # ★ 黑色描边

	for offset in action.shape_coords:
		var target_grid_pos = action.origin_grid_pos + offset
		var block = Panel.new()
		block.add_theme_stylebox_override("panel", style)
		block.custom_minimum_size = Vector2(slot_size, slot_size)

		# 计算绝对像素位置
		var pos_x = target_grid_pos.x * (slot_size + spacing)
		var pos_y = target_grid_pos.y * (slot_size + spacing)
		block.position = Vector2(pos_x, pos_y)

		# ★ 给每个小块加上鼠标检测区
		block.mouse_filter = Control.MOUSE_FILTER_PASS
		block.mouse_entered.connect(_on_block_hovered.bind(action))
		block.mouse_exited.connect(_on_block_exited)

		shape_container.add_child(block)

		# 添加放置动画：从透明逐渐变为action.color
		_animate_block_placement(block, action.color)


func _on_timeline_cleared():
	for child in shape_layer.get_children():
		child.queue_free()


# ==========================================
# ★ 交互逻辑 (需求 3)
# ==========================================
func _gui_input(event):
	# 点击时间轴放大并居中
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		toggle_expand()


func toggle_expand():
	is_expanded = !is_expanded
	var tw = create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

	# 控制遮罩显示/隐藏
	GameLogger.debug("toggle_expand: is_expanded=" + str(is_expanded) + ", background_mask=" + str(background_mask), "TimelineUI")
	if background_mask:
		GameLogger.debug("遮罩控制: 当前visible=" + str(background_mask.visible) + ", modulate=" + str(background_mask.modulate), "TimelineUI")
		if is_expanded:
			background_mask.visible = true
			GameLogger.debug("遮罩显示，开始淡入动画", "TimelineUI")
			# 遮罩淡入动画
			background_mask.modulate = Color.TRANSPARENT
			tw.parallel().tween_property(background_mask, "modulate", Color.WHITE, anim_duration * 0.5)
		else:
			GameLogger.debug("遮罩隐藏，开始淡出动画", "TimelineUI")
			# 遮罩淡出动画后隐藏
			tw.parallel().tween_property(background_mask, "modulate", Color.TRANSPARENT, anim_duration * 0.5)
			tw.tween_callback(func():
				if background_mask and not is_expanded:
					background_mask.visible = false
					GameLogger.debug("遮罩动画完成，设置visible=false", "TimelineUI")
			)

	# 控制地图交互：时间轴展开时禁用地图交互
	var map_node = get_tree().root.find_child("map", true, false)
	if map_node and map_node is Control:
		if is_expanded:
			# 时间轴展开时，禁用地图交互
			map_node.mouse_filter = Control.MOUSE_FILTER_IGNORE
			GameLogger.debug("时间轴展开，已禁用地图交互", "TimelineUI")
		else:
			# 时间轴收起时，恢复地图交互
			map_node.mouse_filter = Control.MOUSE_FILTER_STOP
			GameLogger.debug("时间轴收起，已恢复地图交互", "TimelineUI")

	if is_expanded:
		tw.tween_property(self, "scale", expanded_scale, anim_duration)
		tw.parallel().tween_property(self, "position", original_pos + Vector2(expanded_offset_x, expanded_offset_y), anim_duration)
	else:
		tw.tween_property(self, "scale", original_scale, anim_duration)
		tw.parallel().tween_property(self, "position", original_pos, anim_duration)


func _on_block_hovered(action: TimelineAction):
	hovered_action = action
	# ★ 修复：通过TimelineManager发射高亮信号
	GameLogger.debug("悬浮在对象上！类型: " + str(action.type), "TimelineUI")
	
	# 通过TimelineManager通知主场景高亮
	if timeline_manager and timeline_manager.has_signal("action_hovered_changed"):
		timeline_manager.action_hovered_changed.emit(action, true)
	else:
		GameLogger.warning("无法发送高亮信号: timeline_manager无效或没有action_hovered_changed信号", "TimelineUI")
	
	# 保留原有的类型判断（可选，实际高亮逻辑在主场景中实现）
	if action.type == TimelineAction.Type.ENEMY:
		# 敌人行动：红色高亮
		GameLogger.debug("敌人意图高亮（红色）", "TimelineUI")
	elif action.type == TimelineAction.Type.PLAYER:
		# 玩家卡牌：金色高亮
		GameLogger.debug("玩家卡牌高亮（金色）", "TimelineUI")


func _on_block_exited():
	if hovered_action != null:
		# ★ 修复：通知主场景清除高亮
		GameLogger.debug("离开悬浮对象，清除高亮", "TimelineUI")
		
		# 通过TimelineManager通知主场景清除高亮
		if timeline_manager and timeline_manager.has_signal("action_hovered_changed"):
			timeline_manager.action_hovered_changed.emit(hovered_action, false)
		else:
			GameLogger.warning("无法发送清除高亮信号", "TimelineUI")
		
		hovered_action = null


# ==========================================
# ★ 网格单元交互处理
# ==========================================
func _on_grid_cell_gui_input(event: InputEvent, cell_index: int) -> void:
	if event is InputEventMouseButton and event.pressed:
		var grid_x = cell_index % grid_width
		var grid_y = cell_index / grid_width
		var grid_pos = Vector2i(grid_x, grid_y)

		GameLogger.debug("点击时间轴网格单元: " + str(grid_pos), "TimelineUI")

		# 发射信号，通知其他系统这个网格被点击了
		# 可以用于快速放置卡牌或显示信息
		if event.button_index == MOUSE_BUTTON_LEFT:
			# 左键点击：尝试放置卡牌或显示信息
			emit_signal("grid_cell_clicked", grid_pos, false)
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			# 右键点击：清除该位置的行动或显示详细
			emit_signal("grid_cell_right_clicked", grid_pos, true)


func _on_grid_cell_mouse_entered(cell_index: int) -> void:
	var grid_x = cell_index % grid_width
	var grid_y = cell_index / grid_width
	var grid_pos = Vector2i(grid_x, grid_y)

	# 高亮显示这个网格单元
	var cell = grid_cells.get(grid_pos)
	if cell and cell is Panel:
		# 使用StyleBox设置悬停颜色
		var style = cell.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
		if not style:
			style = StyleBoxFlat.new()
		style.bg_color = Color(0.3, 0.3, 0.3, 0.7)  # 悬停颜色
		style.set_border_width_all(0)
		cell.add_theme_stylebox_override("panel", style)

	GameLogger.debug("鼠标进入时间轴网格单元: " + str(grid_pos), "TimelineUI")
	emit_signal("grid_cell_hovered", grid_pos, true)


func _on_grid_cell_mouse_exited(cell_index: int) -> void:
	var grid_x = cell_index % grid_width
	var grid_y = cell_index / grid_width
	var grid_pos = Vector2i(grid_x, grid_y)

	# 恢复网格单元颜色
	var cell = grid_cells.get(grid_pos)
	if cell and cell is Panel:
		# 使用StyleBox恢复原始颜色
		var style = cell.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
		if not style:
			style = StyleBoxFlat.new()
		style.bg_color = Color(0.2, 0.2, 0.2, 0.5)  # 原始颜色
		style.set_border_width_all(0)
		cell.add_theme_stylebox_override("panel", style)

	GameLogger.debug("鼠标离开时间轴网格单元: " + str(grid_pos), "TimelineUI")
	emit_signal("grid_cell_hovered", grid_pos, false)


## 动画：时间轴方格放置后变蓝色效果
func _animate_block_placement(block: Panel, target_color: Color) -> void:
	if not is_instance_valid(block):
		return

	# 获取block的StyleBox并创建副本（避免修改共享资源）
	var original_style = block.get_theme_stylebox("panel") as StyleBoxFlat
	if not original_style:
		return

	# 创建独立副本
	var style = original_style.duplicate() as StyleBoxFlat
	block.add_theme_stylebox_override("panel", style)

	# 设置初始状态：透明
	var transparent_color = Color(target_color.r, target_color.g, target_color.b, 0.0)
	style.bg_color = transparent_color
	style.border_color = Color.BLACK

	# 创建补间动画：使用tween_property直接对bg_color进行补间
	var tw = create_tween().set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tw.tween_property(style, "bg_color", target_color, 0.5)

	# 添加轻微的缩放动画
	block.scale = Vector2(0.8, 0.8)
	tw.parallel().tween_property(block, "scale", Vector2.ONE, 0.3).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)


## 强制收起时间轴（用于卡牌放置后自动返回上方）
func collapse() -> void:
	if not is_expanded:
		return  # 已经收起，无需操作

	# 直接设置状态并执行收起动画
	is_expanded = false
	var tw = create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

	# 收起遮罩（如果存在）
	if background_mask and background_mask.visible:
		tw.parallel().tween_property(background_mask, "modulate", Color.TRANSPARENT, anim_duration * 0.5)
		tw.tween_callback(func():
			if background_mask and not is_expanded:
				background_mask.visible = false
		)

	# 收起时间轴
	tw.tween_property(self, "scale", original_scale, anim_duration)
	tw.parallel().tween_property(self, "position", original_pos, anim_duration)


## 更新网格预览：根据形状和位置显示蓝色/红色格子，并检测敌人意图
func update_grid_preview(shape_coords: Array[Vector2i], origin_pos: Vector2i, is_valid: bool) -> void:
	# 首先清除所有预览效果
	clear_grid_preview()
	
	# 如果没有形状数据，直接返回
	if shape_coords.is_empty():
		return
	
	# 计算形状覆盖的所有格子
	var covered_cells: Array[Vector2i] = []
	var all_in_bounds = true
	
	for coord in shape_coords:
		var cell_pos = Vector2i(origin_pos.x + coord.x, origin_pos.y + coord.y)
		covered_cells.append(cell_pos)
		
		# 检查是否在网格边界内
		if cell_pos.x < 0 or cell_pos.x >= grid_width or cell_pos.y < 0 or cell_pos.y >= grid_height:
			all_in_bounds = false
	
	# 检查是否有任何覆盖的单元格被敌人意图占据
	var overlaps_enemy_intent = false
	if timeline_manager:
		for cell_pos in covered_cells:
			if timeline_manager.grid.has(cell_pos):
				var action = timeline_manager.grid[cell_pos] as TimelineAction
				if action and action.type == TimelineAction.Type.ENEMY:
					overlaps_enemy_intent = true
					break
	
	# 确定预览样式
	var is_preview_valid = is_valid and all_in_bounds
	var is_enemy_overlap = overlaps_enemy_intent
	
	# 更新格子样式
	for cell_pos in covered_cells:
		# 检查是否在网格边界内
		if cell_pos.x >= 0 and cell_pos.x < grid_width and cell_pos.y >= 0 and cell_pos.y < grid_height:
			if cell_pos in grid_cells:
				var cell = grid_cells[cell_pos] as Panel
				if not cell:
					continue
				
				# 获取或创建样式
				var style = cell.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
				if not style:
					style = StyleBoxFlat.new()
				
				# 根据状态设置样式
				if is_preview_valid:
					# 有效位置：蓝色填充，无边框
					style.bg_color = Color.SKY_BLUE
					style.set_border_width_all(0)
				elif is_enemy_overlap:
					# 敌人意图重叠：红色网格状（红色边框，透明背景）
					style.bg_color = Color.TRANSPARENT
					style.set_border_width_all(2)
					style.border_color = Color.INDIAN_RED
				else:
					# 无效位置：红色填充，无边框
					style.bg_color = Color.INDIAN_RED
					style.set_border_width_all(0)
				
				cell.add_theme_stylebox_override("panel", style)
				
				# 添加轻微动画
				var tw = create_tween()
				cell.scale = Vector2(0.9, 0.9)
				tw.tween_property(cell, "scale", Vector2.ONE, 0.15)
		else:
			# 部分格子越界，整体标记为红色
			pass
	
	GameLogger.debug("更新网格预览: 形状=" + str(shape_coords) + ", 原点=" + str(origin_pos) + ", 有效=" + str(is_valid) + ", 全部在边界内=" + str(all_in_bounds) + ", 敌人重叠=" + str(overlaps_enemy_intent), "TimelineUI")


## 清除所有网格预览效果
func clear_grid_preview() -> void:
	for cell_pos in grid_cells.keys():
		var cell = grid_cells[cell_pos] as Panel
		if cell:
			# 恢复原始样式：半透明深灰色背景，无边框
			var style = cell.get_theme_stylebox("panel").duplicate() as StyleBoxFlat
			if not style:
				style = StyleBoxFlat.new()
			style.bg_color = Color(0.2, 0.2, 0.2, 0.5)
			style.set_border_width_all(0)
			cell.add_theme_stylebox_override("panel", style)
			cell.scale = Vector2.ONE


## 清除时间轴UI（用于回合结束）
func clear_ui() -> void:
	# 清除所有放置的行动块
	_on_timeline_cleared()
	
	# 清除网格预览效果
	clear_grid_preview()
	
	# 重置悬停状态
	hovered_action = null
	
	GameLogger.debug("时间轴UI已清空", "TimelineUI")
