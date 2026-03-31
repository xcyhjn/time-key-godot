extends Camera2D

@export_group("摄像机控制配置")
@export var zoom_step: float = 0.1
@export var zoom_smoothness: float = 10.0
@export var drag_sensitivity: float = 1.0
@export var camera_speed: float = 1500.0

@export_group("边界限制")
@export var min_zoom: Vector2 = Vector2(0.4, 0.4)
@export var max_zoom: Vector2 = Vector2(2.0, 2.0)

var _target_zoom: Vector2 = Vector2.ONE 
var _is_dragging: bool = false 
var _is_locked: bool = false 

var _pre_focus_pos: Vector2
var _pre_focus_zoom: Vector2

func _ready() -> void:
	_target_zoom = zoom

func _process(delta: float) -> void:
	if _is_locked: return 
	_handle_camera_movement(delta)
	if zoom.distance_squared_to(_target_zoom) > 0.00001:
		zoom = zoom.lerp(_target_zoom, zoom_smoothness * delta)

func _unhandled_input(event: InputEvent) -> void:
	if _is_locked: return 
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			_change_zoom_target(1.0 + zoom_step)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			_change_zoom_target(1.0 / (1.0 + zoom_step))
		if event.button_index == MOUSE_BUTTON_LEFT or event.button_index == MOUSE_BUTTON_MIDDLE:
			_is_dragging = event.pressed
	if event is InputEventMouseMotion and _is_dragging:
		position -= event.relative * drag_sensitivity / zoom.x

# 关键：这是一个异步函数
func focus_on_position(target_pos: Vector2, target_zoom: Vector2, duration: float):
	_pre_focus_pos = position
	_pre_focus_zoom = _target_zoom
	_is_locked = true 
	_target_zoom = target_zoom 
	
	var tween = create_tween().set_parallel(true)
	tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(self, "position", target_pos, duration)
	tween.tween_property(self, "zoom", target_zoom, duration)
	await tween.finished # 等待动画完成

# 关键：这也是一个异步函数
func restore_camera(duration: float):
	var tween = create_tween().set_parallel(true)
	tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tween.tween_property(self, "position", _pre_focus_pos, duration)
	tween.tween_property(self, "zoom", _pre_focus_zoom, duration)
	await tween.finished
	_target_zoom = _pre_focus_zoom
	_is_locked = false 
	return true # 显式返回，确保 await 正常工作

func _change_zoom_target(multiplier: float) -> void:
	_target_zoom *= multiplier
	_target_zoom.x = clamp(_target_zoom.x, min_zoom.x, max_zoom.x)
	_target_zoom.y = clamp(_target_zoom.y, min_zoom.y, max_zoom.y)

func _handle_camera_movement(delta: float) -> void:
	var dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	if dir != Vector2.ZERO:
		position += dir * camera_speed * delta
