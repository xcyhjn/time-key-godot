extends Control

const ENEMY_INTENT_TIMELINE_SHADER: Shader = preload("res://shaders/enemy_intent_timeline_pulse.gdshader")
const TimelineActionShapeVisualScene = preload("res://scene/in_scene/timeline/TimelineActionShapeVisual.gd")

@export_group("Grid Settings")
@export var slot_size: float = 40.0  # 格子大小，应与DragShapeController的slot_size一致
@export var spacing: float = 2.0  # 格子间距，应与DragShapeController的spacing一致
@export var grid_width: int = 12
@export var grid_height: int = 3

@export_group("Grid Visual Settings")
## 时间轴空格子的默认底色。Alpha 越低越透明；RGB 越高越浅。
@export var grid_cell_default_color: Color = Color(0.4, 0.4, 0.4, 0.6)
## 鼠标悬停在空格子上时的底色。
@export var grid_cell_hover_color: Color = Color(0.5, 0.5, 0.5, 0.75)

@export_group("Layout Settings")
@export var margin_top_preset: float = 0.0      # 紧贴屏幕最上沿的距离 (设为0即死死贴住)
## 额外预留给顶部 UI 的空间。
## 这个值主要由外部（例如局内的 CartoonUI）在运行时注入，
## 用来把时间轴整体往下压，避免与顶部 HUD 重叠。
@export var top_reserved_space: float = 0.0
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
## 时间占位方格因为丢失目标、意图失效等“非回合结算原因”被移除时的下落距离。
## 这个动画只播放在新生成的无 Shader 残影上，原占位节点会先解除材质和鼠标互动。
@export var action_removal_drop_distance: float = 14.0
## 时间占位方格失效消失时的最终颜色。RGB 偏暗，Alpha 为 0，形成“变暗后淡出”的感觉。
@export var action_removal_fade_color: Color = Color(0.28, 0.28, 0.28, 0.0)

@export_group("行动整体轮廓")
## 是否启用“同一个意图看起来像一个整体”的视觉层。
## 关闭后会退回到单格 Panel 的显示方式，方便你对比新旧效果。
@export var action_group_visual_enabled: bool = true
## 整体外轮廓宽度。数值越大，不同意图贴在一起时的分界越明显。
@export var action_group_outline_width: float = 3.0
## 整体外轮廓颜色。这里使用接近黑色但保留一点透明度，避免时间轴变得过重。
@export var action_group_outline_color: Color = Color(0.03, 0.03, 0.03, 0.95)
## 同一意图内部相邻格子的弱分割线宽度。设为 0 可以完全隐藏内部格线。
@export var action_group_internal_seam_width: float = 1.0
## 同一意图内部弱分割线颜色。默认很淡，只提示占了几个格子，不抢外轮廓的信息层级。
@export var action_group_internal_seam_color: Color = Color(1.0, 1.0, 1.0, 0.14)

@export_group("卡牌遮罩设置")
@export var mask_color: Color = Color(0.75, 0.75, 0.75, 0.6)  # 浅灰色半透明遮罩
@export var mask_layer: int = 100  # 遮罩层级，低于时间轴但高于其他内容

@onready var grid_background = $GridBackground
@onready var shape_layer = $ShapeLayer
@onready var timeline_intro_animator: TimelineIntroAnimator = get_node_or_null("TimelineIntroAnimator") as TimelineIntroAnimator
var background_mask: ColorRect  # 遮罩节点（运行时创建）


var is_expanded: bool = false
var hovered_action: TimelineAction = null
var grid_cells: Dictionary = { }  # 存储网格单元引用，键：Vector2i，值：ColorRect
var allow_click_to_expand: bool = false  # 是否允许通过点击缩放时间轴（常态下禁用，仅卡牌选中时启用）
var action_containers: Dictionary = {}
var current_enemy_intent_preview_action: TimelineAction = null
var enemy_intent_preview_tween: Tween = null
var _timeline_intro_has_played: bool = false
var _timeline_intro_in_progress: bool = false

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
	var effective_top_offset = margin_top_preset + top_reserved_space
	
	# 3. 设置精确的偏移量（完美贴身包裹，彻底解决偏左问题）
	offset_left = -actual_width / 2.0
	offset_right = actual_width / 2.0
	offset_top = effective_top_offset
	offset_bottom = effective_top_offset + actual_height
	
	# 4. 强制底层立刻刷新布局，防止 size 计算滞后
	force_update_transform()
	
	# 5. 精确设置缩放中心为【自身顶部正中心】
	pivot_offset = Vector2(actual_width / 2.0, 0.0)
	
	# 6. 确保内部网格背景贴死左上角 (消除内部误差)
	if is_instance_valid(grid_background):
		grid_background.position = Vector2.ZERO
		grid_background.size = size


## 外部接口：设置顶部需要额外避让的像素高度。
## 例如局内把 CartoonUI 放到最上方后，就可以把该 UI 的占位高度传进来，
## 让时间轴整体向下移动而不需要手动改 tscn 偏移。
func set_top_reserved_space(px: float) -> void:
	top_reserved_space = max(px, 0.0)
	_apply_anchor_layout()
	


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
	_setup_timeline_intro_animator()
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
		default_style.bg_color = grid_cell_default_color
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


## 初始化时间轴入场动画子节点。
## TimelineUI 只负责把自己创建好的格子/行动容器交给动画器，具体节奏与开关都在子节点导出参数里调。
func _setup_timeline_intro_animator() -> void:
	if not is_instance_valid(timeline_intro_animator):
		return

	timeline_intro_animator.setup(self, grid_background, shape_layer)
	timeline_intro_animator.prepare_grid_for_intro(grid_cells)


## 外部流程入口：播放时间轴背景格子的左下角涟漪入场。
## 返回时表示背景格子已经固定在最终位置，后续再生成敌人意图会更干净。
func play_intro() -> void:
	if not is_instance_valid(timeline_intro_animator):
		return
	if _timeline_intro_in_progress:
		await timeline_intro_animator.grid_intro_finished
		return
	if _timeline_intro_has_played and timeline_intro_animator.play_grid_intro_once:
		return

	_timeline_intro_in_progress = true
	await timeline_intro_animator.play_grid_intro(grid_cells, grid_width, grid_height)
	_timeline_intro_in_progress = false
	_timeline_intro_has_played = true


func is_intro_in_progress() -> bool:
	return _timeline_intro_in_progress


## 将 action.shape_coords 转成 shape_container 内部使用的局部坐标。
## TimelineAction 允许 shape 坐标里存在负值或不从 0 开始；容器已经按 min_x / min_y 放到了正确位置，
## 因此绘制整体轮廓时只需要从左上角重新归一化，避免视觉层和 Panel 方格发生偏移。
func _get_local_shape_coords(shape_coords: Array[Vector2i], min_x: int, min_y: int) -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for coord in shape_coords:
		result.append(Vector2i(coord.x - min_x, coord.y - min_y))
	return result


## 给单个 TimelineAction 容器添加整体视觉层。
## BACKPLATE 放在方格下方，负责连接同意图内部空隙；OUTLINE 放在方格上方，负责外轮廓和淡内线。
func _add_action_shape_visual(
	shape_container: Control,
	local_shape_coords: Array[Vector2i],
	action_color: Color,
	visual_mode: int
) -> void:
	if not is_instance_valid(shape_container) or local_shape_coords.is_empty():
		return

	var visual := TimelineActionShapeVisualScene.new() as TimelineActionShapeVisual
	visual.name = "ActionBackplateVisual" if visual_mode == TimelineActionShapeVisual.VisualMode.BACKPLATE else "ActionOutlineVisual"
	visual.size = shape_container.size
	visual.custom_minimum_size = shape_container.size
	visual.mouse_filter = Control.MOUSE_FILTER_IGNORE
	visual.z_index = 20 if visual_mode == TimelineActionShapeVisual.VisualMode.OUTLINE else 0
	visual.setup(
		visual_mode,
		local_shape_coords,
		slot_size,
		spacing,
		action_color,
		action_group_outline_color,
		action_group_outline_width,
		action_group_internal_seam_color,
		action_group_internal_seam_width
	)
	shape_container.add_child(visual)


# ==========================================
# ★ UI 绘制：方格保留交互，整体轮廓负责表达同一意图的归属
# ==========================================
func _on_action_placed(action: TimelineAction):
	var shape_container = Control.new()
	shape_layer.add_child(shape_container)
	var use_action_intro := _should_play_action_intro(action)

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

	var local_shape_coords := _get_local_shape_coords(action.shape_coords, min_x, min_y)
	if action_group_visual_enabled:
		# 底板先于 Panel 加入，专门填补同一意图内部的 spacing 缝隙。
		# 这样 "11"、"11,1" 等多格意图会先有一个连起来的整体色块。
		_add_action_shape_visual(
			shape_container,
			local_shape_coords,
			action.color,
			TimelineActionShapeVisual.VisualMode.BACKPLATE
		)

	# 为所有方块设置统一样式。
	# 强黑边改由整体轮廓层负责；单个 Panel 不再画黑框，避免内部相邻格被误读成不同意图。
	var style = StyleBoxFlat.new()
	style.bg_color = action.color
	if action_group_visual_enabled:
		style.set_border_width_all(0)
		style.border_color = Color.TRANSPARENT
	else:
		style.set_border_width_all(2)
		style.border_color = Color.BLACK

	for offset in action.shape_coords:
		var target_grid_pos = action.origin_grid_pos + offset
		var block = Panel.new()
		block.name = "ActionBlock_%s_%s" % [target_grid_pos.x, target_grid_pos.y]
		block.add_theme_stylebox_override("panel", style)
		block.size = Vector2(slot_size, slot_size)
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

		# 意图入场动画由 TimelineIntroAnimator 统一驱动整个容器；
		# 普通玩家放置仍沿用原逐格弹出，保持手感不变。
		if not use_action_intro:
			_animate_block_placement(block, action.color)

	if action_group_visual_enabled:
		# 外轮廓最后加入，确保它压在方格和敌人意图 pulse overlay 之上。
		# 它只画整个形状外侧，内部相邻边仅保留很淡的 seam，归属关系会更清楚。
		_add_action_shape_visual(
			shape_container,
			local_shape_coords,
			action.color,
			TimelineActionShapeVisual.VisualMode.OUTLINE
		)

	if use_action_intro:
		timeline_intro_animator.play_action_intro(shape_container, action, grid_width, grid_height, true)


func _on_timeline_cleared():
	clear_enemy_intent_preview()
	if is_instance_valid(timeline_intro_animator):
		timeline_intro_animator.stop_action_intros()
	action_containers.clear()
	for child in shape_layer.get_children():
		child.queue_free()


func _should_play_action_intro(action: TimelineAction) -> bool:
	if not is_instance_valid(timeline_intro_animator):
		return false
	return timeline_intro_animator.should_animate_action(action)


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
## 旧接口保留给已有调用使用，内部转到通用的时间占位移除动画。
func animate_enemy_intent_removal(action: TimelineAction) -> void:
	animate_action_removal(action, "enemy_intent_invalid")


## 通用时间占位移除动画。
## 设计重点：
## - 原 action 容器可能正处在 hover 放大、敌人意图 pulse shader、地图联动高亮等状态。
## - 移除时先生成一份“纯 Panel + 整体轮廓绘制层”的残影，残影不复制任何 ShaderMaterial / Overlay。
## - 原容器随后立刻禁用交互并 queue_free，避免正在播放的 shader 参与淡出动画或留下幽灵输入。
func animate_action_removal(action: TimelineAction, reason: String = "") -> void:
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

	if hovered_action == action:
		if timeline_manager and timeline_manager.has_signal("action_hovered_changed"):
			timeline_manager.action_hovered_changed.emit(action, false)
		hovered_action = null

	var ghost = _create_action_removal_ghost(container, action_id, reason)
	_strip_action_container_runtime_effects(container)
	container.queue_free()
	action_containers.erase(action_id)

	if not is_instance_valid(ghost):
		return

	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN)
	tw.tween_property(ghost, "modulate", action_removal_fade_color, enemy_intent_removal_duration)
	tw.parallel().tween_property(ghost, "position:y", ghost.position.y + action_removal_drop_distance, enemy_intent_removal_duration)
	tw.parallel().tween_property(ghost, "scale", enemy_intent_removal_scale, enemy_intent_removal_duration)
	tw.tween_callback(func():
		if is_instance_valid(ghost):
			ghost.queue_free()
	)


## 根据当前 action 容器生成一份无 Shader、无 Overlay、无鼠标交互的视觉残影。
## 残影复制方块几何、StyleBox 颜色和 TimelineActionShapeVisual 的纯绘制参数，
## 但不复制任何子节点材质，确保清除动画是干净且仍保留“同一意图整体感”的一版。
func _create_action_removal_ghost(source_container: Control, action_id: int, reason: String = "") -> Control:
	if not is_instance_valid(source_container):
		return null

	var parent = source_container.get_parent()
	if parent == null:
		return null

	var ghost = Control.new()
	ghost.name = "ActionRemovalGhost_%s" % action_id
	ghost.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ghost.size = source_container.size
	ghost.position = source_container.position
	ghost.pivot_offset = source_container.pivot_offset
	ghost.scale = Vector2.ONE
	ghost.modulate = Color.WHITE
	ghost.z_index = max(source_container.z_index, enemy_intent_preview_z_index + 1)
	ghost.set_meta("action_id", action_id)
	ghost.set_meta("removal_reason", reason)
	parent.add_child(ghost)
	parent.move_child(ghost, parent.get_child_count() - 1)

	for child in source_container.get_children():
		if child is TimelineActionShapeVisual:
			var source_visual := child as TimelineActionShapeVisual
			ghost.add_child(source_visual.clone_visual())
			continue

		if not (child is Panel):
			continue

		var source_block := child as Panel
		var ghost_block := Panel.new()
		ghost_block.mouse_filter = Control.MOUSE_FILTER_IGNORE
		ghost_block.position = source_block.position
		ghost_block.size = source_block.size
		ghost_block.custom_minimum_size = source_block.custom_minimum_size
		ghost_block.pivot_offset = source_block.pivot_offset
		ghost_block.scale = source_block.scale
		ghost_block.rotation = source_block.rotation

		var source_style = source_block.get_theme_stylebox("panel")
		if source_style != null:
			ghost_block.add_theme_stylebox_override("panel", source_style.duplicate())

		ghost.add_child(ghost_block)

	return ghost


## 递归卸载 action 容器上的运行时表现：
## - 清掉 CanvasItem.material，避免 pulse / hover shader 继续参与渲染；
## - 关闭 EnemyIntentOverlay，避免残留发光层；
## - 禁用鼠标输入，避免移除中的节点继续触发 hover。
func _strip_action_container_runtime_effects(node: Node) -> void:
	if node is CanvasItem:
		(node as CanvasItem).material = null
	if node is Control:
		(node as Control).mouse_filter = Control.MOUSE_FILTER_IGNORE
	if node is ColorRect and node.name == "EnemyIntentOverlay":
		(node as ColorRect).visible = false

	for child in node.get_children():
		_strip_action_container_runtime_effects(child)


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
		style.bg_color = grid_cell_hover_color
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
		style.bg_color = grid_cell_default_color
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
			style.bg_color = grid_cell_default_color
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
