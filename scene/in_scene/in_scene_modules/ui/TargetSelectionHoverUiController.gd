class_name TargetSelectionHoverUiController
extends RefCounted


## TargetSelectionHoverUiController 只负责目标选择 hover 的光标提示表现。
## 目标是否合法仍由 MainBoard/HexMap 规则判断，避免 UI 模块持有地图规则。


func update_hover(config: Dictionary) -> void:
	var cursor_tooltip: Variant = config.get("cursor_tooltip")
	if not is_instance_valid(cursor_tooltip):
		return

	var set_position: Variant = config.get("set_cursor_tooltip_position")
	if set_position is Callable:
		var callable: Callable = set_position
		if callable.is_valid():
			callable.call(config.get("mouse_position", Vector2.ZERO) + Vector2(20, -30))

	if bool(config.get("is_valid_target", false)):
		_show_valid_target(cursor_tooltip, config.get("active_card"))
	else:
		_show_invalid_target(cursor_tooltip)


func _show_valid_target(cursor_tooltip: Variant, active_card: Variant) -> void:
	var damage: int = _get_card_attack(active_card)
	cursor_tooltip.text = "-" + str(damage)
	cursor_tooltip.add_theme_color_override("font_color", Color.RED)
	cursor_tooltip.show()


func _show_invalid_target(cursor_tooltip: Variant) -> void:
	cursor_tooltip.text = "无效果"
	cursor_tooltip.add_theme_color_override("font_color", Color.GRAY)
	cursor_tooltip.show()


func _get_card_attack(active_card: Variant) -> int:
	if not is_instance_valid(active_card):
		return 0

	var card_info: Variant = active_card.get("card_info")
	if typeof(card_info) != TYPE_DICTIONARY:
		return 0

	return int(card_info.get("ATK", 0))
