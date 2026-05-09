extends CanvasLayer

# --- 配置参数 ---
@export_group("Animation Settings")
@export var anim_duration: float = 0.5 
@export var stagger_delay: float = 0.08 
@export var phase_gap: float = 0.1
@export var top_layer: int = 10000
@export var move_to_root_on_open: bool = true
@export var restore_original_parent_on_close: bool = true

@export_subgroup("Easing")
@export var trans_type: Tween.TransitionType = Tween.TRANS_BACK # 更平滑的曲线
@export var ease_in_type: Tween.EaseType = Tween.EASE_OUT
@export var ease_out_type: Tween.EaseType = Tween.EASE_IN

# --- 节点引用 ---
@onready var panel_container: PanelContainer = $PanelContainer
@onready var menu: Control = $Tip
@onready var back_button1: Button = $Back
@onready var back_button2: Button = $Confirm

# --- 内部状态 ---
var _is_animating: bool = false
var _active_tween: Tween
var _original_parent: Node = null
var _original_index: int = -1

func _ready() -> void:
	layer = top_layer
	process_mode = Node.PROCESS_MODE_ALWAYS
	self.hide()

# --- 入场动画 ---
func play_entrance():
	_ensure_topmost_layer()
	self.show()
	if _is_animating: return
	_is_animating = true
	back_button1.disabled = false
	back_button2.disabled = false
	Global.clock.emit(0)
	
	if _active_tween: _active_tween.kill()
	_active_tween = create_tween().set_parallel(true)
	_active_tween.set_trans(trans_type).set_ease(ease_in_type)
	
	_active_tween.tween_property(panel_container, "modulate:a", 1.0, anim_duration).from(0.0)
	_active_tween.tween_property(menu, "modulate:a", 1.0, anim_duration * 2).from(0.0)
	var back_delay = phase_gap + phase_gap
	_active_tween.tween_property(back_button1, "modulate:a", 1.0, anim_duration).set_delay(back_delay).from(0.0)
	_active_tween.tween_property(back_button2, "modulate:a", 1.0, anim_duration).set_delay(back_delay).from(0.0)
	
	await _active_tween.finished
	_is_animating = false

# --- 退场动画 ---
func play_exit() -> Signal:
	if _is_animating: return self.ready # 防止重复触发
	_is_animating = true
	back_button1.disabled = true
	back_button2.disabled = true
	Global.clock.emit(1)
	
	if _active_tween: _active_tween.kill()
	_active_tween = create_tween().set_parallel(true)
	_active_tween.set_trans(trans_type).set_ease(ease_out_type)
	
	_active_tween.tween_property(back_button1, "modulate:a", 0.0, anim_duration * 0.5)
	_active_tween.tween_property(back_button2, "modulate:a", 0.0, anim_duration * 0.5)
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
	_restore_original_parent()

func _on_confirm_button_down() -> void:
	get_tree().quit()


## 确认退出弹窗会被暂停菜单等 CanvasLayer 调起。
## 核心逻辑：显示前移到 root 并抬高 layer，避免原父级 UI 拦截输入。
func _ensure_topmost_layer() -> void:
	layer = top_layer
	process_mode = Node.PROCESS_MODE_ALWAYS
	if not move_to_root_on_open:
		return

	var tree := get_tree()
	if tree == null:
		return
	var root := tree.root
	if get_parent() == root:
		root.move_child(self, root.get_child_count() - 1)
		return

	var current_parent := get_parent()
	if current_parent:
		_original_parent = current_parent
		_original_index = current_parent.get_children().find(self)
		current_parent.remove_child(self)
	root.add_child(self)


## 关闭确认框后放回原父节点，避免从菜单/暂停层移到 root 后残留。
func _restore_original_parent() -> void:
	if not restore_original_parent_on_close or not move_to_root_on_open:
		return
	if not is_instance_valid(_original_parent):
		return
	if get_parent() == _original_parent:
		return

	var current_parent := get_parent()
	if current_parent:
		current_parent.remove_child(self)
	_original_parent.add_child(self)
	if _original_index >= 0 and _original_index < _original_parent.get_child_count():
		_original_parent.move_child(self, _original_index)
	_original_parent = null
	_original_index = -1
