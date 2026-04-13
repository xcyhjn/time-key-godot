extends ColorRect

@export var final_clock_radius: float = 0.225
@export var initial_state: float = 1.0

func _ready() -> void:
	_update_shader_screen_size(initial_state)

func _update_shader_screen_size(_state):
	var screen_size = get_viewport().get_visible_rect().size
	self.material.set_shader_parameter("screen_width", screen_size.x)
	self.material.set_shader_parameter("screen_height", screen_size.y)
	self.material.set_shader_parameter("circle_size", _state)

# --- 动画函数 ---
func start_iris_in(state):
	_update_shader_screen_size(state)
	var tween = create_tween()
	tween.tween_property(self.material, "shader_parameter/circle_size", final_clock_radius, 1.5)\
		.set_trans(Tween.TRANS_QUART)\
		.set_ease(Tween.EASE_OUT)
	await tween.finished
	await get_tree().create_timer(0.25).timeout

## 黑幕打开：从当前的圆圈大小扩张到全屏
func start_iris_out(state):
	_update_shader_screen_size(state)
	var tween = create_tween()
	tween.tween_property(self.material, "shader_parameter/circle_size", 1.0, 1.0)\
		.set_trans(Tween.TRANS_EXPO)\
		.set_ease(Tween.EASE_OUT)
	await tween.finished
	self.hide()
