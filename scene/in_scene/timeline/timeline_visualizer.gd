extends Node2D

# 时间轴可视化器
# 负责卡牌shape的实时可视化预览
# 与 DragShapeController 协同工作，提供拖拽时的形状预览和放置反馈

class_name TimelineVisualizer

@export_group("预览设置")
@export var preview_block_size: float = 40.0  # 预览方块大小
@export var preview_block_spacing: float = 2.0  # 预览方块间距
@export var valid_preview_color: Color = Color(0.2, 0.8, 0.2, 0.6)  # 有效位置颜色
@export var invalid_preview_color: Color = Color(0.8, 0.2, 0.2, 0.6)  # 无效位置颜色
@export var preview_animation_speed: float = 0.2  # 预览动画速度

@export_group("节点引用")
@export var timeline_ui_path: NodePath  # TimelineUI 节点路径
@export var drag_shape_controller_path: NodePath  # DragShapeController 节点路径

# 节点引用（自动获取）
@onready var timeline_ui: Control = get_node(timeline_ui_path) if not timeline_ui_path.is_empty() else null
@onready var drag_shape_controller: Node = get_node(drag_shape_controller_path) if not drag_shape_controller_path.is_empty() else null

# 预览状态
var is_showing_preview: bool = false  # 是否正在显示预览
var current_preview_blocks: Array[Control] = []  # 当前预览方块列表
var current_shape_coords: Array[Vector2i] = []  # 当前形状坐标
var current_grid_pos: Vector2i = Vector2i.ZERO  # 当前网格位置
var is_current_position_valid: bool = false  # 当前位置是否有效

# 信号定义
signal preview_shown(shape_coords: Array[Vector2i], grid_pos: Vector2i)  # 预览显示
signal preview_hidden()  # 预览隐藏
signal preview_position_changed(grid_pos: Vector2i, is_valid: bool)  # 预览位置变化
signal preview_placed(grid_pos: Vector2i)  # 预览放置（用户确认）

# ==========================================
# ★ 核心功能：预览显示与控制
# ==========================================


## 显示形状预览
func show_shape_preview(shape_coords: Array[Vector2i], initial_grid_pos: Vector2i = Vector2i.ZERO) -> void:
	if is_showing_preview:
		hide_shape_preview()

	if shape_coords.is_empty():
		GameLogger.warning("形状坐标为空，无法显示预览", "TimelineVisualizer")
		return

	GameLogger.debug("显示形状预览，坐标数量: %s" % shape_coords.size(), "TimelineVisualizer")

	# 保存状态
	current_shape_coords = shape_coords.duplicate()
	current_grid_pos = initial_grid_pos
	is_showing_preview = true

	# 创建预览方块
	_create_preview_blocks()

	# 更新预览位置
	_update_preview_position(initial_grid_pos)

	# 发射信号
	preview_shown.emit(shape_coords, initial_grid_pos)


## 隐藏形状预览
func hide_shape_preview() -> void:
	if not is_showing_preview:
		return

	GameLogger.debug("隐藏形状预览", "TimelineVisualizer")

	# 清除所有预览方块（已禁用预览，但保留清理逻辑）
	_clear_preview_blocks()

	# 重置状态
	current_shape_coords.clear()
	current_grid_pos = Vector2i.ZERO
	is_showing_preview = false
	is_current_position_valid = false

	# 发射信号
	preview_hidden.emit()


## 更新预览位置
func update_preview_position(grid_pos: Vector2i, is_valid: bool = true) -> void:
	if not is_showing_preview:
		return

	# 检查位置是否变化
	if grid_pos == current_grid_pos and is_valid == is_current_position_valid:
		return

	GameLogger.debug("更新预览位置: %s, 有效: %s" % [grid_pos, is_valid], "TimelineVisualizer")

	# 更新状态
	current_grid_pos = grid_pos
	is_current_position_valid = is_valid

	# 更新预览方块位置和颜色
	_update_preview_position(grid_pos)

	# 发射信号
	preview_position_changed.emit(grid_pos, is_valid)


## 获取当前预览的绝对坐标
func get_current_preview_coords() -> Array[Vector2i]:
	var result: Array[Vector2i] = []

	for offset in current_shape_coords:
		result.append(current_grid_pos + offset)

	return result


## 检查当前位置是否有效（包装 TimelineManager 的功能）
func is_position_valid(grid_pos: Vector2i) -> bool:
	if not drag_shape_controller:
		return false

	# 通过 DragShapeController 访问 TimelineManager
	if drag_shape_controller.has_method("_is_placement_valid"):
		# 调用 DragShapeController 的检查方法
		return drag_shape_controller._is_placement_valid(grid_pos)

	# 备用方案：直接检查
	return true  # 默认返回有效，实际应由 TimelineManager 检查

# ==========================================
# ★ 预览方块创建与管理
# ==========================================


## 创建预览方块
func _create_preview_blocks() -> void:
	if not timeline_ui:
		GameLogger.error("未找到 TimelineUI 引用，无法创建预览方块", "TimelineVisualizer")
		return

	_clear_preview_blocks()

	# 创建预览方块容器（如果需要）
	if not has_node("PreviewContainer"):
		var container = Control.new()
		container.name = "PreviewContainer"
		add_child(container)

	var container = $PreviewContainer

	for offset in current_shape_coords:
		var block = _create_preview_block()
		container.add_child(block)
		current_preview_blocks.append(block)

	GameLogger.debug("创建 %s 个预览方块" % current_preview_blocks.size(), "TimelineVisualizer")


## 创建单个预览方块
func _create_preview_block() -> Control:
	var block = Control.new()
	block.custom_minimum_size = Vector2(preview_block_size, preview_block_size)

	# 创建样式
	var style = StyleBoxFlat.new()
	style.bg_color = valid_preview_color
	style.set_border_width_all(2)
	style.border_color = Color(1.0, 1.0, 1.0, 0.8)  # 白色边框

	block.add_theme_stylebox_override("panel", style)

	# 设置鼠标过滤为忽略，避免干扰拖拽
	block.mouse_filter = Control.MOUSE_FILTER_IGNORE

	return block


## 清除所有预览方块
func _clear_preview_blocks() -> void:
	for block in current_preview_blocks:
		if is_instance_valid(block):
			block.queue_free()

	current_preview_blocks.clear()

	# 清除容器
	if has_node("PreviewContainer"):
		var container = $PreviewContainer
		for child in container.get_children():
			child.queue_free()


## 更新预览方块位置和颜色
func _update_preview_position(grid_pos: Vector2i) -> void:
	if not timeline_ui or current_preview_blocks.is_empty():
		return

	# 获取时间轴网格背景的全局位置
	var grid_bg = timeline_ui.get_node("GridBackground") as Control
	if not grid_bg:
		GameLogger.warning("未找到 GridBackground 节点", "TimelineVisualizer")
		return

	var grid_global_pos = grid_bg.global_position

	# ★ 关键修复：设置预览容器的位置为网格背景的全局位置
	if has_node("PreviewContainer"):
		var container = $PreviewContainer
		container.global_position = grid_global_pos
		GameLogger.debug("设置预览容器位置: " + str(grid_global_pos), "TimelineVisualizer")

	# 更新每个预览方块的位置和颜色
	for i in range(current_shape_coords.size()):
		var offset = current_shape_coords[i]
		var target_grid_pos = grid_pos + offset

		# 计算像素位置（相对于网格背景）
		var pos_x = target_grid_pos.x * (preview_block_size + preview_block_spacing)
		var pos_y = target_grid_pos.y * (preview_block_size + preview_block_spacing)

		var block = current_preview_blocks[i]
		if is_instance_valid(block):
			# 设置位置（相对于预览容器，即网格背景）
			block.position = Vector2(pos_x, pos_y)
			GameLogger.debug("设置预览方块位置: 方块索引=" + str(i) + 
				", 网格坐标=" + str(target_grid_pos) + 
				", 像素位置=(" + str(pos_x) + ", " + str(pos_y) + ")", "TimelineVisualizer")

			# 更新颜色基于有效性
			var style = block.get_theme_stylebox("panel") as StyleBoxFlat
			if style:
				style.bg_color = valid_preview_color if is_current_position_valid else invalid_preview_color

				# 添加闪烁效果给无效位置
				if not is_current_position_valid:
					style.border_color = Color(1.0, 0.5, 0.5, 0.8 + sin(Time.get_ticks_msec() * 0.01) * 0.2)
				else:
					style.border_color = Color(1.0, 1.0, 1.0, 0.8)

# ==========================================
# ★ 与 DragShapeController 集成
# ==========================================


## 连接到 DragShapeController 的事件
func connect_to_drag_controller() -> void:
	if not drag_shape_controller:
		GameLogger.warning("未找到 DragShapeController，无法连接事件", "TimelineVisualizer")
		return

	# 监听拖拽开始事件
	if drag_shape_controller.has_signal("drag_started"):
		drag_shape_controller.drag_started.connect(_on_drag_started)

	# 监听拖拽结束事件
	if drag_shape_controller.has_signal("drag_ended"):
		drag_shape_controller.drag_ended.connect(_on_drag_ended)

	# 监听悬停位置变化事件
	if drag_shape_controller.has_signal("hover_position_changed"):
		drag_shape_controller.hover_position_changed.connect(_on_hover_position_changed)

	GameLogger.info("已连接到 DragShapeController", "TimelineVisualizer")


## 拖拽开始回调
func _on_drag_started(card: Control, shape_coords: Array[Vector2i]) -> void:
	GameLogger.debug("拖拽开始，显示形状预览", "TimelineVisualizer")
	show_shape_preview(shape_coords)


## 拖拽结束回调
func _on_drag_ended(card: Control, was_placed: bool) -> void:
	GameLogger.debug("拖拽结束，隐藏形状预览", "TimelineVisualizer")
	hide_shape_preview()


## 悬停位置变化回调
func _on_hover_position_changed(grid_pos: Vector2i, is_valid: bool) -> void:
	update_preview_position(grid_pos, is_valid)

# ==========================================
# ★ 生命周期方法
# ==========================================


func _ready() -> void:
	GameLogger.debug("时间轴可视化器已就绪", "TimelineVisualizer")

	# 自动查找节点引用（备用方案）
	if not timeline_ui and timeline_ui_path.is_empty():
		timeline_ui = get_tree().root.find_child("TimelineUI", true, false)

	if not drag_shape_controller and drag_shape_controller_path.is_empty():
		drag_shape_controller = get_parent().find_child("DragShapeController") if get_parent() else null

	# 连接到 DragShapeController
	connect_to_drag_controller()


func _process(delta: float) -> void:
	# 如果需要，可以在这里添加持续的视觉效果
	# 例如：预览方块的脉动效果
	pass

# ==========================================
# ★ 调试与工具方法
# ==========================================


## 调试：打印当前预览状态
func debug_print_status() -> void:
	GameLogger.info("=== 时间轴可视化器状态 ===", "TimelineVisualizer")
	GameLogger.info("正在显示预览: %s" % is_showing_preview, "TimelineVisualizer")
	GameLogger.info("形状坐标数量: %s" % current_shape_coords.size(), "TimelineVisualizer")
	GameLogger.info("当前网格位置: %s" % current_grid_pos, "TimelineVisualizer")
	GameLogger.info("当前位置有效: %s" % is_current_position_valid, "TimelineVisualizer")
	GameLogger.info("预览方块数量: %s" % current_preview_blocks.size(), "TimelineVisualizer")

	if is_showing_preview and not current_shape_coords.is_empty():
		var abs_coords = get_current_preview_coords()
