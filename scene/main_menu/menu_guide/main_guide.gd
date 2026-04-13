extends CanvasLayer

# --- 配置参数 ---
@export_group("Animation Settings")
@export var start_horizontal_offset: float = -600.0 
@export var anim_duration: float = 0.5 
@export var stagger_delay: float = 0.08 
@export var phase_gap: float = 0.1

@export_subgroup("Easing")
@export var trans_type: Tween.TransitionType = Tween.TRANS_BACK # 更平滑的曲线
@export var ease_in_type: Tween.EaseType = Tween.EASE_OUT
@export var ease_out_type: Tween.EaseType = Tween.EASE_IN

# --- 节点引用 ---
@onready var panel_container: Control = $Control/PanelContainer
@onready var button_group: Control = $Control/ButtonGroup
@onready var back_button: Button = $Control/Back
@onready var control: Control = $Control

# --- 内部状态 ---
var _original_positions: Dictionary = {} # 使用字典记录，更安全
var _is_animating: bool = false
var _active_tween: Tween

func _ready():
	self.show()
	trans(0)
	_initialize_ui()
	# 等待一帧确保 UI 布局计算完成
	await get_tree().process_frame
	_record_positions()
	

func trans(mode):
	match mode:
		0:
			control.modulate.a = 0
			layer = -1
		1:
			control.modulate.a = 1
			layer = 1
	
func _initialize_ui():
	panel_container.modulate.a = 0
	back_button.modulate.a = 0
	for btn in _get_valid_buttons():
		btn.modulate.a = 0

func _record_positions():
	for btn in _get_valid_buttons():
		_original_positions[btn.get_instance_id()] = btn.position.x

func _get_valid_buttons() -> Array[Control]:
	# 过滤掉非 Control 节点，确保代码不会崩
	var list: Array[Control] = []
	for child in button_group.get_children():
		if child is Control:
			list.append(child)
	return list

# --- 入场动画 ---
func play_entrance():
	trans(1)
	back_button.disabled = false
	if _is_animating: return
	_is_animating = true
	Global.clock.emit(0)
	
	if _active_tween: _active_tween.kill()
	_active_tween = create_tween().set_parallel(true)
	_active_tween.set_trans(trans_type).set_ease(ease_in_type)
	
	# A: 背景
	_active_tween.tween_property(panel_container, "modulate:a", 1.0, anim_duration)
	
	# B: 滑块组
	var buttons = _get_valid_buttons()
	for i in range(buttons.size()):
		var btn = buttons[i]
		var delay = phase_gap + (i * stagger_delay)
		var orig_x = _original_positions.get(btn.get_instance_id(), btn.position.x)
		
		btn.position.x = orig_x + start_horizontal_offset
		_active_tween.tween_property(btn, "position:x", orig_x, anim_duration).set_delay(delay)
		_active_tween.tween_property(btn, "modulate:a", 1.0, anim_duration * 0.6).set_delay(delay)
	
	# C: 返回按钮
	var back_delay = phase_gap + (buttons.size() * stagger_delay) + phase_gap
	_active_tween.tween_property(back_button, "modulate:a", 1.0, anim_duration).set_delay(back_delay)
	
	await _active_tween.finished
	_is_animating = false

# --- 退场动画 ---
func play_exit() -> Signal:
	if _is_animating: return self.ready # 防止重复触发
	_is_animating = true
	back_button.disabled = true
	Global.clock.emit(1)
	
	if _active_tween: _active_tween.kill()
	_active_tween = create_tween().set_parallel(true)
	_active_tween.set_trans(trans_type).set_ease(ease_out_type)
	
	# A: 返回按钮
	_active_tween.tween_property(back_button, "modulate:a", 0.0, anim_duration * 0.5)
	
	# B: 滑块组 (反向)
	var buttons = _get_valid_buttons()
	for i in range(buttons.size()):
		var btn = buttons[i]
		var rev_idx = (buttons.size() - 1 - i)
		var delay = phase_gap + (rev_idx * stagger_delay)
		var target_x = _original_positions.get(btn.get_instance_id(), btn.position.x) + start_horizontal_offset
		
		_active_tween.tween_property(btn, "position:x", target_x, anim_duration).set_delay(delay)
		_active_tween.tween_property(btn, "modulate:a", 0.0, anim_duration * 0.8).set_delay(delay)
	
	# C: 背景
	var bg_delay = phase_gap + (buttons.size() * stagger_delay) + phase_gap
	_active_tween.tween_property(panel_container, "modulate:a", 0.0, anim_duration).set_delay(bg_delay)
	
	_active_tween.chain().tween_callback(func(): 
		_is_animating = false
	)
	return _active_tween.finished

# --- 信号处理 ---
func _on_back_button_down() -> void:
	await play_exit()
	trans(0)
