class_name HeightViewIndicatorPresenter
extends RefCounted

const INDICATOR_LINE_META := "height_indicator_line"
const INDICATOR_LABEL_META := "height_indicator_label"
const PILLAR_META := "height_view_pillar"

var _pillar_tweens: Dictionary = {}


## 为一个地块创建高度视图指示器。
## HexMap 负责决定何时进入平铺视图；本模块只根据传入配置生成光柱和数字标签。
func create_indicator(stack: Area2D, original_height: int, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return

	remove_indicator(stack)

	var sprites := _get_stack_sprites(stack)
	if sprites.is_empty():
		return
	if original_height <= 0 or original_height - 1 >= sprites.size():
		return

	var top_sprite = sprites[original_height - 1]
	if not is_instance_valid(top_sprite):
		return

	var pillar_position := _get_indicator_position(top_sprite, original_height, config)
	if bool(config.get("show_pillars", true)):
		_create_pillar(stack, original_height, pillar_position, config)
	if bool(config.get("show_labels", true)):
		_create_label(stack, original_height, pillar_position, config)


## 移除一个地块上的高度视图指示器。
## 退出平铺视图、地块销毁或重新创建指示器前都应调用，避免旧光柱/标签残留。
func remove_indicator(stack: Area2D) -> void:
	if not is_instance_valid(stack):
		return

	_kill_pillar_tween(stack)
	_queue_meta_node_free(stack, INDICATOR_LINE_META)
	_queue_meta_node_free(stack, INDICATOR_LABEL_META)
	if stack.has_meta(PILLAR_META):
		stack.set_meta(PILLAR_META, null)


## 启动光柱 hover 浮动动画。
## 只有当前处于平铺视图时才会播放，防止普通 3D 视图下误动指示器。
func start_pillar_floating(stack: Area2D, config: Dictionary) -> void:
	if not bool(config.get("is_flat_view", false)) or not is_instance_valid(stack):
		return

	var pillar = stack.get_meta(PILLAR_META) if stack.has_meta(PILLAR_META) else null
	if not is_instance_valid(pillar):
		return

	_kill_pillar_tween(stack)

	var original_y := float(pillar.position.y)
	var hover_speed := maxf(0.01, float(config.get("hover_speed", 1.5)))
	var hover_amplitude := float(config.get("hover_amplitude", 15.0))
	var tween: Tween = pillar.create_tween()
	tween.set_loops()
	tween.set_trans(Tween.TRANS_SINE)
	tween.set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(pillar, "position:y", original_y - hover_amplitude, hover_speed / 2.0)
	tween.tween_property(pillar, "position:y", original_y + hover_amplitude, hover_speed)
	tween.tween_property(pillar, "position:y", original_y, hover_speed / 2.0)
	_pillar_tweens[stack] = tween


## 停止光柱 hover 浮动动画。
## 当前行为保持旧逻辑：停止循环，并以短 tween 回到停止瞬间的位置，避免额外改变视觉基准。
func stop_pillar_floating(stack: Area2D, config: Dictionary) -> void:
	if not bool(config.get("is_flat_view", false)) or not is_instance_valid(stack):
		return

	_kill_pillar_tween(stack)
	var pillar = stack.get_meta(PILLAR_META) if stack.has_meta(PILLAR_META) else null
	if not is_instance_valid(pillar):
		return

	var original_y := float(pillar.position.y)
	var restore_tween: Tween = pillar.create_tween()
	restore_tween.set_trans(Tween.TRANS_SINE)
	restore_tween.set_ease(Tween.EASE_IN_OUT)
	restore_tween.tween_property(pillar, "position:y", original_y, 0.2)


## 更新并播放高度数字变化反馈。
## 地形升降在平铺视图下只改变数字标签；调用方可以 await 本函数等待反馈播完。
func animate_label_height(stack: Area2D, visual_height: int, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return

	var label = stack.get_meta(INDICATOR_LABEL_META) if stack.has_meta(INDICATOR_LABEL_META) else null
	if not is_instance_valid(label):
		return

	label.text = str(visual_height)
	var step_duration := maxf(0.01, float(config.get("label_bounce_duration", 0.15)))
	var tween: Tween = label.create_tween()
	tween.set_trans(Tween.TRANS_BACK)
	tween.set_ease(Tween.EASE_OUT)
	tween.tween_property(label, "scale", Vector2(1.8, 1.8), step_duration)
	tween.parallel().tween_property(label, "modulate", Color(1.0, 0.2, 0.2), step_duration)
	tween.tween_property(label, "scale", Vector2.ONE, step_duration)
	tween.parallel().tween_property(label, "modulate", Color.WHITE, step_duration)
	await tween.finished


## 安全读取 stack 上记录的渲染 sprite 列表。
## 指示器只需要用它找顶层 sprite 的位置，不负责维护 sprites 元数据。
func _get_stack_sprites(stack: Area2D) -> Array:
	if not is_instance_valid(stack) or not stack.has_meta("sprites"):
		return []

	var sprites: Variant = stack.get_meta("sprites")
	if sprites is Array:
		return sprites
	return []


## 计算光柱/数字标签的基础锚点。
## 平铺视图固定跟随 hitbox；3D 视图则使用顶层 sprite 的 y 值兼容旧路径。
func _get_indicator_position(top_sprite: Node, _height: int, config: Dictionary) -> Vector2:
	var hitbox_offset := Vector2(
		float(config.get("hitbox_offset_x", 0.0)),
		float(config.get("hitbox_offset_y", -110.0))
	)
	var pillar_offset: Vector2 = config.get("pillar_offset", Vector2.ZERO)
	if bool(config.get("is_flat_view", false)):
		return hitbox_offset + pillar_offset

	var top_y := 0.0
	if top_sprite is Node2D:
		top_y = (top_sprite as Node2D).position.y
	elif top_sprite is Control:
		top_y = (top_sprite as Control).position.y
	return Vector2(hitbox_offset.x, top_y + hitbox_offset.y) + pillar_offset


## 创建光柱 Line2D 并记录到 stack meta。
## 光柱长度保持旧公式：随高度增长，但至少保留 1.5 倍基础长度以保证可见。
func _create_pillar(stack: Area2D, original_height: int, position: Vector2, config: Dictionary) -> void:
	var line := Line2D.new()
	line.name = "HeightIndicatorLine"
	line.width = float(config.get("pillar_width", 5.0))
	line.default_color = config.get("pillar_color", Color(1.0, 1.0, 1.0, 0.8))

	var base_length := float(config.get("pillar_length", 50.0))
	var pillar_length := maxf(base_length * original_height * 0.5, base_length * 1.5)
	line.points = PackedVector2Array([Vector2.ZERO, Vector2(0.0, -pillar_length)])
	line.position = position
	line.z_index = int(config.get("pillar_z_index", 1000))
	stack.add_child(line)
	stack.set_meta(INDICATOR_LINE_META, line)
	stack.set_meta(PILLAR_META, line)


## 创建高度数字 Label 并记录到 stack meta。
## Label 禁用鼠标输入，避免在平铺视图中遮挡地块 Area2D 的 hover/click。
func _create_label(stack: Area2D, original_height: int, position: Vector2, config: Dictionary) -> void:
	var label := Label.new()
	label.name = "HeightIndicatorLabel"
	label.text = str(original_height)
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.focus_mode = Control.FOCUS_NONE

	var label_font := config.get("label_font", null) as Font
	if label_font != null:
		label.add_theme_font_override("font", label_font)

	label.add_theme_font_size_override("font_size", int(config.get("label_font_size", 32)))
	label.add_theme_color_override("font_color", config.get("label_color", Color.WHITE))
	label.add_theme_color_override("font_outline_color", config.get("label_outline_color", Color.BLACK))
	label.add_theme_constant_override("outline_size", int(config.get("label_outline_size", 2)))
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.position = position + (config.get("label_offset", Vector2(-10.0, -40.0)) as Vector2)
	label.z_index = int(config.get("label_z_index", 1001))
	stack.add_child(label)
	stack.set_meta(INDICATOR_LABEL_META, label)


## 释放 stack meta 指向的节点。
## 使用 set_meta(null) 保持旧代码的 meta 读写习惯，不额外改变外部检查方式。
func _queue_meta_node_free(stack: Area2D, meta_key: String) -> void:
	if not stack.has_meta(meta_key):
		return

	var node = stack.get_meta(meta_key)
	if is_instance_valid(node):
		node.queue_free()
	stack.set_meta(meta_key, null)


## 停止并移除某个 stack 的光柱 tween 引用。
## 这个字典由 presenter 自己维护，避免 HexMap 再持有表现层 tween 状态。
func _kill_pillar_tween(stack: Area2D) -> void:
	if not _pillar_tweens.has(stack):
		return

	var tween = _pillar_tweens[stack]
	if is_instance_valid(tween):
		tween.stop()
	_pillar_tweens.erase(stack)
