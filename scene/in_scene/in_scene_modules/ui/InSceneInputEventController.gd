class_name InSceneInputEventController
extends RefCounted


## InSceneInputEventController 负责把 MainBoard 的原始输入拆成明确动作。
## 它只处理低风险同步输入；需要 await 的弃牌移动仍交回 InScene 主脚本执行。

const ACTION_NONE: String = "none"
const ACTION_HANDLED: String = "handled"
const ACTION_DISCARD_CARD: String = "discard_card"


func handle_input(event: InputEvent, config: Dictionary) -> Dictionary:
	if _handle_keyboard_timecoin_debug(event, config):
		return {"action": ACTION_HANDLED}

	if _handle_right_click_deselect(event, config):
		return {"action": ACTION_HANDLED}

	var discard_card: Variant = _find_card_for_empty_release_discard(event, config)
	if is_instance_valid(discard_card):
		return {
			"action": ACTION_DISCARD_CARD,
			"card": discard_card,
		}

	return {"action": ACTION_NONE}


func _handle_keyboard_timecoin_debug(event: InputEvent, config: Dictionary) -> bool:
	if not bool(config.get("enable_keyboard_timecoin_debug", false)):
		return false
	if not (event is InputEventKey):
		return false

	var key_event: InputEventKey = event as InputEventKey
	if not key_event.pressed or key_event.echo or key_event.keycode != KEY_9:
		return false

	var global_timecoin: Variant = config.get("global_timecoin")
	if global_timecoin and global_timecoin.has_method("add_timecoins"):
		global_timecoin.add_timecoins(int(config.get("keyboard_timecoin_debug_amount", 0)))
	return true


func _handle_right_click_deselect(event: InputEvent, config: Dictionary) -> bool:
	if not (event is InputEventMouseButton):
		return false

	var mouse_event: InputEventMouseButton = event as InputEventMouseButton
	if not mouse_event.pressed or mouse_event.button_index != MOUSE_BUTTON_RIGHT:
		return false
	if _is_dragging_timeline_shape(config.get("drag_controller")):
		return false

	var manager_instance: Variant = config.get("manager_instance")
	var selected_card: Variant = manager_instance.get("current_selected_card") if manager_instance else null
	if not is_instance_valid(selected_card) or not selected_card.has_method("force_deselect"):
		return false

	selected_card.force_deselect()
	_call_if_valid(config.get("hide_tooltip"))
	return true


func _find_card_for_empty_release_discard(event: InputEvent, config: Dictionary) -> Variant:
	if not (event is InputEventMouseButton):
		return null

	var mouse_event: InputEventMouseButton = event as InputEventMouseButton
	if mouse_event.pressed or mouse_event.button_index != MOUSE_BUTTON_LEFT:
		return null

	var player_hand: Hand = config.get("player_hand") as Hand
	if not is_instance_valid(player_hand):
		return null

	var mouse_position: Vector2 = config.get("mouse_position", Vector2.ZERO)
	var screen_size: Vector2 = config.get("screen_size", Vector2.ZERO)
	var discard_threshold: float = screen_size.y * float(config.get("drop_area", 0.0))
	if mouse_position.y >= discard_threshold:
		return null

	for card in player_hand._held_cards:
		if is_instance_valid(card) and card.get("is_pressed") == true:
			return card

	return null


func _is_dragging_timeline_shape(drag_controller: Variant) -> bool:
	if not is_instance_valid(drag_controller):
		return false
	if drag_controller.get("is_dragging") == null:
		return false
	return bool(drag_controller.get("is_dragging"))


func _call_if_valid(callback: Variant) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.call()
