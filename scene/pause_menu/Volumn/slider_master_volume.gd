extends HSlider

@onready var value_popup = $ValuePopup
@onready var value_label = $ValuePopup/ValueLabel

var fade_tween: Tween
# 状态标记
var mouse_over: bool = false
var is_dragging: bool = false

@export var manual_grabber_width: int = 0

func _ready():
	# --- 当滑条自身大小改变时（比如窗口拉伸），重新计算弹窗位置 ---
	item_rect_changed.connect(_update_popup_position)
	_on_value_changed(value)

	value_popup.modulate.a = 0.0
	await get_tree().process_frame

	value_changed.connect(_on_value_changed)

	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)

	drag_started.connect(_on_drag_started)
	drag_ended.connect(_on_drag_ended)

	focus_entered.connect(_on_focus_entered)
	focus_exited.connect(_on_focus_exited)

	_on_value_changed(value)

# --- 核心逻辑 ---

func _on_value_changed(new_value):
	value_label.text = str(int(new_value * 100))
	_update_popup_position()
	SoundManager.set_volume(SoundManager.Bus.MASTER, new_value)

	# 键盘操作唤醒：如果数值变了，且有焦点，且框是隐藏的 -> 显示
	if has_focus() and value_popup.modulate.a < 0.1:
		_show_popup()

func _on_mouse_entered():
	mouse_over = true
	_show_popup()

func _on_mouse_exited():
	mouse_over = false
	if not is_dragging:
		_hide_popup()

func _on_drag_started():
	is_dragging = true
	_show_popup()

func _on_drag_ended(_value_changed):
	is_dragging = false
	if not mouse_over:
		_hide_popup()

func _on_focus_entered():
	_show_popup()

func _on_focus_exited():
	_hide_popup()

# --- [新增] 全局输入监听：解决点击空白处不消失的问题 ---
func _input(event):
	# 只有当：鼠标左键按下 时才检测
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		# 获取 Slider 在屏幕上的矩形范围
		var rect = get_global_rect()
		# 检查鼠标点击的位置是否在 Slider 矩形范围内
		if not rect.has_point(get_global_mouse_position()):
			# 如果点在外面，不管焦没焦点，强制隐藏
			_hide_popup()

# --- 动画与位置 ---

func _show_popup():
	# --- 显示前强制校准位置 ---
	_update_popup_position()

	if value_popup.modulate.a > 0.9 and (fade_tween and fade_tween.is_running()):
		return
	if fade_tween: fade_tween.kill()
	fade_tween = create_tween()
	fade_tween.tween_property(value_popup, "modulate:a", 1.0, 0.15)

func _hide_popup():
	if fade_tween: fade_tween.kill()
	fade_tween = create_tween()
	fade_tween.tween_property(value_popup, "modulate:a", 0.0, 0.2)

func _update_popup_position():
	var grabber_width = 0
	if manual_grabber_width > 0:
		grabber_width = manual_grabber_width
	else:
		var icon = get_theme_icon("grabber", "HSlider")
		if icon: grabber_width = icon.get_width()
		else: grabber_width = 20

	var available_width = size.x - grabber_width
	var grabber_center_x = (grabber_width / 2.0) + (available_width * ratio)
	var popup_x = grabber_center_x - (value_popup.size.x / 2.0)
	var popup_y = -value_popup.size.y - 10

	value_popup.position = Vector2(popup_x, popup_y)
