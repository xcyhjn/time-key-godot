class_name InSceneInputLockController
extends RefCounted


## InSceneInputLockController 集中维护局内“玩家输入是否可用”的 UI 状态。
## 它不决定什么时候锁输入，只执行 MainBoard 下发的锁定/解锁命令。


func disable(config: Dictionary) -> void:
	_set_mouse_filter(config.get("player_hand"), Control.MOUSE_FILTER_IGNORE)
	_set_mouse_filter(config.get("deck_button"), Control.MOUSE_FILTER_IGNORE)
	_set_mouse_filter(config.get("discard_button"), Control.MOUSE_FILTER_IGNORE)
	_set_button_disabled(config.get("end_turn_button"), true)


func enable(config: Dictionary) -> void:
	_set_mouse_filter(config.get("player_hand"), Control.MOUSE_FILTER_PASS)
	_set_mouse_filter(config.get("deck_button"), Control.MOUSE_FILTER_PASS)
	_set_mouse_filter(config.get("discard_button"), Control.MOUSE_FILTER_PASS)
	_set_button_disabled(config.get("end_turn_button"), false)


func _set_mouse_filter(target: Variant, mode: int) -> void:
	if is_instance_valid(target) and target is Control:
		target.mouse_filter = mode


func _set_button_disabled(target: Variant, disabled: bool) -> void:
	if is_instance_valid(target) and target is BaseButton:
		target.disabled = disabled
