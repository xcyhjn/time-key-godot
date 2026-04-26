extends Control

const ENEMY_INTENT_TIMELINE_SHADER: Shader = preload("res://shaders/enemy_intent_timeline_pulse.gdshader")

@export_group("Grid Settings")
@export var slot_size: float = 40.0  # 格子大小，应与DragShapeController的slot_size一致
@export var spacing: float = 2.0  # 格子间距，应与DragShapeController的spacing一致
@export var grid_width: int = 12
@export var grid_height: int = 3

@export_group("Layout Settings")
@export var margin_top_preset: float = 0.0      # 紧贴屏幕最上沿的距离 (设为0即死死贴住)
@export var expanded_scale: Vector2 = Vector2(1.5, 1.5)
@export var anim_duration: float = 0.3

@export_group("敌人意图表现")
## 时间轴敌人意图格子使用的独立 pulse shader。若不手动指定，运行时自动回退到默认 shader。
@export var enemy_intent_timeline_shader: Shader
## 当前敌人意图组 hover 时的整体缩放值。应当只做轻微放大，不要产生明显位移感。
@export var enemy_intent_preview_scale: Vector2 = Vector2(1.08, 1.08)
## 敌人意图失效时，暗淡消失前缩小到的比例。
@export var enemy_intent_removal_scale: Vector2 = Vector2(0.92, 0.92)
## 敌人意图失效时，暗淡消失动画时长。
@export var enemy_intent_removal_duration: float = 0.28
## 当前展示中的敌人意图容器的层级，避免被普通方块覆盖。
@export var enemy_intent_preview_z_index: int = 50
## 时间轴敌人意图脉冲 shader 的速度。
@export var enemy_intent_pulse_speed: float = 3.0
## 时间轴敌人意图脉冲 shader 的最小透明度。
@export var enemy_intent_pulse_min_alpha: float = 0.15
## 时间轴敌人意图脉冲 shader 的最大透明度。
@export var enemy_intent_pulse_max_alpha: float = 0.75

@export_group("卡牌遮罩设置")
@export var mask_color: Color = Color(0.75, 0.75, 0.75, 0.6)  # 浅灰色半透明遮罩
@export var mask_layer: int = 100  # 遮罩层级，低于时间轴但高于其他内容

@onready var grid_background = $GridBackground
@onready var shape_layer = $ShapeLayer
var background_mask: ColorRect  # 遮罩节点（运行时创建）


var is_expanded: bool = false
var hovered_action: TimelineAction = null
var grid_cells: Dictionary = { }  # 存储网格单元引用，键：Vector2i，值：ColorRect
var allow_click_to_expand: bool = false  # 是否允许通过点击缩放时间轴（常态下禁用，仅卡牌选中时启用）
var action_containers: Dictionary = {}
var current_enemy_intent_preview_action: TimelineAction = null
var enemy_intent_preview_tween: Tween = null

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
	
	return null


## 应用调试用的offset设置
func _apply_anchor_layout() -> void:
	
	# 1. 强制使用 Godot 原生预设：顶部居中
	set_anchors_preset(Control.PRESET_CENTER_TOP, true)
	
	# 2. 动态计算网格的【真实物理宽度】和【真实物理高度】
	var actual_width = (grid_width * slot_size) + ((grid_width - 1) * spacing)
	var actual_height = (grid_height * slot_size) + ((grid_height - 1) * spacing)
	
	# 3. 设置精确的偏移量（完美贴身包裹，彻底解决偏左问题）
	offset_left = -actual_width / 2.0
	offset_right = actual_width / 2.0
	offset_top = margin_top_preset  # 紧贴屏幕顶部
	offset_bottom = margin_top_preset + actual_height
	
	# 4. 强制底层立刻刷新布局，防止 size 计算滞后
	force_update_transform()
	
	# 5. 精确设置缩放中心为【自身顶部正中心】
	pivot_offset = Vector2(actual_width / 2.0, 0.0)
	
	# 6. 确保内部网格背景贴死左上角 (消除内部误差)
	if is_instance_valid(grid_background):
		grid_background.position = Vector2.ZERO
		grid_background.size = size
	


func _ready():
	if enemy_intent_timeline_shader == null:
		enemy_intent_timeline_shader = ENEMY_INTENT_TIMELINE_SHADER

	# 应用响应式锚点布局
	_apply_anchor_layout()
	shape_layer.mouse_filter = Control.MOUSE_FILTER_PASS

	# 设置时间轴层级，确保在卡牌之下但在其他UI之上
	z_index = 100

	# 创建遮罩节点（如果不存在）
	_create_background_mask()

	_init_background_grid()
	# 关键修复：
	# - 敌人/玩家的时间轴 action 方块必须位于 GridBackground 之上，
	#   否则鼠标 hover 会先命中底层网格单元，导致 action 方块无法正确回调。
	# - 这里显式把 ShapeLayer 提到最上层。
	move_child(shape_layer, get_child_count() - 1)
	
	# 自动查找TimelineManager（如果未手动设置）
	if not timeline_manager:
		timeline_manager = _find_timeline_manager()
		if timeline_manager:
			pass
		else:
			pass

	# 连接TimelineManager信号以更新时间轴显示
	if timeline_manager:
		timeline_manager.action_placed.connect(_on_action_placed)
		timeline_manager.timeline_cleared.connect(_on_timeline_cleared)
	
	# 初始化时间轴点击缩放权限为禁用状态
	set_allow_click_to_expand(false)


## 创建背景遮罩（参考RewardManager的BackgroundMask）
func _create_background_mask() -> void:
	# 如果已经存在遮罩节点，直接返回
	if background_mask:
		return

	# 创建遮罩节点
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
	shape_container.set_meta("action_id", action.get_instance_id())
	action_containers[action.get_instance_id()] = shape_container

	var min_x = 0
	var max_x = 0
	var min_y = 0
	var max_y = 0
	if not action.shape_coords.is_empty():
		min_x = action.shape_coords[0].x
		max_x = action.shape_coords[0].x
		min_y = action.shape_coords[0].y
		max_y = action.shape_coords[0].y
		for offset in action.shape_coords:
			min_x = min(min_x, offset.x)
			max_x = max(max_x, offset.x)
			min_y = min(min_y, offset.y)
			max_y = max(max_y, offset.y)

	var cell_span_x = slot_size + spacing
	var cell_span_y = slot_size + spacing
	var width = (max_x - min_x + 1) * slot_size + max(0, max_x - min_x) * spacing
	var height = (max_y - min_y + 1) * slot_size + max(0, max_y - min_y) * spacing
	shape_container.size = Vector2(width, height)
	shape_container.pivot_offset = shape_container.size * 0.5
	shape_container.position = Vector2(
		(action.origin_grid_pos.x + min_x) * cell_span_x,
		(action.origin_grid_pos.y + min_y) * cell_span_y
	)

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

		# 计算相对于 shape_container 左上角的局部像素位置
		var pos_x = (target_grid_pos.x - (action.origin_grid_pos.x + min_x)) * cell_span_x
		var pos_y = (target_grid_pos.y - (action.origin_grid_pos.y + min_y)) * cell_span_y
		block.position = Vector2(pos_x, pos_y)

		# ★ 给每个小块加上鼠标检测区
		block.mouse_filter = Control.MOUSE_FILTER_PASS
		block.mouse_entered.connect(_on_block_hovered.bind(action))
		block.mouse_exited.connect(_on_block_exited)

		var overlay = ColorRect.new()
		overlay.name = "EnemyIntentOverlay"
		overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
		overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
		overlay.visible = false
		overlay.color = Color.WHITE
		if action.type == TimelineAction.Type.ENEMY:
			var overlay_material = ShaderMaterial.new()
			overlay_material.shader = enemy_intent_timeline_shader
			overlay_material.set_shader_parameter("pulse_speed", enemy_intent_pulse_speed)
			overlay_material.set_shader_parameter("min_alpha", enemy_intent_pulse_min_alpha)
			overlay_material.set_shader_parameter("max_alpha", enemy_intent_pulse_max_alpha)
			overlay.material = overlay_material
		block.add_child(overlay)

		shape_container.add_child(block)

		# 添加放置动画：从透明逐渐变为action.color
		_animate_block_placement(block, action.color)


func _on_timeline_cleared():
	clear_enemy_intent_preview()
	action_containers.clear()
	for child in shape_layer.get_children():
		child.queue_free()


# ==========================================
# ★ 交互逻辑 (需求 3)
# ==========================================
func _gui_input(event):
	# 点击时间轴放大并居中
	# 常态下禁用点击缩放，只有当选中卡牌后才允许缩放
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if allow_click_to_expand:
			toggle_expand()
		else:
			pass


func toggle_expand():
	is_expanded = !is_expanded
	var tw = create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	
	# 控制遮罩显示/隐藏 (逻辑不变)
	if background_mask:
		if is_expanded:
			background_mask.visible = true
			background_mask.modulate = Color.TRANSPARENT
			tw.parallel().tween_property(background_mask, "modulate", Color.WHITE, anim_duration * 0.5)
		else:
			tw.parallel().tween_property(background_mask, "modulate", Color.TRANSPARENT, anim_duration * 0.5)
			tw.tween_callback(func():
				if background_mask and not is_expanded:
					background_mask.visible = false
			)
	
	# 控制地图交互 (逻辑不变)
	var map_node = get_tree().root.find_child("map", true, false)
	if map_node and map_node is Control:
		map_node.mouse_filter = Control.MOUSE_FILTER_IGNORE if is_expanded else Control.MOUSE_FILTER_PASS
	
	# ★ 核心动画：只需改变 scale，位置交给 Anchor 和 Pivot 自动管理！
	if is_expanded:
		tw.parallel().tween_property(self, "scale", expanded_scale, anim_duration)
	else:
		tw.parallel().tween_property(self, "scale", Vector2.ONE, anim_duration)
		# 动画结束后重新确认布局
		tw.tween_callback(func():
			_apply_anchor_layout()
		)


func _on_block_hovered(action: TimelineAction):
	hovered_action = action
	# ★ 修复：通过TimelineManager发射高亮信号
	
	# 通过TimelineManager通知主场景高亮
	if timeline_manager and timeline_manager.has_signal("action_hovered_changed"):
		timeline_manager.action_hovered_changed.emit(action, true)
	else:
		pass
	
	# 保留原有的类型判断（可选，实际高亮逻辑在主场景中实现）
	if action.type == TimelineAction.Type.ENEMY:
		# 敌人行动：红色高亮
		pass
	elif action.type == TimelineAction.Type.PLAYER:
		# 玩家卡牌：金色高亮
		pass


func _on_block_exited():
	if hovered_action != null:
		# ★ 修复：通知主场景清除高亮
		
		# 通过TimelineManager通知主场景清除高亮
		if timeline_manager and timeline_manager.has_signal("action_hovered_changed"):
			timeline_manager.action_hovered_changed.emit(hovered_action, false)
		else:
			pass
		
		hovered_action = null


## 敌人意图时间轴预览入口
## 作用:
## - 当地图或时间轴 hover 到某个敌人意图时，让这一整组占位格：
##   1. 一起轻微放大
##   2. 一起显示独立 pulse shader
## - 其他敌人意图保持不动。
func show_enemy_intent_preview(action: TimelineAction, is_valid: bool, pulse_color: Color) -> void:
	if not is_instance_valid(action):
		return

	var action_id = action.get_instance_id()
	if not action_containers.has(action_id):
		return

	if current_enemy_intent_preview_action == action:
		return

	clear_enemy_intent_preview()
	current_enemy_intent_preview_action = action

	var container = action_containers[action_id]
	if not is_instance_valid(container):
		return

	container.pivot_offset = container.size * 0.5
	container.modulate = Color.WHITE
	container.scale = enemy_intent_preview_scale
	container.z_index = enemy_intent_preview_z_index
	_set_enemy_intent_overlay_visible(container, true, pulse_color)


## 清除当前时间轴上正在展示的敌人意图预览。
func clear_enemy_intent_preview() -> void:
	if enemy_intent_preview_tween != null and enemy_intent_preview_tween.is_valid():
		enemy_intent_preview_tween.kill()
		enemy_intent_preview_tween = null

	if current_enemy_intent_preview_action == null:
		return

	var action_id = current_enemy_intent_preview_action.get_instance_id()
	if action_containers.has(action_id):
		var container = action_containers[action_id]
		if is_instance_valid(container):
			container.modulate = Color.WHITE
			container.scale = Vector2.ONE
			container.z_index = 0
			_set_enemy_intent_overlay_visible(container, false, current_enemy_intent_preview_action.color)

	current_enemy_intent_preview_action = null


## 当敌人意图在回合中途失效时，播放“暗淡 -> 消失”动画并移除容器。
func animate_enemy_intent_removal(action: TimelineAction) -> void:
	if not is_instance_valid(action):
		return

	var action_id = action.get_instance_id()
	if not action_containers.has(action_id):
		return

	var container = action_containers[action_id]
	if not is_instance_valid(container):
		action_containers.erase(action_id)
		return

	if current_enemy_intent_preview_action == action:
		clear_enemy_intent_preview()

	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN)
	tw.tween_property(container, "modulate", Color(0.35, 0.35, 0.35, 0.0), enemy_intent_removal_duration)
	tw.parallel().tween_property(container, "scale", enemy_intent_removal_scale, enemy_intent_removal_duration)
	tw.tween_callback(func():
		if is_instance_valid(container):
			container.queue_free()
		action_containers.erase(action_id)
	)


## 对同一个敌人意图容器中的所有格子 overlay 统一设置显示状态与脉冲颜色。
func _set_enemy_intent_overlay_visible(container: Control, visible: bool, color: Color) -> void:
	for block in container.get_children():
		if block is Panel:
			var overlay = block.get_node_or_null("EnemyIntentOverlay")
			if overlay and overlay is ColorRect:
				overlay.visible = visible
				if visible and overlay.material:
					overlay.material.set_shader_parameter("pulse_color", color)


# ==========================================
# ★ 网格单元交互处理
# ==========================================
func _on_grid_cell_gui_input(event: InputEvent, cell_index: int) -> void:
	if event is InputEventMouseButton and event.pressed:
		var grid_x = cell_index % grid_width
		var grid_y = cell_index / grid_width
		var grid_pos = Vector2i(grid_x, grid_y)


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
		return
	
	is_expanded = false
	var tw = create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	
	if background_mask and background_mask.visible:
		tw.parallel().tween_property(background_mask, "modulate", Color.TRANSPARENT, anim_duration * 0.5)
		tw.tween_callback(func():
			if background_mask and not is_expanded:
				background_mask.visible = false
		)
	
	# 仅缩放回归即可
	tw.parallel().tween_property(self, "scale", Vector2.ONE, anim_duration)
	
	tw.tween_callback(func():
		_apply_anchor_layout()
	)


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
	

## 设置是否允许点击缩放时间轴
## 常态下禁用，仅当卡牌选中准备放置时启用
func set_allow_click_to_expand(allow: bool) -> void:
	allow_click_to_expand = allow
	# 关键修复：
	# - 这里不再通过把 TimelineUI 整体设为 IGNORE 来禁用点击缩放。
	# - 因为那样会让时间轴上的敌人意图方块也收不到 hover，导致无法联动回地图。
	# - 现在统一保持 PASS，由 _gui_input 自己判断 allow_click_to_expand 决定是否处理点击。
	mouse_filter = Control.MOUSE_FILTER_PASS
