class_name DragShapeController
extends Node2D
# 原文件名: DragShapeController(时间轴拖拽控制).gd
# 功能: 时间轴拖拽控制
## 拖拽形状控制器
## 处理卡牌拖拽到时间轴的交互，包括旋转、网格吸附和放置验证

const TimelineClearEffectUtil = preload("res://scene/in_scene/timeline/TimelineClearEffect.gd")
const DragShapeNodeBridgeScript = preload("res://scene/in_scene/drag_modules/DragShapeNodeBridge.gd")
const DragRejectTooltipControllerScript = preload("res://scene/in_scene/drag_modules/DragRejectTooltipController.gd")
const DragTimelineGridPreviewPresenterScript = preload("res://scene/in_scene/drag_modules/DragTimelineGridPreviewPresenter.gd")
const DragCardShapeResolverScript = preload("res://scene/in_scene/drag_modules/DragCardShapeResolver.gd")
const DragTimelineGridMouseFilterControllerScript = preload("res://scene/in_scene/drag_modules/DragTimelineGridMouseFilterController.gd")
const DragPlacementQueryServiceScript = preload("res://scene/in_scene/drag_modules/DragPlacementQueryService.gd")
const DragTimelineUiStateControllerScript = preload("res://scene/in_scene/drag_modules/DragTimelineUiStateController.gd")
const DragTimelineGridCoordinateResolverScript = preload("res://scene/in_scene/drag_modules/DragTimelineGridCoordinateResolver.gd")
const DragSceneInteractionLockControllerScript = preload("res://scene/in_scene/drag_modules/DragSceneInteractionLockController.gd")

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
@export var drag_shader: Shader
@export var float_offset: Vector2 = Vector2(-15, -15)
@export var mouse_grab_offset: Vector2 = Vector2.ZERO # ★ 新增：鼠标抓取偏移，(0,0)表示完美吸附在卡牌中心

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
var is_timeline_clear_mode: bool = false  ## clear 类即时卡牌专用模式：借用时间轴选格，但不创建 TimelineAction

# ==========================================
# 节点引用
# ==========================================
var timeline_ui: Control
var timeline_manager: Node  ## TimelineManager 实例
var cursor_tooltip: RichTextLabel
var discard_pile: Node  ## 弃牌区引用
var _node_bridge = null
var _reject_tooltip_controller = null
var _grid_preview_presenter = null
var _card_shape_resolver = null
var _grid_mouse_filter_controller = null
var _placement_query_service = null
var _timeline_ui_state_controller = null
var _timeline_grid_coordinate_resolver = null
var _scene_interaction_lock_controller = null


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false

# ==========================================
# 工具函数
# ==========================================
func _get_node_bridge():
	if _node_bridge == null:
		_node_bridge = DragShapeNodeBridgeScript.new(self)
	return _node_bridge


func _get_reject_tooltip_controller():
	if _reject_tooltip_controller == null:
		_reject_tooltip_controller = DragRejectTooltipControllerScript.new()
	return _reject_tooltip_controller


func _get_grid_preview_presenter():
	if _grid_preview_presenter == null:
		_grid_preview_presenter = DragTimelineGridPreviewPresenterScript.new()
	return _grid_preview_presenter


func _get_card_shape_resolver():
	if _card_shape_resolver == null:
		_card_shape_resolver = DragCardShapeResolverScript.new()
	return _card_shape_resolver


func _get_grid_mouse_filter_controller():
	if _grid_mouse_filter_controller == null:
		_grid_mouse_filter_controller = DragTimelineGridMouseFilterControllerScript.new()
	return _grid_mouse_filter_controller


func _get_placement_query_service():
	if _placement_query_service == null:
		_placement_query_service = DragPlacementQueryServiceScript.new()
	return _placement_query_service


func _get_timeline_ui_state_controller():
	if _timeline_ui_state_controller == null:
		_timeline_ui_state_controller = DragTimelineUiStateControllerScript.new()
	return _timeline_ui_state_controller


func _get_timeline_grid_coordinate_resolver():
	if _timeline_grid_coordinate_resolver == null:
		_timeline_grid_coordinate_resolver = DragTimelineGridCoordinateResolverScript.new()
	return _timeline_grid_coordinate_resolver


func _get_scene_interaction_lock_controller():
	if _scene_interaction_lock_controller == null:
		_scene_interaction_lock_controller = DragSceneInteractionLockControllerScript.new()
	return _scene_interaction_lock_controller


## 统一获取主面板 (MainBoard) 的快捷方法
func _get_main_board() -> Node:
	return _get_node_bridge().get_main_board()

## 获取卡牌管理器
func _get_card_manager() -> Node:
	return _get_node_bridge().get_card_manager()
	
## 查找弃牌区
func _find_discard_pile() -> Node:
	return _get_node_bridge().find_discard_pile()
		
## 查找玩家手牌
func _find_player_hand() -> Node:
	return _get_node_bridge().find_player_hand()
	
## 获取HexMap管理器
func _get_hex_map() -> Node:
	return _get_node_bridge().get_hex_map()
	
## 查找project.gd节点（现在叫 in_scene.gd，即 MainBoard）
func _find_project_node() -> Node:
	return _get_node_bridge().find_project_node()
# ==========================================
# 生命周期
# ==========================================


func _ready() -> void:
	_node_bridge = DragShapeNodeBridgeScript.new(self)
	_reject_tooltip_controller = DragRejectTooltipControllerScript.new()
	_grid_preview_presenter = DragTimelineGridPreviewPresenterScript.new()
	_card_shape_resolver = DragCardShapeResolverScript.new()
	_grid_mouse_filter_controller = DragTimelineGridMouseFilterControllerScript.new()
	_placement_query_service = DragPlacementQueryServiceScript.new()
	_timeline_ui_state_controller = DragTimelineUiStateControllerScript.new()
	_timeline_grid_coordinate_resolver = DragTimelineGridCoordinateResolverScript.new()
	_scene_interaction_lock_controller = DragSceneInteractionLockControllerScript.new()
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

	# 延迟查找弃牌区（确保DiscardPile已创建并注册）
	call_deferred("_find_discard_pile")

	set_process_input(true)
	set_process(true)

# ==========================================
# 拖拽启动
# ==========================================


## 启动拖拽模式
func start_dragging(card: Control, target_tile: Node) -> void:
	is_dragging = true
	current_card = card
	current_target_tile = target_tile
	is_timeline_clear_mode = TimelineClearEffectUtil.is_clear_card(card)

	current_shape_coords = _get_card_shape_resolver().resolve_shape_coords(card, is_timeline_clear_mode)

	# 发射拖拽开始信号
	drag_started.emit(card, current_shape_coords)

	# 关键修复：
	# - 不再整体禁用 TimelineUI 的鼠标交互。
	# - 否则时间轴上的敌人意图方格将完全收不到 hover，无法在拖拽阶段联动回地图。
	# - 真正需要禁用的是普通网格单元格点击，因此仍然保留 _disable_grid_cells_mouse_filter()。
	_get_timeline_ui_state_controller().enter_drag_mode(timeline_ui)
	
	_get_scene_interaction_lock_controller().lock_for_drag(_get_hex_map(), timeline_ui)

	# 禁用网格单元格鼠标交互
	_disable_grid_cells_mouse_filter()

	if is_timeline_clear_mode:
		# clear 是“手牌中静止、只借用时间轴选格”的即时效果。
		# 这里提前返回，避免走下面普通卡牌的重挂父节点、缩放、拖拽 shader 和鼠标跟随逻辑。
		if card.has_method("set_card_transparency"):
			card.set_card_transparency(1.0)
		# 卡牌视觉上仍停在手牌里，但当前输入焦点已经交给时间轴。
		# 临时忽略卡牌自身点击，避免玩家再次点到这张牌时触发普通选中/取消逻辑造成抖动。
		card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		drag_offset = Vector2.ZERO
		return

	# ★ 无副本架构：直接操作原卡牌
	# 1. 通知卡牌进入拖拽状态
	if card.has_method("enter_dragging_state"):
		card.enter_dragging_state()
	if card.has_method("set_card_transparency"):
		card.set_card_transparency(0.5)

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
			pass
		
		
		# 重新父级化到场景根节点
		scene_root.add_child(card)
		card.set_as_top_level(true)
		
		# 获取当前鼠标位置
		var mouse_pos = get_global_mouse_position()
		
		# ★ 重要：验证重新父级化后卡牌全局位置是否变化
		var new_card_global_pos = card.global_position
		var new_card_position = card.position
		if new_card_global_pos != original_card_global_pos:
			pass
		
		# ★ 关键修复：重新计算卡牌中心（考虑set_as_top_level后的坐标变化）
		# 重新获取卡牌当前的实际全局位置和大小
		var actual_card_global_pos = card.global_position
		var actual_card_size = card.get_size() if card.has_method("get_size") else original_card_size
		
		# ★ 关键修复：计算目标缩放因子并在计算中心时使用
		# 这样drag_offset计算会考虑卡牌将要缩放的大小
		var scale_factor = slot_size / 20.0
		
		# 使用目标缩放因子计算卡牌中心（而不是当前缩放）
		var actual_card_center = actual_card_global_pos + actual_card_size * Vector2(scale_factor, scale_factor) / 2
		
		# ★ 修复拖拽偏移：废弃容易出错的全局坐标换算，直接使用用户定义的抓取偏移
		# 默认值为 Vector2.ZERO，意味着鼠标会精确地按在卡牌的视觉中心点上
		drag_offset = mouse_grab_offset

		# 3. 应用拖拽着色器
		if card.material and drag_shader:
			card.material = drag_shader.duplicate()
			
			# ★ 新增修复 1：在拖拽期间彻底禁用卡牌自身的鼠标检测
			# 防止在拖拽跟随过程中鼠标意外触发卡牌自带的悬浮高亮 Shader
			card.mouse_filter = Control.MOUSE_FILTER_IGNORE

		# 4. 应用缩放动画（使用前面计算的scale_factor）
		var tw = create_tween()
		tw.tween_property(card, "scale", Vector2(scale_factor, scale_factor), scale_animation_duration)


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
	var grid_info: Dictionary = _get_timeline_grid_coordinate_resolver().resolve_hover(timeline_ui, slot_size, spacing)
	var hover_grid_pos: Vector2i = grid_info["grid_pos"]
	
	# ★ 边界检查：确保网格坐标在合理范围内（包括负坐标保护）
	var grid_width: int = grid_info["grid_width"]
	var grid_height: int = grid_info["grid_height"]
	var is_in_grid_bounds: bool = grid_info["is_in_bounds"]
	
	# ★ 修复编译错误：声明未使用的变量（原网格吸附相关变量）
	# 变量已移除，网格吸附功能已取消

	if not is_timeline_clear_mode:
		# ★ 已修改：取消网格吸附，让卡牌直接跟随鼠标（根据用户要求）
		# clear 模式不进入这里，卡牌必须停留在手牌原位，只更新时间轴预览。
		var target_center = mouse_pos - drag_offset
		var card_top_left = target_center
		if current_card.has_method("get_size"):
			var card_size = current_card.get_size()
			var card_scale = current_card.scale
			card_top_left -= card_size * card_scale / 2
		current_card.global_position = card_top_left

	# 更新着色器状态
	var is_valid = true
	var drag_visual_state: int = 0
	if is_timeline_clear_mode:
		is_valid = is_in_grid_bounds and TimelineClearEffectUtil.is_origin_in_bounds(current_shape_coords, hover_grid_pos, grid_width, grid_height)
		if is_valid and TimelineClearEffectUtil.has_overlap(timeline_manager, current_shape_coords, hover_grid_pos):
			drag_visual_state = 2
	elif timeline_manager and is_in_grid_bounds:
		is_valid = timeline_manager.is_placement_valid(current_shape_coords, hover_grid_pos)
	else:
		is_valid = false
	drag_visual_state = 1 if not is_valid else drag_visual_state
	
	if current_card.material and not is_timeline_clear_mode:
		current_card.material.set_shader_parameter("is_invalid", not is_valid)
		current_card.material.set_shader_parameter("drag_visual_state", drag_visual_state)

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
	_get_grid_preview_presenter().update_preview(
		timeline_ui,
		timeline_manager,
		current_shape_coords,
		grid_pos,
		is_valid,
		is_timeline_clear_mode
	)


func _clear_timeline_grid_preview() -> void:
	_get_grid_preview_presenter().clear_preview(timeline_ui, is_timeline_clear_mode)


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

	# 清除时间轴网格预览。
	# clear 类即时卡牌会额外生成一层覆盖在已有行动方格上方的蓝/绿/红预览，
	# 因此这里统一走专用清理接口；普通卡牌仍然只清理原本的网格预览。
	_clear_timeline_grid_preview()
	
	# 收起时间轴并禁用点击缩放。
	_get_timeline_ui_state_controller().exit_drag_mode(timeline_ui)

	# 隐藏提示
	if is_instance_valid(cursor_tooltip):
		cursor_tooltip.hide()

	# 恢复鼠标过滤（确保地块和时间轴UI可以正常交互）
	_restore_mouse_filters()

	# 卡牌返回手牌逻辑
	if is_instance_valid(card_to_restore):
		# clear 模式开始时会让卡牌忽略鼠标输入；任何取消/失败回手路径都在这里恢复。
		card_to_restore.mouse_filter = Control.MOUSE_FILTER_STOP

		# 停止卡牌上的所有动画
		if card_to_restore.has_method("force_reset_visuals"):
			card_to_restore.force_reset_visuals()

		# 调用卡牌的返回手牌方法
		if card_to_restore.has_method("return_to_hand"):
			card_to_restore.return_to_hand()
		elif card_to_restore.has_method("force_deselect"):
			# 备用方案：使用旧的force_deselect方法
			card_to_restore.force_deselect()
		else:
			pass
	else:
		pass

	# 清空暂存数据
	current_target_tile = null
	current_shape_coords = []
	current_card = null
	is_timeline_clear_mode = false


## 恢复鼠标过滤
func _restore_mouse_filters() -> void:
	_get_scene_interaction_lock_controller().restore_after_drag(_get_hex_map(), timeline_ui)

	# 恢复网格单元格鼠标交互
	_restore_grid_cells_mouse_filter()


## 右键取消选中
func _cancel_selection() -> void:
	if not is_dragging or is_placing:
		return


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


## 处理自由拖拽逻辑（鼠标不在时间轴上时）
## 卡牌自由跟随鼠标移动，不受网格约束
func _handle_free_drag(mouse_pos: Vector2) -> void:
	if is_timeline_clear_mode and timeline_ui:
		# clear 预览是即时效果的临时示意，鼠标离开时间轴后必须立刻清掉。
		# 这里不能只调用 clear_grid_preview()，因为重叠绿色需要一层盖在已有行动上方的预览节点。
		_clear_timeline_grid_preview()
		return

	# 计算卡牌中心的目标位置（保持鼠标相对于卡牌中心的偏移）
	var target_center = mouse_pos - drag_offset
	
	# 将卡牌中心位置转换为左上角位置
	var card_top_left = target_center
	if current_card.has_method("get_size"):
		var card_size = current_card.get_size()
		var card_scale = current_card.scale
		card_top_left -= card_size * card_scale / 2

	
	current_card.global_position = card_top_left
	if current_card.material:
		current_card.material.set_shader_parameter("is_invalid", false)
		current_card.material.set_shader_parameter("drag_visual_state", 0)

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
	)

# ==========================================
# 放置逻辑
# ==========================================


## 尝试将当前拖拽的形状放置到时间轴上
## 验证鼠标位置是否在时间轴内，计算网格坐标，检查放置有效性
## 有效时执行放置动画，无效时显示拒绝提示并返回手牌
func try_place_shape() -> void:
	
	var mouse_pos = get_global_mouse_position()
	var is_over_timeline = timeline_ui.get_global_rect().has_point(mouse_pos)

	if not is_over_timeline:
		_end_dragging()
		return

	# ★ 修复实际放置判定：与 _handle_timeline_hover 保持一致的坐标计算
	# 使用 get_local_mouse_position() 自动剔除父级缩放变换，解决视觉预览正确但实际放置位置偏移的 Bug
	var max_grid_x = 12  # TimelineManager.GRID_WIDTH
	var max_grid_y = 3   # TimelineManager.GRID_HEIGHT
	var grid_info: Dictionary = _get_timeline_grid_coordinate_resolver().resolve_fixed_bounds(
		timeline_ui,
		slot_size,
		spacing,
		max_grid_x,
		max_grid_y
	)
	var origin_pos: Vector2i = grid_info["grid_pos"]
	
	if not grid_info["is_in_bounds"]:
		_play_reject_animation()
		return
	
	# 预计算所有目标位置并记录
	if not current_shape_coords.is_empty():
		for offset in current_shape_coords:
			var target_pos = origin_pos + offset
	else:
		pass

	# 验证并放置
	if is_timeline_clear_mode:
		var clear_grid_width = timeline_ui.grid_width if _object_has_property(timeline_ui, &"grid_width") else max_grid_x
		var clear_grid_height = timeline_ui.grid_height if _object_has_property(timeline_ui, &"grid_height") else max_grid_y
		if TimelineClearEffectUtil.is_origin_in_bounds(current_shape_coords, origin_pos, clear_grid_width, clear_grid_height):
			_execute_timeline_clear(origin_pos)
		else:
			_play_reject_animation()
	elif timeline_manager and timeline_manager.is_placement_valid(current_shape_coords, origin_pos):
		_place_action(origin_pos)
	else:
		_play_reject_animation()
		# 放置失败后自动返回手牌（拒绝动画完成后会调用_end_dragging）


## 执行放置
func _place_action(origin_pos: Vector2i) -> void:

	# 播放放置动画，动画完成后会调用_finish_placement
	_play_placement_animation(origin_pos)


## 执行 clear 类即时卡牌。
## 这个分支不会调用 timeline_manager.place_action()，因此不会留下新的时间轴占位。
## 点击确认后先清除蓝/绿/红预览，再让 TimelineClearEffect 找出范围内已有行动并触发移除动画。
func _execute_timeline_clear(origin_pos: Vector2i) -> void:
	is_placing = true
	_restore_mouse_filters()

	if timeline_ui:
		# clear 的蓝/绿/红示意格包含覆盖层，确认施放前先完整移除，
		# 之后被命中的原有时间占位方格会各自播放渐隐下落动画。
		_clear_timeline_grid_preview()
	_clear_effect_preview()

	TimelineClearEffectUtil.execute(origin_pos, current_shape_coords, timeline_manager, timeline_ui)
	end_dragging_success()
	is_timeline_clear_mode = false
	is_placing = false


## 将普通数组转换为Vector2i数组
## 支持多种输入格式：Vector2i、[x, y]数组、{x: value, y: value}字典
func _convert_to_vector2i_array(raw_array: Array) -> Array[Vector2i]:
	return _get_card_shape_resolver().convert_to_vector2i_array(raw_array)

## 停止拖拽（右键取消时调用）


## 显示拒绝放置提示
func _show_reject_tooltip(message: String = "无法放置") -> void:
	_get_reject_tooltip_controller().show_tooltip(cursor_tooltip, message)
	
	# 更新提示框位置跟随鼠标
	call_deferred("_update_reject_tooltip_position")


## 更新拒绝提示位置
func _update_reject_tooltip_position() -> void:
	_get_reject_tooltip_controller().update_position(
		cursor_tooltip,
		get_global_mouse_position(),
		get_viewport_rect().size
	)


## 隐藏拒绝放置提示
func _hide_reject_tooltip() -> void:
	_get_reject_tooltip_controller().hide_tooltip(cursor_tooltip)


## 播放拒绝动画
func _play_reject_animation() -> void:
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
	is_placing = true

	# 禁用卡牌鼠标交互，防止与其他元素碰撞
	if is_instance_valid(current_card):
		current_card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		# 移除无效状态着色器
		if current_card.material:
			current_card.material.set_shader_parameter("is_invalid", false)
			current_card.material.set_shader_parameter("drag_visual_state", 0)

	# 禁用地块容器和时间轴UI的鼠标交互，防止意外触发
	var hex_map = _get_hex_map()
	if hex_map and hex_map is Control:
		hex_map.mouse_filter = Control.MOUSE_FILTER_IGNORE

	if timeline_ui and timeline_ui is Control:
		timeline_ui.mouse_filter = Control.MOUSE_FILTER_IGNORE

	# ★ 修复缩放导致的卡牌放置动画飞偏：直接使用 grid_cells 字典获取精准位置
	var grid_cell_center = Vector2.ZERO
	if _object_has_property(timeline_ui, &"grid_cells") and timeline_ui.grid_cells.has(grid_pos):
		var target_cell = timeline_ui.grid_cells[grid_pos]
		# target_cell.global_position 是绝对精准的（已包含所有父级变换）
		# size 乘以 timeline_ui 的实际 scale 获取真实视觉大小
		var actual_cell_size = target_cell.size * timeline_ui.scale
		grid_cell_center = target_cell.global_position + (actual_cell_size / 2.0) + float_offset
	else:
		# 兜底方案：使用鼠标位置（理论上不应该发生）
		grid_cell_center = get_global_mouse_position()
	
	# 将卡牌中心位置转换为左上角位置（注意卡牌的目标 scale 是 0.9）
	var card_top_left = grid_cell_center
	if current_card.has_method("get_size"):
		var card_size = current_card.get_size()
		var target_card_scale = Vector2(0.9, 0.9)  # 卡牌动画目标缩放
		card_top_left -= card_size * target_card_scale / 2

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
	
	if placement_success:
		player_action_placed.emit(action)
	else:
		# 放置失败，触发拒绝动画
		_play_reject_animation()
		# 这里不调用end_dragging_success，因为放置失败了
		is_placing = false
		return

	# 触发卡牌效果（如果有目标地块）
	_trigger_card_effect()
	
	# 清除时间轴网格预览
	_clear_timeline_grid_preview()

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
			# 注意：原apply_effect_immediate会销毁卡牌，但我们希望卡牌进入弃牌区
			# 这里只记录日志，不实际触发效果（效果将在时间轴结算时触发）
			# current_card.apply_effect_immediate(current_target_tile)  # 注释掉，避免销毁卡牌
			pass

			# 可以在这里发射信号让其他系统处理效果
			# effect_triggered.emit(current_card, current_target_tile)
		else:
			pass
	else:
		pass


## 成功结束拖拽
func end_dragging_success() -> void:
	is_dragging = false
	is_placing = false

	# 发射拖拽结束信号（用于时间轴可视化器）
	if is_instance_valid(current_card):
		drag_ended.emit(current_card, true)  # true表示已放置
		
		# ==========================================
		# ★ 核心修复 4：彻底剥离拖拽 Shader 与状态
		# 强制调用刚才完善的重置函数，洗掉所有拖拽特效
		# ==========================================
		if current_card.has_method("force_reset_visuals"):
			current_card.force_reset_visuals()
		
		# 将卡牌内部状态机硬重置为闲置状态
		if _object_has_property(current_card, &"card_current_state"):
			current_card.card_current_state = 0 # 0 对应 CustomCardState.IDLE
			
		current_card.mouse_filter = Control.MOUSE_FILTER_STOP
		current_card.modulate = Color.WHITE
		current_card.scale = Vector2.ONE
		
		# ========================================================
		# ★ 新增修复 2：将卡牌送入弃牌区前，彻底剥离拖拽Shader，恢复原始外观
		# ========================================================
		if _object_has_property(current_card, &"original_material"):
			current_card.material = current_card.original_material
			# 如果卡牌有正面贴图引用，一并恢复
			var tex_node = current_card.get("front_face_texture")
			if tex_node and _object_has_property(tex_node, &"material"):
				tex_node.material = current_card.original_material
				
		# 关闭底层卡牌框架的高亮逻辑，并恢复透明度
		if current_card.has_method("_set_shader"):
			current_card._set_shader(false) 
		if current_card.has_method("set_card_transparency"):
			current_card.set_card_transparency(1.0)
		# ========================================================

	# 处理弃牌逻辑
	if is_instance_valid(current_card):
		var discard_pile_node = _find_discard_pile()
		var main_board = _get_main_board()

		# 直接尝试移入弃牌区
		if is_instance_valid(discard_pile_node) and discard_pile_node.has_method("move_cards"):
			discard_pile_node.move_cards([current_card])
			
			# 触发弃牌效果
			if is_instance_valid(main_board) and main_board.has_method("_handle_discard_effects"):
				main_board.call_deferred("_handle_discard_effects", current_card)
		else:
			# 终极回退
			_return_card_to_hand(current_card)

	# 收起时间轴
	_get_timeline_ui_state_controller().collapse(timeline_ui)



## 禁用时间轴网格单元格鼠标交互
func _disable_grid_cells_mouse_filter() -> void:
	_get_grid_mouse_filter_controller().disable_grid_cells(timeline_ui)

## 恢复时间轴网格单元格鼠标交互
func _restore_grid_cells_mouse_filter() -> void:
	_get_grid_mouse_filter_controller().restore_grid_cells(timeline_ui)

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
	else:
		pass
	
	if timeline_ui.has_method("toggle_expand"):
		timeline_ui.toggle_expand()  # 回退方案

	# 隐藏提示
	if is_instance_valid(cursor_tooltip):
		cursor_tooltip.hide()

	# 清空暂存数据
	current_target_tile = null
	current_shape_coords = []
	current_card = null
	is_timeline_clear_mode = false


## 检查放置是否有效（公共方法，供时间轴可视化器调用）
func _is_placement_valid(grid_pos: Vector2i) -> bool:
	return _get_placement_query_service().is_placement_valid(
		timeline_ui,
		timeline_manager,
		current_shape_coords,
		grid_pos,
		is_timeline_clear_mode
	)

## ==========================================
## ★ 强制打断拖拽（供外部事件、回合结束时调用）
## ==========================================
func force_cancel_drag() -> void:
	if not is_dragging or not is_instance_valid(current_card):
		return
	
	
	# 清除时间轴预览网格与高亮。
	# clear 模式可能存在额外覆盖层，强制打断时也要一并清理。
	_clear_timeline_grid_preview()
	_clear_effect_preview()
	
	# 隐式调用内部清理逻辑，将卡牌移回手牌容器
	_end_dragging()
