extends CanvasLayer

## 动画配置
@export var transition_duration : float = 1.0  # 动画持续时间
@export var ui_scale : Vector2 = Vector2(1.5, 1.5) # 最终缩小的比例
@export var margin_top : float = 160.0          # 距离顶部像素

@onready var ring = $Ring

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
