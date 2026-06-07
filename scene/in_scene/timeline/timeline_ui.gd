extends Control

const ENEMY_INTENT_TIMELINE_SHADER: Shader = preload("res://shaders/enemy_intent_timeline_pulse.gdshader")
const TimelineActionShapeVisualScene = preload("res://scene/in_scene/timeline/TimelineActionShapeVisual.gd")
const TimelineExpandVisualControllerScript = preload("res://scene/in_scene/timeline/ui_modules/layout/TimelineExpandVisualController.gd")
const TimelineLayoutControllerScript = preload("res://scene/in_scene/timeline/ui_modules/layout/TimelineLayoutController.gd")
const TimelineGridBuilderScript = preload("res://scene/in_scene/timeline/ui_modules/grid/TimelineGridBuilder.gd")
const TimelineGridCellInteractionPresenterScript = preload("res://scene/in_scene/timeline/ui_modules/grid/TimelineGridCellInteractionPresenter.gd")
const TimelineGridPreviewPresenterScript = preload("res://scene/in_scene/timeline/ui_modules/grid/TimelineGridPreviewPresenter.gd")
const TimelineManagerLocatorScript = preload("res://scene/in_scene/timeline/ui_modules/bridges/TimelineManagerLocator.gd")
const TimelineEnemyIntentOverlayPresenterScript = preload("res://scene/in_scene/timeline/ui_modules/presenters/TimelineEnemyIntentOverlayPresenter.gd")
const TimelineBlockPlacementAnimatorScript = preload("res://scene/in_scene/timeline/ui_modules/animation/TimelineBlockPlacementAnimator.gd")
const TimelineActionRemovalGhostBuilderScript = preload("res://scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalGhostBuilder.gd")
const TimelineActionRemovalAnimatorScript = preload("res://scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalAnimator.gd")
const TimelineActionHoverStateControllerScript = preload("res://scene/in_scene/timeline/ui_modules/controllers/TimelineActionHoverStateController.gd")

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
## 时间轴敌人意图被 hover/选中时叠加的条纹颜色。
@export var enemy_intent_stripe_color: Color = Color(1.0, 0.85, 0.12, 1.0)
## 条纹移动速度。
@export var enemy_intent_stripe_speed: float = 3.8
## 条纹密度，越高条纹越密。
@export var enemy_intent_stripe_density: float = 12.0
## 条纹宽度，越高每条纹越粗。
@export var enemy_intent_stripe_width: float = 0.12
## 条纹混合强度。
@export_range(0.0, 1.0, 0.01) var enemy_intent_stripe_strength: float = 0.75
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
var _expand_visual_controller = null
var _layout_controller = null
var _grid_builder = null
var _grid_cell_interaction_presenter = null
var _grid_preview_presenter = null
var _timeline_manager_locator = null
var _enemy_intent_overlay_presenter = null
var _block_placement_animator = null
var _action_removal_ghost_builder = null
var _action_removal_animator = null
var _action_hover_state_controller = null

# 信号定义
signal grid_cell_clicked(grid_pos: Vector2i, is_right_click: bool)
signal grid_cell_right_clicked(grid_pos: Vector2i, is_right_click: bool)
signal grid_cell_hovered(grid_pos: Vector2i, is_hovering: bool)

# 如果需要跨脚本调用，可以在这里注入 manager 引用
var timeline_manager: TimelineManager


func _get_expand_visual_controller():
	if _expand_visual_controller == null:
		_expand_visual_controller = TimelineExpandVisualControllerScript.new()
	return _expand_visual_controller


func _get_layout_controller():
	if _layout_controller == null:
		_layout_controller = TimelineLayoutControllerScript.new()
	return _layout_controller


func _get_grid_builder():
	if _grid_builder == null:
		_grid_builder = TimelineGridBuilderScript.new()
	return _grid_builder


func _get_grid_cell_interaction_presenter():
	if _grid_cell_interaction_presenter == null:
		_grid_cell_interaction_presenter = TimelineGridCellInteractionPresenterScript.new()
	return _grid_cell_interaction_presenter


func _get_grid_preview_presenter():
	if _grid_preview_presenter == null:
		_grid_preview_presenter = TimelineGridPreviewPresenterScript.new()
	return _grid_preview_presenter


func _get_timeline_manager_locator():
	if _timeline_manager_locator == null:
		_timeline_manager_locator = TimelineManagerLocatorScript.new()
	return _timeline_manager_locator


func _get_enemy_intent_overlay_presenter():
	if _enemy_intent_overlay_presenter == null:
		_enemy_intent_overlay_presenter = TimelineEnemyIntentOverlayPresenterScript.new()
	return _enemy_intent_overlay_presenter


func _get_block_placement_animator():
	if _block_placement_animator == null:
		_block_placement_animator = TimelineBlockPlacementAnimatorScript.new()
	return _block_placement_animator


func _get_action_removal_ghost_builder():
	if _action_removal_ghost_builder == null:
		_action_removal_ghost_builder = TimelineActionRemovalGhostBuilderScript.new()
	return _action_removal_ghost_builder


func _get_action_removal_animator():
	if _action_removal_animator == null:
		_action_removal_animator = TimelineActionRemovalAnimatorScript.new()
	return _action_removal_animator


func _get_action_hover_state_controller():
	if _action_hover_state_controller == null:
		_action_hover_state_controller = TimelineActionHoverStateControllerScript.new()
	return _action_hover_state_controller


## 查找TimelineManager节点
func _find_timeline_manager() -> TimelineManager:
	return _get_timeline_manager_locator().find_timeline_manager(self)


## 应用调试用的offset设置
func _apply_anchor_layout() -> void:
	_get_layout_controller().apply_anchor_layout(
		self,
		grid_background,
		grid_width,
		grid_height,
		slot_size,
		spacing,
		margin_top_preset,
		top_reserved_space
	)


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
	background_mask = _get_expand_visual_controller().create_background_mask(self, mask_color, mask_layer)



func _init_background_grid():
	grid_cells = _get_grid_builder().build_background_grid(
		grid_background,
		grid_width,
		grid_height,
		slot_size,
		spacing,
		grid_cell_default_color,
		_on_grid_cell_gui_input,
		_on_grid_cell_mouse_entered,
		_on_grid_cell_mouse_exited
	)


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
			overlay.material = _get_enemy_intent_overlay_presenter().create_overlay_material(
				enemy_intent_timeline_shader,
				enemy_intent_pulse_speed,
				enemy_intent_pulse_min_alpha,
				enemy_intent_pulse_max_alpha,
				enemy_intent_stripe_color,
				enemy_intent_stripe_speed,
				enemy_intent_stripe_density,
				enemy_intent_stripe_width,
				enemy_intent_stripe_strength
			)
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
	
	_get_expand_visual_controller().animate_background_mask(
		tw,
		background_mask,
		is_expanded,
		anim_duration,
		func(): return not is_expanded
	)
	
	_get_expand_visual_controller().set_map_interaction_for_expand(self, is_expanded)
	
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
	hovered_action = _get_action_hover_state_controller().enter_hover(action, timeline_manager)


func _on_block_exited():
	hovered_action = _get_action_hover_state_controller().exit_hover(hovered_action, timeline_manager)


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
		hovered_action = _get_action_hover_state_controller().clear_removed_action_hover(
			hovered_action,
			action,
			timeline_manager
		)

	var ghost = _create_action_removal_ghost(container, action_id, reason)
	_strip_action_container_runtime_effects(container)
	container.queue_free()
	action_containers.erase(action_id)

	if not is_instance_valid(ghost):
		return

	_get_action_removal_animator().animate_removal(
		ghost,
		func(): return create_tween(),
		action_removal_fade_color,
		action_removal_drop_distance,
		enemy_intent_removal_scale,
		enemy_intent_removal_duration
	)


## 根据当前 action 容器生成一份无 Shader、无 Overlay、无鼠标交互的视觉残影。
## 残影复制方块几何、StyleBox 颜色和 TimelineActionShapeVisual 的纯绘制参数，
## 但不复制任何子节点材质，确保清除动画是干净且仍保留“同一意图整体感”的一版。
func _create_action_removal_ghost(source_container: Control, action_id: int, reason: String = "") -> Control:
	return _get_action_removal_ghost_builder().create_ghost(
		source_container,
		action_id,
		reason,
		enemy_intent_preview_z_index
	)


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
	_get_enemy_intent_overlay_presenter().set_overlay_visible(
		container,
		visible,
		color,
		enemy_intent_stripe_color,
		enemy_intent_stripe_speed,
		enemy_intent_stripe_density,
		enemy_intent_stripe_width,
		enemy_intent_stripe_strength
	)


## 将导出的时间轴意图条纹参数写入 shader。
## 核心逻辑：每个敌人意图格子的 Overlay 都独立持有 ShaderMaterial，hover 时统一刷新参数即可。
func _configure_enemy_intent_timeline_material(material: Material) -> void:
	_get_enemy_intent_overlay_presenter().configure_material(
		material,
		enemy_intent_stripe_color,
		enemy_intent_stripe_speed,
		enemy_intent_stripe_density,
		enemy_intent_stripe_width,
		enemy_intent_stripe_strength
	)


# ==========================================
# ★ 网格单元交互处理
# ==========================================
func _on_grid_cell_gui_input(event: InputEvent, cell_index: int) -> void:
	if event is InputEventMouseButton and event.pressed:
		var grid_pos = _get_grid_cell_interaction_presenter().get_grid_pos(cell_index, grid_width)


		# 发射信号，通知其他系统这个网格被点击了
		# 可以用于快速放置卡牌或显示信息
		if event.button_index == MOUSE_BUTTON_LEFT:
			# 左键点击：尝试放置卡牌或显示信息
			emit_signal("grid_cell_clicked", grid_pos, false)
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			# 右键点击：清除该位置的行动或显示详细
			emit_signal("grid_cell_right_clicked", grid_pos, true)


func _on_grid_cell_mouse_entered(cell_index: int) -> void:
	var grid_pos = _get_grid_cell_interaction_presenter().get_grid_pos(cell_index, grid_width)

	# 高亮显示这个网格单元
	_get_grid_cell_interaction_presenter().apply_cell_color(grid_cells, grid_pos, grid_cell_hover_color)

	emit_signal("grid_cell_hovered", grid_pos, true)


func _on_grid_cell_mouse_exited(cell_index: int) -> void:
	var grid_pos = _get_grid_cell_interaction_presenter().get_grid_pos(cell_index, grid_width)

	# 恢复网格单元颜色
	_get_grid_cell_interaction_presenter().apply_cell_color(grid_cells, grid_pos, grid_cell_default_color)

	emit_signal("grid_cell_hovered", grid_pos, false)


## 动画：时间轴方格放置后变蓝色效果
func _animate_block_placement(block: Panel, target_color: Color) -> void:
	_get_block_placement_animator().animate_block_placement(
		block,
		target_color,
		func(): return create_tween()
	)


## 强制收起时间轴（用于卡牌放置后自动返回上方）
func collapse() -> void:
	if not is_expanded:
		return
	
	is_expanded = false
	var tw = create_tween().set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	
	_get_expand_visual_controller().animate_background_mask(
		tw,
		background_mask,
		false,
		anim_duration,
		func(): return not is_expanded
	)
	
	# 仅缩放回归即可
	tw.parallel().tween_property(self, "scale", Vector2.ONE, anim_duration)
	
	tw.tween_callback(func():
		_apply_anchor_layout()
	)


## 更新网格预览：根据形状和位置显示蓝色/红色格子，并检测敌人意图
func update_grid_preview(shape_coords: Array[Vector2i], origin_pos: Vector2i, is_valid: bool) -> void:
	_get_grid_preview_presenter().update_grid_preview(
		grid_cells,
		timeline_manager,
		shape_coords,
		origin_pos,
		is_valid,
		grid_width,
		grid_height,
		grid_cell_default_color,
		func(): return create_tween()
	)


## 清除所有网格预览效果
func clear_grid_preview() -> void:
	_get_grid_preview_presenter().clear_grid_preview(grid_cells, grid_cell_default_color)


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
