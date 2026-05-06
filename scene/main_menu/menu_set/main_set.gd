extends CanvasLayer

# --- 配置参数 ---
@export_group("Animation Settings")
@export var anim_duration: float = 0.5 
@export var stagger_delay: float = 0.08 
@export var phase_gap: float = 0.1

@export_subgroup("Easing")
@export var trans_type: Tween.TransitionType = Tween.TRANS_BACK # 更平滑的曲线
@export var ease_in_type: Tween.EaseType = Tween.EASE_OUT
@export var ease_out_type: Tween.EaseType = Tween.EASE_IN

# --- 节点引用 ---
@onready var panel_container: PanelContainer = $PanelContainer
@onready var menu: Control = $OptionsMenu
@onready var back_button: Button = $Back

# --- 内部状态 ---
var _is_animating: bool = false
var _active_tween: Tween

func _ready() -> void:
	panel_container.modulate.a = 0.0
	menu.modulate.a = 0.0
	back_button.modulate.a = 0.0
	self.hide()

# --- 入场动画 ---
func play_entrance():
	self.show()
	if _is_animating: return
	_is_animating = true
	back_button.disabled = false
	Global.clock.emit(0)
	
	if _active_tween: _active_tween.kill()
	_active_tween = create_tween().set_parallel(true)
	_active_tween.set_trans(trans_type).set_ease(ease_in_type)
	
	_active_tween.tween_property(panel_container, "modulate:a", 1.0, anim_duration).from(0.0)
	_active_tween.tween_property(menu, "modulate:a", 1.0, anim_duration * 2).from(0.0)
	var back_delay = phase_gap + phase_gap
	_active_tween.tween_property(back_button, "modulate:a", 1.0, anim_duration).set_delay(back_delay).from(0.0)
	
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
	
	_active_tween.tween_property(back_button, "modulate:a", 0.0, anim_duration * 0.5)
	_active_tween.tween_property(menu, "modulate:a", 0.0, anim_duration)
	var bg_delay = phase_gap + phase_gap
	_active_tween.tween_property(panel_container, "modulate:a", 0.0, anim_duration).set_delay(bg_delay)
	
	_active_tween.chain().tween_callback(func(): 
		_is_animating = false
	)
	return _active_tween.finished

func _on_back_button_down() -> void:
	await play_exit()
	self.hide()
