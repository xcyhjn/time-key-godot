extends CanvasLayer

## 动画配置
@export var transition_duration : float = 1.0  # 动画持续时间
@export var ui_scale : Vector2 = Vector2(1.5, 1.5) # 最终缩小的比例
@export var margin_top : float = 160.0          # 距离顶部像素

@onready var ring = $Ring
@onready var pole = $Pole
@onready var rect = $Rect
@onready var menuui = $MenuUI/Control
@onready var pole_target_scale_y : float = pole.scale.x if pole else 1.0
@onready var rect_target_scale_y : float = rect.scale.x if pole else 1.0

var clock_node : Node2D = null

func move_clock_to_ui(target_clock: Node2D):
	if not target_clock: return
	clock_node = target_clock
	var screen_width = get_viewport().get_visible_rect().size.x
	var target_pos = Vector2(screen_width / 2.0, margin_top)
	var current_global_pos = clock_node.global_position

	if clock_node.get_parent() != self:
		clock_node.get_parent().remove_child(clock_node)
		add_child(clock_node)
		clock_node.global_position = current_global_pos

	if clock_node.has_method("change"):
		clock_node.change(0)

	var tween = create_tween()
	tween.set_parallel(true)
	tween.set_trans(Tween.TRANS_QUINT)
	tween.set_ease(Tween.EASE_OUT)
	
	tween.tween_property(clock_node, "global_position", target_pos, transition_duration)
	tween.tween_property(clock_node, "scale", ui_scale, transition_duration)
	tween.tween_property(clock_node, "rotation", 0.0, transition_duration)
	tween.set_parallel(false)
	
	if ring:
		tween.tween_property(ring, "modulate:a", 1.0, 0.5)\
			.set_trans(Tween.TRANS_SINE)\
			.set_ease(Tween.EASE_IN_OUT)
	
	if rect:
		rect.scale.y = 0.0
		rect.modulate.a = 1.0 
		tween.tween_property(rect, "scale:y", rect_target_scale_y, 0.5)\
			.set_trans(Tween.TRANS_BOUNCE) \
			.set_ease(Tween.EASE_OUT) # 使用 TRANS_BACK 会带有一点自然的弹性展开效果
	
	if pole:
		pole.scale.y = 0.0
		pole.modulate.a = 1.0 
		tween.tween_property(pole, "scale:y", pole_target_scale_y, 0.5)\
			.set_trans(Tween.TRANS_BACK) \
			.set_ease(Tween.EASE_OUT) # 使用 TRANS_BACK 会带有一点自然的弹性展开效果
			
	if menuui:
		tween.tween_property(menuui, "modulate:a", 1.0, 0.5)\
			.set_trans(Tween.TRANS_SINE)\
			.set_ease(Tween.EASE_IN_OUT)

func snap_clock_to_ui(target_clock: Node2D):
	if not target_clock: return
	clock_node = target_clock
	var screen_width = get_viewport().get_visible_rect().size.x
	var target_pos = Vector2(screen_width / 2.0, margin_top)
	
	if clock_node.get_parent() != self:
		if clock_node.get_parent():
			clock_node.get_parent().remove_child(clock_node)
		add_child(clock_node)
	
	clock_node.global_position = target_pos
	clock_node.scale = ui_scale
	clock_node.rotation = 0.0
	
	if clock_node.has_method("change"):
		clock_node.change(0)

	if ring: ring.modulate.a = 1.0
	if menuui: menuui.modulate.a = 1.0
	
	if rect:
		rect.modulate.a = 1.0
		rect.scale.y = rect_target_scale_y

	if pole:
		pole.modulate.a = 1.0
		pole.scale.y = pole_target_scale_y
