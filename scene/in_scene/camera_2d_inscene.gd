# 原文件名: camera_2d(局内镜头).gd
# 功能: 局内镜头控制
extends Camera2D

@export_group("摄像机控制配置 (Camera Settings)")
## 鼠标滚轮缩放的灵敏度 (每次滚动的比例)
@export var zoom_step: float = 0.1
## 缩放平滑度 (数值越大越快，越小越平滑)
@export var zoom_smoothness: float = 10.0
## 鼠标拖动的灵敏度
@export var drag_sensitivity: float = 1.0
## 键盘移动摄像机的速度
@export var camera_speed: float = 1500.0

@export_group("边界限制 (Limits)")
## 允许缩小的最小倍数 (视野最大)
@export var min_zoom: Vector2 = Vector2(0.8, 0.8)
## 允许放大的最大倍数 (视野最小)
@export var max_zoom: Vector2 = Vector2(2.0, 2.0)
## 是否允许鼠标滚轮缩放。局内收获阶段会临时关闭，但保留普通镜头移动。
@export var zoom_input_enabled: bool = true

# --- 内部变量 ---
var _target_zoom: Vector2 = Vector2.ONE  # 记录目标缩放值，用于实现平滑过渡
var _is_dragging: bool = false  # 标记鼠标是否按下


func _ready() -> void:
	# 初始化目标缩放值为当前自身 (Camera2D) 的缩放值
	_target_zoom = zoom


func _process(delta: float) -> void:
	## 1. 允许键盘 (WASD或方向键) 平滑移动摄像机
	#_handle_camera_movement(delta)

	# 2. 平滑缩放插值
	# 直接使用 zoom 而不是 camera.zoom，因为脚本本身就是 Camera2D
	if zoom.distance_squared_to(_target_zoom) > 0.00001:
		zoom = zoom.lerp(_target_zoom, zoom_smoothness * delta)


func _unhandled_input(event: InputEvent) -> void:
	# 1. 处理鼠标滚轮缩放
	if event is InputEventMouseButton:
		if zoom_input_enabled:
			if event.button_index == MOUSE_BUTTON_WHEEL_UP:
				_change_zoom_target(1.0 + zoom_step)  # 向上滚：放大
			elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				_change_zoom_target(1.0 / (1.0 + zoom_step))  # 向下滚：缩小
#
		## 2. 处理鼠标按住拖动 (支持左键或中键)
		#if event.button_index == MOUSE_BUTTON_LEFT or event.button_index == MOUSE_BUTTON_MIDDLE:
			## 简化写法：直接把按键的按下状态赋给变量
			#_is_dragging = event.pressed
#
	## 3. 处理鼠标拖拽时的画面位移
	#if event is InputEventMouseMotion and _is_dragging:
		## 移动量 = 鼠标相对移动 / 当前自身的缩放倍率
		#position -= event.relative * drag_sensitivity / zoom.x

# --- 辅助函数 ---


# 修改目标缩放倍数，并限制范围
func _change_zoom_target(multiplier: float) -> void:
	_target_zoom *= multiplier
	_target_zoom.x = clamp(_target_zoom.x, min_zoom.x, max_zoom.x)
	_target_zoom.y = clamp(_target_zoom.y, min_zoom.y, max_zoom.y)

#
## 键盘移动逻辑
#func _handle_camera_movement(delta: float) -> void:
	#var dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	#if dir != Vector2.ZERO:
		#position += dir * camera_speed * delta
