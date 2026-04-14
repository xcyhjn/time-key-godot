class_name DragShapeController
extends Node2D
# 原文件名: DragShapeController(时间轴拖拽控制).gd
# 功能: 时间轴拖拽控制
## 拖拽形状控制器
## 处理卡牌拖拽到时间轴的交互，包括旋转、网格吸附和放置验证

# ==========================================
# 信号
# ==========================================
signal player_action_placed(action: TimelineAction)  ## 玩家成功放置行动后发射
signal drag_started(card: Control, shape_coords: Array[Vector2i])  ## 拖拽开始
signal drag_ended(card: Control, was_placed: bool)  ## 拖拽结束
signal hover_position_changed(grid_pos: Vector2i, is_valid: bool)  ## 悬停位置变化

# ==========================================
# 导出变量 - 时间轴设置
# ==========================================
@export_group("时间轴设置")
@export var slot_size: float = 80.0  ## 格子大小
@export var spacing: float = 4.0  ## 格子间距
@export var timeline_ui_path: NodePath  ## TimelineUI 节点路径

# ==========================================
# 导出变量 - 视觉效果
# ==========================================# 导出变量 - 视觉效果
@export_group("视觉效果")
@export var drag_shader: Shader  ## 拖拽时的着色器
@export var float_offset: Vector2 = Vector2(-15, -15)  ## 悬浮偏移量

# 导出变量 - 动画设置
@export_group("动画设置")
@export var timeline_lerp_speed: float = 1.0  ## 时间轴吸附移动速度 (0-1)，1.0为立即跟随
@export var free_drag_lerp_speed: float = 1.0  ## 自由拖拽移动速度 (0-1)，1.0为立即跟随
@export var rotation_animation_duration: float = 0.15  ## 旋转动画时长
@export var scale_animation_duration: float = 0.2  ## 缩放动画时长

# ==========================================
# 导出变量 - 节点引用
# ==========================================
@export_group("节点引用")
@export_node_path("RichTextLabel") var cursor_tooltip_path: NodePath  ## 光标提示标签路径（必须使用RichTextLabel）
@export_node_path("TimelineManager") var timeline_manager_path: NodePath  ## TimelineManager 节点路径

# ==========================================
# 状态变量
# ==========================================
var is_dragging: bool = false  ## 是否正在拖拽
var is_placing: bool = false  ## 是否正在放置中（放置期间禁用其他输入）
var current_card: Control = null  ## 当前拖拽的卡牌
var current_shape_coords: Array[Vector2i] = []  ## 当前形状坐标
var current_target_tile: Node = null  ## 暂存的目标地块
var drag_offset: Vector2 = Vector2.ZERO  ## 鼠标相对于卡牌中心的偏移

# ==========================================
# 节点引用
# ==========================================
var timeline_ui: Control
var timeline_manager: Node  ## TimelineManager 实例
var cursor_tooltip: RichTextLabel
var discard_pile: Node  ## 弃牌区引用

# ==========================================
# 工具函数
# ==========================================


## 获取卡牌管理器
func _get_card_manager() -> Node:
	# 直接查找CardManager节点
	var card_manager = get_node_or_null("../../CardManager")  # 从DragShapeController向上两级到project，查找CardManager子节点
	if card_manager:
		GameLogger.debug("找到CardManager节点", "DragShapeController")
		return card_manager
	
	# 备用方案：通过元数据查找
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		GameLogger.debug("通过树根元数据找到CardManager", "DragShapeController")
		return tree_root.get_meta("card_manager")
	
	# 回退到当前场景元数据
	var scene_root = get_tree().current_scene
	if scene_root and scene_root.has_meta("card_manager"):
		GameLogger.debug("通过场景元数据找到CardManager", "DragShapeController")
		return scene_root.get_meta("card_manager")

	GameLogger.warning("未找到CardManager", "DragShapeController")
	return null


## 查找弃牌区
func _find_discard_pile() -> void:
	"""查找弃牌区，支持多种查找方式，优先级：project节点属性 > 组查找 > 路径查找"""
	
	# 方案1：优先查找project节点的discard_pile属性
	var control_node = _find_project_node()
	if control_node:
		var pile = control_node.get("discard_pile")
		if pile != null and pile is Object and pile.has_method("move_cards"):
			discard_pile = pile
			GameLogger.debug("通过project.discard_pile属性找到弃牌区", "DragShapeController")
			return
		
		# 检查project节点是否有弃牌处理方法
		if control_node.has_method("_handle_discard_effects"):
			discard_pile = null  # 设置为null，表示使用project系统
			GameLogger.debug("project.gd有弃牌处理方法，将使用project的弃牌系统", "DragShapeController")
			return
	
	# 方案2：通过组查找弃牌区
	var discard_group_nodes = get_tree().get_nodes_in_group("DiscardPile")
	for node in discard_group_nodes:
		if node is Object and node.has_method("move_cards"):
			discard_pile = node
			GameLogger.debug("通过'DiscardPile'组找到弃牌区: " + str(node.get_path()), "DragShapeController")
			return
	
	# 方案3：直接路径查找
	var simple_paths = [
		"/root/project/DiscardPile",  # 绝对路径
		"../DiscardPile",  # 相对路径
		"DiscardPile"  # 当前节点下的DiscardPile
	]
	
	for path in simple_paths:
		var node = get_node_or_null(NodePath(path))
		if node and node is Object and node.has_method("move_cards"):
			discard_pile = node
			GameLogger.debug("通过路径找到弃牌区: " + path, "DragShapeController")
			return
	
	# 所有方法都失败
	if control_node:
		GameLogger.debug("project.gd存在但没有弃牌系统，将使用回退方案", "DragShapeController")
	else:
		GameLogger.warning("未找到弃牌区，卡牌放置后将被销毁而不是进入弃牌区", "DragShapeController")


## 获取HexMap管理器
func _get_hex_map() -> Node:
	# 直接定位HexMap节点，路径：从DragShapeController向上两级到project，再到map/HexMap
	var hex_map = get_node_or_null("../../map/HexMap")
	if hex_map:
		GameLogger.debug("找到HexMap节点", "DragShapeController")
		return hex_map
	
	# 备用方案：如果节点结构变化，尝试绝对路径
	hex_map = get_tree().root.get_node_or_null("/root/in_scene/map/HexMap")
	if hex_map:
		GameLogger.debug("通过绝对路径找到HexMap节点", "DragShapeController")
		return hex_map
	
	GameLogger.warning("未找到HexMap节点", "DragShapeController")
	return null

# ==========================================
# 生命周期
# ==========================================


func _ready() -> void:
	timeline_ui = get_node(timeline_ui_path)

	if not cursor_tooltip_path.is_empty():
		cursor_tooltip = get_node(cursor_tooltip_path)
		# 确保RichTextLabel正确配置BBCode
		if cursor_tooltip:
			cursor_tooltip.bbcode_enabled = true
			cursor_tooltip.fit_content = true
			cursor_tooltip.mouse_filter = Control.MOUSE_FILTER_IGNORE

	if not timeline_manager_path.is_empty():
		timeline_manager = get_node(timeline_manager_path)

	# 添加到DragShapeController组，方便其他脚本查找
	add_to_group("DragShapeController")
	GameLogger.debug("DragShapeController已添加到组，准备接收拖拽事件", "DragShapeController")

	# 延迟查找弃牌区（确保DiscardPile已创建并注册）
	call_deferred("_find_discard_pile")

	set_process_input(true)
	set_process(true)

# ==========================================
# 拖拽启动
# ==========================================


## 启动拖拽模式（在玩家点击合法地块后调用）
func start_dragging(card: Control, target_tile: Node) -> void:
	is_dragging = true
	current_card = card
	current_target_tile = target_tile

	# 从卡牌数据中获取初始形状
	var raw_shape = card.card_info.get("shape", [Vector2i(0, 0)])
	current_shape_coords = _convert_to_vector2i_array(raw_shape)
	GameLogger.debug("卡牌形状: " + str(current_shape_coords), "DragShapeController")

	# 发射拖拽开始信号（用于时间轴可视化器）
	drag_started.emit(card, current_shape_coords)

	# ★ 允许时间轴点击缩放（卡牌选中时）
	if timeline_ui and timeline_ui.has_method("set_allow_click_to_expand"):
		timeline_ui.set_allow_click_to_expand(true)
		GameLogger.debug("已启用时间轴点击缩放权限（卡牌选中中）", "DragShapeController")

	# 唤起时间轴
	if timeline_ui.has_method("toggle_expand"):
		timeline_ui.is_expanded = false
		timeline_ui.toggle_expand()

	# 禁用时间轴UI和地块容器的鼠标交互，防止点击被拦截
	if timeline_ui and timeline_ui is Control:
		timeline_ui.mouse_filter = Control.MOUSE_FILTER_IGNORE
	GameLogger.debug("已禁用时间轴UI鼠标交互", "DragShapeController")
	
	var hex_map = _get_hex_map()
	if hex_map and hex_map.has_method("set_tiles_interactive"):
		hex_map.set_tiles_interactive(false)
		GameLogger.debug("已禁用地块交互", "DragShapeController")
		# 锁定地块视觉状态，防止鼠标退出信号清除高亮和消融效果
		if hex_map.has_method("set_visuals_locked"):
			hex_map.set_visuals_locked(true)
	else:
		GameLogger.warning("未找到HexMap节点或缺少set_tiles_interactive方法", "DragShapeController")

	# 禁用网格单元格鼠标交互
	_disable_grid_cells_mouse_filter()

	# ★ 无副本架构：直接操作原卡牌
	# 1. 通知卡牌进入拖拽状态
	if card.has_method("enter_dragging_state"):
		card.enter_dragging_state()

	# 2. 将卡牌从手牌容器移除，成为场景根节点的子节点
	var original_parent = card.get_parent()
	if original_parent:
		original_parent.remove_child(card)

	var scene_root = get_tree().current_scene
	if scene_root:
		# ★ 关键修复：在重新父级化之前计算卡牌中心位置
		# 获取卡牌在当前父节点中的全局位置和大小
		var original_card_global_pos = card.global_position
		var original_card_size = card.get_size() if card.has_method("get_size") else Vector2.ZERO
		var original_card_scale = card.scale
		var original_card_center = original_card_global_pos + original_card_size * original_card_scale / 2
		
		# 调试：记录卡牌重新父级化前信息
		if original_parent:
			GameLogger.debug("卡牌父节点: " + original_parent.get_class() + " @ " + str(original_parent.global_position), "DragShapeController")
		
		GameLogger.debug("卡牌重新父级化前: 位置=" + str(original_card_global_pos) + ", 大小=" + str(original_card_size), "DragShapeController")
		
		# 重新父级化到场景根节点
		scene_root.add_child(card)
		card.set_as_top_level(true)
		
		# 获取当前鼠标位置
		var mouse_pos = get_global_mouse_position()
		
		# ★ 重要：验证重新父级化后卡牌全局位置是否变化
		var new_card_global_pos = card.global_position
		var new_card_position = card.position
		if new_card_global_pos != original_card_global_pos:
			GameLogger.warning("重新父级化后卡牌全局位置发生变化: 原位置=" + str(original_card_global_pos) + 
				", 新位置=" + str(new_card_global_pos), "DragShapeController")
			GameLogger.warning("卡牌position属性: 新position=" + str(new_card_position), "DragShapeController")
		
		# ★ 关键修复：重新计算卡牌中心（考虑set_as_top_level后的坐标变化）
		# 重新获取卡牌当前的实际全局位置和大小
		var actual_card_global_pos = card.global_position
		var actual_card_size = card.get_size() if card.has_method("get_size") else original_card_size
		
		# ★ 关键修复：计算目标缩放因子并在计算中心时使用
		# 这样drag_offset计算会考虑卡牌将要缩放的大小
		var scale_factor = slot_size / 20.0
		GameLogger.debug("卡牌缩放: 目标=" + str(scale_factor) + ", 当前=" + str(card.scale), "DragShapeController")
		
		# 使用目标缩放因子计算卡牌中心（而不是当前缩放）
		var actual_card_center = actual_card_global_pos + actual_card_size * Vector2(scale_factor, scale_factor) / 2
		GameLogger.debug("卡牌中心计算完成: " + str(actual_card_center), "DragShapeController")
		
		# 使用目标缩放计算的卡牌中心计算拖拽偏移
		drag_offset = mouse_pos - actual_card_center
		GameLogger.debug("拖拽偏移计算: " + str(drag_offset), "DragShapeController")

		# 3. 应用拖拽着色器
		if card.material and drag_shader:
			card.material = drag_shader.duplicate()

		# 4. 应用缩放动画（使用前面计算的scale_factor）
		var tw = create_tween()
		tw.tween_property(card, "scale", Vector2(scale_factor, scale_factor), scale_animation_duration)

	GameLogger.info("开始拖拽实体卡牌，已重新父级化到场景根节点", "DragShapeController")

## （已移除）创建拖拽克隆体 - 无副本架构不再需要此函数

# ==========================================
# 处理循环
# ==========================================


func _process(_delta: float) -> void:
	if not is_dragging or current_card == null:
		return

	var mouse_pos = get_global_mouse_position()
	var is_over_timeline = timeline_ui.get_global_rect().has_point(mouse_pos)

	if is_over_timeline:
		_handle_timeline_hover(mouse_pos)
	else:
		_handle_free_drag(mouse_pos)


## 处理时间轴悬停逻辑
## 计算鼠标在时间轴网格上的坐标，更新卡牌位置，验证放置有效性
## 更新时间轴网格预览（蓝色/红色高亮）
func _handle_timeline_hover(mouse_pos: Vector2) -> void:
	# ★ 修复缩放导致的鼠标网格吸附错位：使用 get_local_mouse_position() 自动剔除父级缩放变换
	var local_mouse = timeline_ui.grid_background.get_local_mouse_position()

	# 计算网格坐标 - 从timeline_ui获取实际的格子大小和间距，确保与UI设置一致
	var ui_slot_size = timeline_ui.slot_size if "slot_size" in timeline_ui else slot_size
	var ui_spacing = timeline_ui.spacing if "spacing" in timeline_ui else spacing
	var cell_size = ui_slot_size + ui_spacing
	var grid_x = int(local_mouse.x / cell_size)
	var grid_y = int(local_mouse.y / cell_size)
	var hover_grid_pos = Vector2i(grid_x, grid_y)
	
	# ★ 边界检查：确保网格坐标在合理范围内（包括负坐标保护）
	var grid_width = timeline_ui.grid_width if "grid_width" in timeline_ui else 12
	var grid_height = timeline_ui.grid_height if "grid_height" in timeline_ui else 3
	var is_in_grid_bounds = (grid_x >= 0 and grid_x < grid_width and grid_y >= 0 and grid_y < grid_height)
	# ★ 额外保护：防止在网格左侧或上方悬浮时产生负数数组越界
	if local_mouse.x < 0 or local_mouse.y < 0:
		is_in_grid_bounds = false
		GameLogger.debug("鼠标位于网格背景左侧或上方，标记为无效区域", "DragShapeController")
	
	# ★ 修复编译错误：声明未使用的变量（原网格吸附相关变量）
	# 变量已移除，网格吸附功能已取消

	# ★ 已修改：取消网格吸附，让卡牌直接跟随鼠标（根据用户要求）
	# 计算卡牌中心的目标位置（鼠标位置减去偏移量，保持自由拖拽行为）
	var target_center = mouse_pos - drag_offset
	
	# 时间轴悬停调试信息 - 详细版本
	GameLogger.debug("时间轴悬停调试: \n" +
		"  - 鼠标位置: " + str(mouse_pos) + "\n" +
		"  - 网格背景本地鼠标位置: " + str(local_mouse) + " (已自动剔除缩放变换)\n" +
		"  - 格子大小: " + str(cell_size) + " (ui_slot_size=" + str(ui_slot_size) + ", ui_spacing=" + str(ui_spacing) + ")" + "\n" +
		"  - 网格坐标: " + str(hover_grid_pos) + "\n" +
		"  - 网格边界检查: " + str(is_in_grid_bounds) + " (范围: X[0-" + str(grid_width-1) + "], Y[0-" + str(grid_height-1) + "])" + "\n" +
		"  - 拖拽偏移: " + str(drag_offset) + "\n" +
		"  - 目标中心: " + str(target_center) + "\n" +
		"  - 当前卡牌位置: " + str(current_card.global_position) if is_instance_valid(current_card) else "无效",
		"DragShapeController")
	
	# 将卡牌中心位置转换为左上角位置
	var card_top_left = target_center
	if current_card.has_method("get_size"):
		var card_size = current_card.get_size()
		var card_scale = current_card.scale
		card_top_left -= card_size * card_scale / 2
		GameLogger.debug("卡牌左上角计算: 大小=" + str(card_size) + ", 缩放=" + str(card_scale) + ", 左上角=" + str(card_top_left), "DragShapeController")

	
	current_card.global_position = card_top_left

	# 更新着色器状态
	var is_valid = true
	if timeline_manager and is_in_grid_bounds:
		is_valid = timeline_manager.is_placement_valid(current_shape_coords, hover_grid_pos)
	else:
		is_valid = false
		GameLogger.debug("网格坐标超出边界或timeline_manager无效，标记为无效放置", "DragShapeController")
	
	if current_card.material:
		current_card.material.set_shader_parameter("is_invalid", not is_valid)

	# 发射悬停位置变化信号（用于时间轴可视化器）
	hover_position_changed.emit(hover_grid_pos, is_valid)

	# ★ 已禁用：更新鼠标上方效果文字显示（根据用户要求移除效果框）
	# _update_cursor_tooltip(hover_grid_pos, is_valid)
	
	# 更新时间轴网格预览（显示蓝色/红色格子）
	_update_timeline_grid_preview(hover_grid_pos, is_valid)


## 更新鼠标上方效果文字显示
func _update_cursor_tooltip(grid_pos: Vector2i, is_valid: bool) -> void:
	if not is_instance_valid(cursor_tooltip):
		return

	var tooltip_text = ""
	var text_color = ""
	var damage_amount = 0

	if is_valid:
		# 有效位置：显示卡牌效果预览（红色字）
		tooltip_text = _get_card_effect_preview_text()
		text_color = "#ff5555"  # 红色
		damage_amount = _get_card_damage_amount()

		# 触发敌人变红效果和血条预览
		_trigger_enemy_effect_preview(damage_amount)
	else:
		# 无效位置：显示"无效果"（灰色字）
		tooltip_text = "无效果"
		text_color = "#888888"  # 灰色

		# 清除预览效果
		_clear_effect_preview()

	# 设置提示文本和颜色 - 使用BBCode格式
	cursor_tooltip.text = "[color=" + text_color + "]" + tooltip_text + "[/color]"

	# 强制重置尺寸，防止幽灵撑大（模仿Main.gd中的做法）
	cursor_tooltip.size = Vector2.ZERO

	# 显示提示框
	cursor_tooltip.show()

	# 使用call_deferred延迟计算位置，避免阻塞（解决延迟问题）
	call_deferred("_deferred_position_tooltip")


## 延迟计算提示框位置
func _deferred_position_tooltip() -> void:
	if not is_instance_valid(cursor_tooltip):
		return

	# 更新位置跟随鼠标
	var mouse_pos = get_global_mouse_position()
	var screen_size = get_viewport_rect().size

	# 计算提示框位置（使用统一偏移量: Vector2(20, -30)）
	var tooltip_width = cursor_tooltip.size.x
	var tooltip_height = cursor_tooltip.size.y

	var target_x = mouse_pos.x + 20
	var target_y = mouse_pos.y - 30

	# 边界检查：防止提示框超出屏幕
	if target_x + tooltip_width > screen_size.x:
		target_x = mouse_pos.x - tooltip_width - 20

	if target_y + tooltip_height > screen_size.y:
		target_y = screen_size.y - tooltip_height - 10

	cursor_tooltip.global_position = Vector2(target_x, target_y)


## 更新时间轴网格预览
func _update_timeline_grid_preview(grid_pos: Vector2i, is_valid: bool) -> void:
	if not timeline_ui or not current_shape_coords:
		return
	
	# 检查timeline_ui是否有update_grid_preview方法
	if timeline_ui.has_method("update_grid_preview"):
		# 获取卡牌的形状信息（如果卡牌有专门的方法）
		var shape_coords = current_shape_coords
		
		# 调用timeline_ui更新网格预览
		timeline_ui.update_grid_preview(shape_coords, grid_pos, is_valid)
	else:
		GameLogger.debug("timeline_ui没有update_grid_preview方法", "DragShapeController")


## 获取卡牌效果预览文本
func _get_card_effect_preview_text() -> String:
	if not current_card:
		return ""

	# 尝试获取原始描述，避免BBCode标签问题
	var raw_description = ""
	if current_card.has_method("get_parsed_description"):
		# 获取原始描述，而不是BBCode格式的描述
		if current_card.get("raw_description") != null:
			raw_description = current_card.raw_description
		else:
			# 如果没有原始描述，尝试从卡牌信息中获取
			if current_card.get("card_info") != null and "效果" in current_card.card_info:
				raw_description = current_card.card_info["效果"]

	# 如果无法获取原始描述，使用解析描述并尝试移除BBCode标签
	if raw_description.is_empty():
		var parsed_description = current_card.get_parsed_description()
		# 简单移除BBCode标签
		raw_description = parsed_description.replace("[color=", "").replace("]", "").replace("[/color]", "")

	# 根据描述内容生成预览文本
	if "摧毁" in raw_description:
		return "摧毁目标：-10 生命值"
	elif "治疗" in raw_description:
		return "治疗目标：+8 生命值"
	elif "攻击" in raw_description:
		return "攻击目标：-5 生命值"
	else:
		# 默认显示描述的前20个字符（移除BBCode标签后）
		var clean_text = raw_description.replace("[", "").replace("]", "")
		var preview = clean_text.substr(0, 20)
		if clean_text.length() > 20:
			preview += "..."
		return preview


## 获取卡牌伤害数值
func _get_card_damage_amount() -> int:
	if not current_card:
		return 0

	var card_description = current_card.get_parsed_description()

	# 根据卡牌描述提取伤害数值
	if "摧毁" in card_description:
		return 10
	elif "攻击" in card_description:
		return 5
	elif "治疗" in card_description:
		return 8
	else:
		return 0


## 触发敌人效果预览
func _trigger_enemy_effect_preview(damage_amount: int) -> void:
	# 获取当前地块上的敌人
	var target_enemy = _get_enemy_on_tile(current_target_tile)

	if target_enemy and target_enemy.has_method("show_card_effect_preview"):
		# 触发敌人变红效果
		target_enemy.show_card_effect_preview(damage_amount)

	# 触发血条预览效果
	var health_manager = _get_health_bar_manager()
	if health_manager and health_manager.has_method("preview_damage_effect"):
		health_manager.preview_damage_effect(damage_amount)


## 清除效果预览
func _clear_effect_preview() -> void:
	# 清除敌人预览效果
	var target_enemy = _get_enemy_on_tile(current_target_tile)
	if target_enemy and target_enemy.has_method("clear_card_effect_preview"):
		target_enemy.clear_card_effect_preview()

	# 清除血条预览效果
	var health_manager = _get_health_bar_manager()
	if health_manager and health_manager.has_method("clear_preview_effect"):
		health_manager.clear_preview_effect()


## 停止拖拽并重置状态（右键取消或放置失败时调用）
## 清除时间轴预览，恢复UI交互，将卡牌返回手牌
func _end_dragging() -> void:
	is_dragging = false
	is_placing = false

	# 保存当前卡牌引用，以便后续恢复
	var card_to_restore = current_card

	# 发射拖拽结束信号（用于时间轴可视化器）
	if is_instance_valid(card_to_restore):
		drag_ended.emit(card_to_restore, false)  # false表示未放置

	# 清除时间轴网格预览
	if timeline_ui and timeline_ui.has_method("clear_grid_preview"):
		timeline_ui.clear_grid_preview()
	
	# 收起时间轴（优先使用collapse方法，确保遮罩隐藏）
	if timeline_ui:
		if timeline_ui.has_method("collapse"):
			timeline_ui.collapse()
		elif timeline_ui.has_method("toggle_expand"):
			timeline_ui.toggle_expand()
		
		# ★ 禁用时间轴点击缩放（卡牌拖拽结束）
		if timeline_ui.has_method("set_allow_click_to_expand"):
			timeline_ui.set_allow_click_to_expand(false)
			GameLogger.debug("已禁用时间轴点击缩放权限（卡牌拖拽结束）", "DragShapeController")

	# 隐藏提示
	if is_instance_valid(cursor_tooltip):
		cursor_tooltip.hide()

	# 恢复鼠标过滤（确保地块和时间轴UI可以正常交互）
	_restore_mouse_filters()

	# 卡牌返回手牌逻辑
	if is_instance_valid(card_to_restore):
		# 停止卡牌上的所有动画
		if card_to_restore.has_method("force_reset_visuals"):
			card_to_restore.force_reset_visuals()

		# 调用卡牌的返回手牌方法
		if card_to_restore.has_method("return_to_hand"):
			card_to_restore.return_to_hand()
			GameLogger.info("卡牌返回手牌动画已触发（通过return_to_hand）", "DragShapeController")
		elif card_to_restore.has_method("force_deselect"):
			# 备用方案：使用旧的force_deselect方法
			card_to_restore.force_deselect()
			GameLogger.info("卡牌返回手牌动画已触发（通过force_deselect）", "DragShapeController")
		else:
			GameLogger.warning("卡牌没有返回手牌的方法", "DragShapeController")
	else:
		GameLogger.warning("当前卡牌无效", "DragShapeController")

	# 清空暂存数据
	current_target_tile = null
	current_shape_coords = []
	current_card = null


## 恢复鼠标过滤
func _restore_mouse_filters() -> void:
	# 恢复地块容器鼠标交互
	var hex_map = _get_hex_map()
	if hex_map and hex_map.has_method("set_tiles_interactive"):
		hex_map.set_tiles_interactive(true)
		GameLogger.debug("已恢复地块交互", "DragShapeController")
		# 解锁地块视觉状态，允许鼠标悬停事件恢复正常
		if hex_map.has_method("set_visuals_locked"):
			hex_map.set_visuals_locked(false)

	# 恢复时间轴UI鼠标交互
	if timeline_ui and timeline_ui is Control:
		timeline_ui.mouse_filter = Control.MOUSE_FILTER_PASS
		GameLogger.debug("已恢复时间轴UI鼠标交互", "DragShapeController")

	# 恢复网格单元格鼠标交互
	_restore_grid_cells_mouse_filter()


## 右键取消选中
func _cancel_selection() -> void:
	if not is_dragging or is_placing:
		return

	GameLogger.info("右键取消卡牌选中", "DragShapeController")

	# 清除所有预览效果
	_clear_effect_preview()

	# 通知卡牌管理器取消选中
	var cm = _get_card_manager()
	if cm:
		cm.deselect_card()

	# 清除地块条件效果
	var hex_map = _get_hex_map()
	if hex_map and hex_map.has_method("update_all_stack_conditional_effects"):
		hex_map.update_all_stack_conditional_effects()

	# 停止拖拽并重置状态
	_end_dragging()

	GameLogger.info("卡牌选中已取消", "DragShapeController")


## 获取地块上的敌人
func _get_enemy_on_tile(tile: Node) -> Node:
	if not tile:
		return null

	# 这里需要根据你的项目结构来获取地块上的敌人
	# 暂时返回null，需要根据实际项目结构实现
	return null


## 获取血条管理器
func _get_health_bar_manager() -> Node:
	var scene_root = get_tree().current_scene
	if not scene_root:
		return null

	# 查找血条管理器节点
	var health_manager = scene_root.get_node_or_null("HealthBarManager")
	if health_manager:
		return health_manager

	# 如果找不到，尝试通过组名查找
	var managers = get_tree().get_nodes_in_group("health_bar_manager")
	if not managers.is_empty():
		return managers[0]

	return null





## 查找玩家手牌
func _find_player_hand() -> Node:
	var scene_root = get_tree().current_scene
	if not scene_root:
		return null

	# 查找手牌节点（通常名为"Hand"或"PlayerHand"）
	var hand = scene_root.get_node_or_null("Hand")
	if hand:
		return hand

	# 尝试通过组名查找
	var hands = get_tree().get_nodes_in_group("player_hand")
	if not hands.is_empty():
		return hands[0]

	# 尝试查找包含"hand"的节点
	for child in scene_root.get_children():
		if "hand" in child.name.to_lower():
			return child

	return null


## 处理自由拖拽逻辑（鼠标不在时间轴上时）
## 卡牌自由跟随鼠标移动，不受网格约束
func _handle_free_drag(mouse_pos: Vector2) -> void:
	# 计算卡牌中心的目标位置（保持鼠标相对于卡牌中心的偏移）
	var target_center = mouse_pos - drag_offset
	GameLogger.debug("自由拖拽: 目标=" + str(target_center), "DragShapeController")
	
	# 将卡牌中心位置转换为左上角位置
	var card_top_left = target_center
	if current_card.has_method("get_size"):
		var card_size = current_card.get_size()
		var card_scale = current_card.scale
		card_top_left -= card_size * card_scale / 2

	
	current_card.global_position = card_top_left
	if current_card.material:
		current_card.material.set_shader_parameter("is_invalid", false)

# ==========================================
# 输入处理
# ==========================================


func _input(event: InputEvent) -> void:
	# 右键取消选中（适用于所有场景）
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_RIGHT:
		_cancel_selection()
		get_viewport().set_input_as_handled()
		return

	if not is_dragging or is_placing:
		return

	# Q键 逆时针旋转
	if event is InputEventKey and event.pressed and event.keycode == KEY_Q:
		rotate_shape(-1)

	# E键 顺时针旋转
	if event is InputEventKey and event.pressed and event.keycode == KEY_E:
		rotate_shape(1)

	# 左键点击确认放置
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		GameLogger.debug("_input: 收到左键点击事件，尝试放置", "DragShapeController")
		try_place_shape()


## 旋转形状
## 旋转当前拖拽的卡牌形状
## @param direction 旋转方向：1为顺时针，-1为逆时针
## 更新卡牌视觉旋转动画，并相应调整形状坐标数据
func rotate_shape(direction: int) -> void:
	# 视觉旋转
	if not is_instance_valid(current_card):
		return

	var tw = create_tween()
	var target_rot = current_card.rotation + (PI / 2 * direction)
	tw.tween_property(current_card, "rotation", target_rot, rotation_animation_duration)

	# 数据旋转坐标系
	for i in range(current_shape_coords.size()):
		var c = current_shape_coords[i]
		if direction == 1:
			current_shape_coords[i] = Vector2i(-c.y, c.x)
		else:
			current_shape_coords[i] = Vector2i(c.y, -c.x)
	
	# 旋转完成后更新拖拽偏移（防止旋转后偏移错位）
	tw.finished.connect(func():
		if is_instance_valid(current_card):
			var mouse_pos = get_global_mouse_position()
			# 使用卡牌中心计算偏移
			var card_center = current_card.global_position
			if current_card.has_method("get_size"):
				var card_size = current_card.get_size()
				var card_scale = current_card.scale
				card_center += card_size * card_scale / 2
			drag_offset = mouse_pos - card_center
			GameLogger.debug("旋转后更新拖拽偏移，新偏移: " + str(drag_offset), "DragShapeController")
	)

# ==========================================
# 放置逻辑
# ==========================================


## 尝试将当前拖拽的形状放置到时间轴上
## 验证鼠标位置是否在时间轴内，计算网格坐标，检查放置有效性
## 有效时执行放置动画，无效时显示拒绝提示并返回手牌
func try_place_shape() -> void:
	GameLogger.debug("try_place_shape: 收到放置请求，is_dragging=" + str(is_dragging) + ", is_placing=" + str(is_placing), "DragShapeController")
	
	var mouse_pos = get_global_mouse_position()
	var is_over_timeline = timeline_ui.get_global_rect().has_point(mouse_pos)
	GameLogger.debug("try_place_shape: 鼠标位置=" + str(mouse_pos) + ", 是否在时间轴上=" + str(is_over_timeline), "DragShapeController")

	if not is_over_timeline:
		GameLogger.info("在时间轴外点击，取消拖拽并返回手牌", "DragShapeController")
		_end_dragging()
		return

	# ★ 修复实际放置判定：与 _handle_timeline_hover 保持一致的坐标计算
	# 使用 get_local_mouse_position() 自动剔除父级缩放变换，解决视觉预览正确但实际放置位置偏移的 Bug
	var local_mouse = timeline_ui.grid_background.get_local_mouse_position()
	
	# ★ 负数越界保护：防止在网格左侧或上方悬浮时产生无效坐标
	if local_mouse.x < 0 or local_mouse.y < 0:
		GameLogger.warning("鼠标位于网格背景左侧或上方，local_mouse=" + str(local_mouse) + "，坐标越界，触发拒绝动画", "DragShapeController")
		_play_reject_animation()
		return
	
	# 从timeline_ui获取实际的格子大小和间距，确保与UI设置一致
	var ui_slot_size = timeline_ui.slot_size if "slot_size" in timeline_ui else slot_size
	var ui_spacing = timeline_ui.spacing if "spacing" in timeline_ui else spacing
	var cell_size = ui_slot_size + ui_spacing
	GameLogger.debug("网格计算参数: slot_size=" + str(ui_slot_size) + ", spacing=" + str(ui_spacing) + 
		", cell_size=" + str(cell_size) + ", local_mouse=" + str(local_mouse), "DragShapeController")
	
	var grid_x = int(local_mouse.x / cell_size)
	var grid_y = int(local_mouse.y / cell_size)
	var origin_pos = Vector2i(grid_x, grid_y)
	
	# ★ 边界检查：确保计算的网格坐标在合理范围内
	var max_grid_x = 12  # TimelineManager.GRID_WIDTH
	var max_grid_y = 3   # TimelineManager.GRID_HEIGHT
	
	if grid_x < 0 or grid_x >= max_grid_x or grid_y < 0 or grid_y >= max_grid_y:
		GameLogger.warning("网格坐标超出范围！origin_pos=" + str(origin_pos) + 
			", 允许范围: X[0-" + str(max_grid_x-1) + "], Y[0-" + str(max_grid_y-1) + "]", "DragShapeController")
		_play_reject_animation()
		return
	
	GameLogger.debug("尝试放置，origin_pos: " + str(origin_pos) + 
		", shape_coords: " + str(current_shape_coords) + 
		", 网格范围检查通过", "DragShapeController")
	
	# 预计算所有目标位置并记录
	if not current_shape_coords.is_empty():
		GameLogger.debug("预计算所有目标位置:", "DragShapeController")
		for offset in current_shape_coords:
			var target_pos = origin_pos + offset
			GameLogger.debug("  offset=%s -> target_pos=%s" % [offset, target_pos], "DragShapeController")
	else:
		GameLogger.warning("current_shape_coords 为空!", "DragShapeController")

	# 验证并放置
	if timeline_manager and timeline_manager.is_placement_valid(current_shape_coords, origin_pos):
		_place_action(origin_pos)
	else:
		GameLogger.warning("时间轴管理器验证失败！origin_pos=" + str(origin_pos), "DragShapeController")
		_play_reject_animation()
		# 放置失败后自动返回手牌（拒绝动画完成后会调用_end_dragging）


## 执行放置
func _place_action(origin_pos: Vector2i) -> void:
	GameLogger.info("放置成功！开始播放放置动画", "DragShapeController")

	# 播放放置动画，动画完成后会调用_finish_placement
	_play_placement_animation(origin_pos)


## 将普通数组转换为Vector2i数组
## 支持多种输入格式：Vector2i、[x, y]数组、{x: value, y: value}字典
func _convert_to_vector2i_array(raw_array: Array) -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	GameLogger.debug("_convert_to_vector2i_array: 原始数组长度=" + str(raw_array.size()) + ", 内容=" + str(raw_array), "DragShapeController")

	for i in range(raw_array.size()):
		var item = raw_array[i]
		if item is Vector2i:
			GameLogger.debug("项目 " + str(i) + ": Vector2i类型, 值=" + str(item), "DragShapeController")
			result.append(item)
		elif item is Array and item.size() >= 2:
			# 处理 [x, y] 格式的数组
			var x = int(item[0])
			var y = int(item[1])
			var vec = Vector2i(x, y)
			GameLogger.debug("项目 " + str(i) + ": 数组类型, [" + str(item[0]) + ", " + str(item[1]) + "] -> " + str(vec), "DragShapeController")
			result.append(vec)
		elif item is Dictionary and "x" in item and "y" in item:
			# 处理 {x: value, y: value} 格式的字典
			var x = int(item["x"])
			var y = int(item["y"])
			var vec = Vector2i(x, y)
			GameLogger.debug("项目 " + str(i) + ": 字典类型, {x:" + str(item["x"]) + ", y:" + str(item["y"]) + "} -> " + str(vec), "DragShapeController")
			result.append(vec)
		else:
			GameLogger.debug("项目 " + str(i) + ": 未知类型 " + str(typeof(item)) + ", 值=" + str(item), "DragShapeController")

	if result.is_empty():
		GameLogger.debug("转换结果为空，添加默认Vector2i(0, 0)", "DragShapeController")
		result.append(Vector2i(0, 0))

	GameLogger.debug("转换完成，结果: " + str(result), "DragShapeController")
	return result

## 停止拖拽（右键取消时调用）


## 显示拒绝放置提示
func _show_reject_tooltip(message: String = "无法放置") -> void:
	if not is_instance_valid(cursor_tooltip):
		return
	
	# 设置提示文本和样式
	cursor_tooltip.text = "[color=#ff5555]" + message + "[/color]"
	cursor_tooltip.size = Vector2.ZERO  # 重置尺寸
	cursor_tooltip.show()
	
	# 更新提示框位置跟随鼠标
	call_deferred("_update_reject_tooltip_position")


## 更新拒绝提示位置
func _update_reject_tooltip_position() -> void:
	if not is_instance_valid(cursor_tooltip):
		return
	
	var mouse_pos = get_global_mouse_position()
	var screen_size = get_viewport_rect().size
	
	# 计算提示框位置
	var tooltip_width = cursor_tooltip.size.x
	var tooltip_height = cursor_tooltip.size.y
	
	var target_x = mouse_pos.x + 20
	var target_y = mouse_pos.y - 30
	
	# 边界检查：防止提示框超出屏幕
	if target_x + tooltip_width > screen_size.x:
		target_x = mouse_pos.x - tooltip_width - 20
	
	if target_y + tooltip_height > screen_size.y:
		target_y = screen_size.y - tooltip_height - 10
	
	cursor_tooltip.global_position = Vector2(target_x, target_y)


## 隐藏拒绝放置提示
func _hide_reject_tooltip() -> void:
	if is_instance_valid(cursor_tooltip):
		cursor_tooltip.hide()


## 播放拒绝动画
func _play_reject_animation() -> void:
	GameLogger.warning("放置失败，位置非法或重叠！", "DragShapeController")
	if not is_instance_valid(current_card):
		return

	# 显示"无法放置"提示
	_show_reject_tooltip("无法放置")

	var tw = create_tween()
	var orig_x = current_card.position.x
	tw.tween_property(current_card, "position:x", orig_x - 10, 0.05)
	tw.tween_property(current_card, "position:x", orig_x + 10, 0.05)
	tw.tween_property(current_card, "position:x", orig_x, 0.05)

	# ★ 已修改：动画完成后不返回手牌，保持拖拽状态
	# 添加一个计时器，2秒后自动隐藏提示
	var timer = get_tree().create_timer(2.0)
	timer.timeout.connect(_hide_reject_tooltip)


## 播放放置动画
func _play_placement_animation(grid_pos: Vector2i) -> void:
	GameLogger.info("开始播放放置动画", "DragShapeController")
	is_placing = true

	# 禁用卡牌鼠标交互，防止与其他元素碰撞
	if is_instance_valid(current_card):
		current_card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		# 移除无效状态着色器
		if current_card.material:
			current_card.material.set_shader_parameter("is_invalid", false)

	# 禁用地块容器和时间轴UI的鼠标交互，防止意外触发
	var hex_map = _get_hex_map()
	if hex_map and hex_map is Control:
		hex_map.mouse_filter = Control.MOUSE_FILTER_IGNORE
		GameLogger.debug("已禁用地块容器鼠标交互", "DragShapeController")

	if timeline_ui and timeline_ui is Control:
		timeline_ui.mouse_filter = Control.MOUSE_FILTER_IGNORE
		GameLogger.debug("已禁用时间轴UI鼠标交互", "DragShapeController")

	# ★ 修复缩放导致的卡牌放置动画飞偏：直接使用 grid_cells 字典获取精准位置
	var grid_cell_center = Vector2.ZERO
	if "grid_cells" in timeline_ui and timeline_ui.grid_cells.has(grid_pos):
		var target_cell = timeline_ui.grid_cells[grid_pos]
		# target_cell.global_position 是绝对精准的（已包含所有父级变换）
		# size 乘以 timeline_ui 的实际 scale 获取真实视觉大小
		var actual_cell_size = target_cell.size * timeline_ui.scale
		grid_cell_center = target_cell.global_position + (actual_cell_size / 2.0) + float_offset
		GameLogger.debug("使用 grid_cells 精确计算中心点: 目标格子=" + str(target_cell) + 
			", 全局位置=" + str(target_cell.global_position) + 
			", 实际大小=" + str(actual_cell_size) + 
			", 时间轴缩放=" + str(timeline_ui.scale), "DragShapeController")
	else:
		# 兜底方案：使用鼠标位置（理论上不应该发生）
		grid_cell_center = get_global_mouse_position()
		GameLogger.warning("grid_cells 字典不存在或缺少目标格子，使用鼠标位置作为兜底: " + str(grid_cell_center), "DragShapeController")
	
	# 将卡牌中心位置转换为左上角位置（注意卡牌的目标 scale 是 0.9）
	var card_top_left = grid_cell_center
	if current_card.has_method("get_size"):
		var card_size = current_card.get_size()
		var target_card_scale = Vector2(0.9, 0.9)  # 卡牌动画目标缩放
		card_top_left -= card_size * target_card_scale / 2
		GameLogger.debug("放置动画卡牌位置计算: 网格单元中心=" + str(grid_cell_center) + 
			", 卡牌大小=" + str(card_size) + 
			", 目标缩放=" + str(target_card_scale) + 
			", 左上角=" + str(card_top_left), "DragShapeController")
	
	GameLogger.debug("放置动画目标位置: " + str(card_top_left) + 
		", grid_pos=" + str(grid_pos) + 
		", 时间轴缩放=" + str(timeline_ui.scale) + 
		", 是否使用精确计算=" + str("grid_cells" in timeline_ui and timeline_ui.grid_cells.has(grid_pos)), "DragShapeController")

	# 创建放置动画：飞向网格位置并适当缩小
	var tw = create_tween()
	tw.set_parallel(true)
	tw.tween_property(current_card, "global_position", card_top_left, 0.3) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_property(current_card, "scale", Vector2(0.9, 0.9), 0.3) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_property(current_card, "modulate:a", 0.7, 0.3) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	# 动画完成后执行实际放置逻辑
	tw.finished.connect(_finish_placement.bind(grid_pos))


## 完成放置（动画结束后调用）
func _finish_placement(grid_pos: Vector2i) -> void:
	GameLogger.info("放置动画完成，执行实际放置逻辑", "DragShapeController")

	# 恢复鼠标过滤（确保地块和时间轴UI可以正常交互）
	_restore_mouse_filters()

	# 执行实际的时间轴放置
	var action = TimelineAction.new(
		TimelineAction.Type.PLAYER,
		current_card,
		current_target_tile,
		current_shape_coords,
		Color.AQUA,
		current_card.card_info
	)

	var placement_success = false
	if timeline_manager:
		placement_success = timeline_manager.place_action(action, grid_pos)
		GameLogger.debug("时间轴放置结果: success=" + str(placement_success) + ", grid_pos=" + str(grid_pos), "DragShapeController")
	
	if placement_success:
		player_action_placed.emit(action)
	else:
		GameLogger.warning("时间轴放置失败！grid_pos=" + str(grid_pos) + ", shape_coords=" + str(current_shape_coords), "DragShapeController")
		# 放置失败，触发拒绝动画
		_play_reject_animation()
		# 这里不调用end_dragging_success，因为放置失败了
		is_placing = false
		return

	# 触发卡牌效果（如果有目标地块）
	_trigger_card_effect()
	
	# 清除时间轴网格预览
	if timeline_ui and timeline_ui.has_method("clear_grid_preview"):
		timeline_ui.clear_grid_preview()

	# 放置完成后清理
	end_dragging_success()
	is_placing = false


## 触发卡牌效果
func _trigger_card_effect() -> void:
	if not is_instance_valid(current_card):
		return

	# 检查卡牌是否有触发效果的方法
	if current_card.has_method("apply_effect_immediate"):
		# 如果有目标地块，触发效果
		if is_instance_valid(current_target_tile):
			GameLogger.info("触发卡牌效果，目标地块: %s" % current_target_tile.name, "DragShapeController")
			# 注意：原apply_effect_immediate会销毁卡牌，但我们希望卡牌进入弃牌区
			# 这里只记录日志，不实际触发效果（效果将在时间轴结算时触发）
			# current_card.apply_effect_immediate(current_target_tile)  # 注释掉，避免销毁卡牌

			# 可以在这里发射信号让其他系统处理效果
			# effect_triggered.emit(current_card, current_target_tile)
		else:
			GameLogger.info("卡牌没有目标地块，无法立即触发效果", "DragShapeController")
	else:
		GameLogger.debug("卡牌没有apply_effect_immediate方法", "DragShapeController")


## 成功结束拖拽
func end_dragging_success() -> void:
	is_dragging = false
	is_placing = false

	# 发射拖拽结束信号（用于时间轴可视化器）
	if is_instance_valid(current_card):
		drag_ended.emit(current_card, true)  # true表示已放置

	# 处理弃牌逻辑
	if is_instance_valid(current_card):
		var card_moved_to_discard = false
		
		# 方案1：优先使用project.gd的弃牌系统
		var control_node = _find_project_node()
		if control_node:
			GameLogger.debug("找到project节点，尝试使用其弃牌系统", "DragShapeController")
			
			# 检查project是否有discard_pile属性 - 使用安全的get方法
			var discard_pile_ref = control_node.get("discard_pile")
			if discard_pile_ref != null:
				if discard_pile_ref is Object:
					# 安全检查是否有move_cards方法
					var has_move_cards_method = false
					if discard_pile_ref is Object:
						has_move_cards_method = discard_pile_ref.has_method("move_cards")
					
					if has_move_cards_method:
						# 将卡牌移动到弃牌区
						discard_pile_ref.move_cards([current_card])
						GameLogger.info("通过project.gd将卡牌移入弃牌区: %s" % current_card.name, "DragShapeController")
						card_moved_to_discard = true
					else:
						GameLogger.debug("discard_pile对象没有move_cards方法", "DragShapeController")
				else:
					GameLogger.debug("discard_pile属性不是有效的对象", "DragShapeController")
			else:
				GameLogger.debug("project节点没有discard_pile属性", "DragShapeController")
			
			# 如果project有弃牌处理方法，调用它（即使卡牌没有移动到弃牌区）
			# 使用安全的方式检查方法是否存在
			var has_discard_effects_method = false
			if control_node is Object:
				has_discard_effects_method = control_node.has_method("_handle_discard_effects")
			
			if has_discard_effects_method:
				GameLogger.info("调用project.gd的弃牌效果处理方法: %s" % current_card.name, "DragShapeController")
				control_node.call_deferred("_handle_discard_effects", current_card)
		
		# 方案2：如果project.gd不可用，使用DragShapeController找到的弃牌区引用
		if not card_moved_to_discard and discard_pile:
			# 安全检查是否有move_cards方法
			var has_move_cards_method = false
			if discard_pile is Object:
				has_move_cards_method = discard_pile.has_method("move_cards")
			
			if has_move_cards_method:
				GameLogger.info("使用DragShapeController的弃牌区将卡牌移入弃牌区: %s" % current_card.name, "DragShapeController")
				# 确保卡牌状态恢复
				current_card.mouse_filter = Control.MOUSE_FILTER_STOP
				current_card.modulate = Color.WHITE
				current_card.scale = Vector2.ONE
				# 将卡牌移入弃牌区
				discard_pile.move_cards([current_card])
				card_moved_to_discard = true
		
		# 方案3：如果以上都失败，尝试重新查找弃牌区
		if not card_moved_to_discard:
			GameLogger.debug("弃牌区未找到，尝试重新查找...", "DragShapeController")
			_find_discard_pile()
			
			if discard_pile:
				# 安全检查是否有move_cards方法
				var has_move_cards_method = false
				if discard_pile is Object:
					has_move_cards_method = discard_pile.has_method("move_cards")
				
				if has_move_cards_method:
					GameLogger.info("重新查找后找到弃牌区，将卡牌移入弃牌区", "DragShapeController")
					discard_pile.move_cards([current_card])
					card_moved_to_discard = true
		
		# 方案4：终极回退 - 返回手牌
		if not card_moved_to_discard:
			GameLogger.warning("未找到弃牌区，让卡牌返回手牌: %s" % current_card.name, "DragShapeController")
			_return_card_to_hand(current_card)

	# 收起时间轴
	if timeline_ui.has_method("collapse"):
		timeline_ui.collapse()


## 查找project.gd节点（替代废弃的control.gd）
func _find_project_node() -> Node:
	# 直接定位project节点（DragShapeController在ui/TimelineSystem下，project是根节点）
	var project_node = get_node_or_null("../..")
	if project_node and project_node.name == "in_scene":
		GameLogger.debug("找到in_scene节点", "DragShapeController")
		return project_node
	
	# 备用方案：绝对路径
	project_node = get_tree().root.get_node_or_null("/root/in_scene")
	if project_node:
		GameLogger.debug("通过绝对路径找到in_scene节点", "DragShapeController")
		return project_node
	
	return null





## 禁用时间轴网格单元格鼠标交互
func _disable_grid_cells_mouse_filter() -> void:
	if not timeline_ui or not ("grid_cells" in timeline_ui):
		return
	
	var grid_cells = timeline_ui.grid_cells
	if not (grid_cells is Dictionary):
		return
	
	for cell in grid_cells.values():
		if cell is Control:
			cell.mouse_filter = Control.MOUSE_FILTER_IGNORE
			GameLogger.debug("已禁用网格单元格鼠标交互: " + str(cell.name), "DragShapeController")

## 恢复时间轴网格单元格鼠标交互
func _restore_grid_cells_mouse_filter() -> void:
	if not timeline_ui or not ("grid_cells" in timeline_ui):
		return
	
	var grid_cells = timeline_ui.grid_cells
	if not (grid_cells is Dictionary):
		return
	
	for cell in grid_cells.values():
		if cell is Control:
			cell.mouse_filter = Control.MOUSE_FILTER_PASS
			GameLogger.debug("已恢复网格单元格鼠标交互: " + str(cell.name), "DragShapeController")

## 将卡牌返回手牌
func _return_card_to_hand(card: Node) -> void:
	if not is_instance_valid(card):
		return
	
	# 查找手牌容器
	var hand = _find_player_hand()
	if hand and hand.has_method("move_cards"):
		# 确保卡牌状态恢复
		card.mouse_filter = Control.MOUSE_FILTER_STOP
		card.modulate = Color.WHITE
		card.scale = Vector2.ONE
		# 将卡牌返回手牌
		hand.move_cards([card])
		GameLogger.info("卡牌已返回手牌: %s" % card.name, "DragShapeController")
	else:
		GameLogger.warning("无法找到手牌容器，卡牌将保留在原处: %s" % card.name, "DragShapeController")
	
	if timeline_ui.has_method("toggle_expand"):
		timeline_ui.toggle_expand()  # 回退方案

	# 隐藏提示
	if is_instance_valid(cursor_tooltip):
		cursor_tooltip.hide()

	# 清空暂存数据
	current_target_tile = null
	current_shape_coords = []
	current_card = null


## 检查放置是否有效（公共方法，供时间轴可视化器调用）
func _is_placement_valid(grid_pos: Vector2i) -> bool:
	if not timeline_manager or current_shape_coords.is_empty():
		return false

	return timeline_manager.is_placement_valid(current_shape_coords, grid_pos)
