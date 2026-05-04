# 功能: 教程蒙版层，提供全屏遮罩与可移动的圆形聚焦孔位，用于引导玩家关注指定地图格或 UI 区域。
# 核心逻辑: Director 调用 focus_world_position/focus_control 更新焦点；本脚本通过 shader 绘制圆形透明孔与圆环，避免矩形拼接造成横纵轴延伸高亮。
extends Control
class_name TutorialMaskLayer

const MASK_SHADER = preload("res://scene/tutorial/tutorial_mask_layer.gdshader")

@export_group("蒙版表现")
@export var mask_color: Color = Color(0.0, 0.0, 0.0, 0.58)
@export var focus_color: Color = Color(1.0, 0.86, 0.35, 0.95)
@export var focus_radius: float = 86.0
@export var focus_outline_width: float = 4.0
@export var pulse_speed: float = 2.4
@export var pulse_amount: float = 8.0

var _has_focus: bool = false
var _focus_position: Vector2 = Vector2.ZERO
var _focus_radius: float = 86.0
var _mask_material: ShaderMaterial = null


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)
	_mask_material = ShaderMaterial.new()
	_mask_material.shader = MASK_SHADER
	material = _mask_material
	_sync_shader_uniforms()
	hide()


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED:
		_sync_shader_uniforms()
		queue_redraw()


func clear_focus() -> void:
	_has_focus = false
	_sync_shader_uniforms()
	hide()
	queue_redraw()


func focus_screen_position(screen_position: Vector2, radius: float = -1.0) -> void:
	_has_focus = true
	_focus_position = get_global_transform_with_canvas().affine_inverse() * screen_position
	_focus_radius = radius if radius > 0.0 else focus_radius
	_sync_shader_uniforms()
	show()
	queue_redraw()


func focus_world_position(canvas_item: CanvasItem, world_position: Vector2, radius: float = -1.0) -> void:
	if not is_instance_valid(canvas_item):
		return
	var screen_position: Vector2 = canvas_item.get_global_transform_with_canvas() * world_position
	focus_screen_position(screen_position, radius)


func focus_control(control: Control, padding: float = 18.0) -> void:
	if not is_instance_valid(control):
		return
	var rect: Rect2 = control.get_global_rect()
	var radius: float = maxf(rect.size.x, rect.size.y) * 0.5 + padding
	focus_screen_position(rect.get_center(), radius)


func _draw() -> void:
	if not visible:
		return

	_sync_shader_uniforms()
	draw_rect(Rect2(Vector2.ZERO, size), Color.WHITE, true)


func _sync_shader_uniforms() -> void:
	if _mask_material == null:
		return
	_mask_material.set_shader_parameter("mask_color", mask_color)
	_mask_material.set_shader_parameter("focus_color", focus_color)
	_mask_material.set_shader_parameter("focus_position", _focus_position)
	_mask_material.set_shader_parameter("focus_radius", _focus_radius)
	_mask_material.set_shader_parameter("focus_outline_width", focus_outline_width)
	_mask_material.set_shader_parameter("pulse_speed", pulse_speed)
	_mask_material.set_shader_parameter("pulse_amount", pulse_amount)
	_mask_material.set_shader_parameter("has_focus", _has_focus)
