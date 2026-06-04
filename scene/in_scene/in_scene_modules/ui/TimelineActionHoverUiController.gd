class_name TimelineActionHoverUiController
extends RefCounted


## TimelineActionHoverUiController 负责时间轴行动 hover 的旧 UI 表现。
## 敌方意图 hover 已由 EnemyIntentPresentationController 接管，因此这里保持 ENEMY 早退。


func handle_hover(config: Dictionary) -> void:
	var action: TimelineAction = config.get("action") as TimelineAction
	if action == null:
		return
	if action.type == TimelineAction.Type.ENEMY:
		return

	if bool(config.get("is_hovering", false)):
		_show_player_action_hover(action, config)
	else:
		_clear_action_hover(action, config.get("cursor_tooltip"))


func _show_player_action_hover(action: TimelineAction, config: Dictionary) -> void:
	_apply_pulse_shader(action.target_tile, Color(1.0, 0.8, 0.0, 1.0), config.get("pulse_shader"))
	_show_cursor_tooltip(
		config.get("cursor_tooltip"),
		"卡牌效果: " + str(action.action_data.get("效果", "发动未知技能！")),
		Color.GOLD
	)
	_set_tooltip_position(config)


func _clear_action_hover(action: TimelineAction, cursor_tooltip: Variant) -> void:
	_clear_pulse_shader(action.source_node)
	_clear_pulse_shader(action.target_tile)
	if is_instance_valid(cursor_tooltip):
		cursor_tooltip.hide()


func _show_cursor_tooltip(cursor_tooltip: Variant, text: String, font_color: Color) -> void:
	if not is_instance_valid(cursor_tooltip):
		return
	cursor_tooltip.text = text
	cursor_tooltip.add_theme_color_override("font_color", font_color)
	cursor_tooltip.show()


func _set_tooltip_position(config: Dictionary) -> void:
	var set_position: Variant = config.get("set_cursor_tooltip_position")
	if not (set_position is Callable):
		return
	var callable: Callable = set_position
	if callable.is_valid():
		callable.call(config.get("mouse_position", Vector2.ZERO) + Vector2(20, -30))


func _apply_pulse_shader(target_node: Node, color: Color, pulse_shader: Variant) -> void:
	if not is_instance_valid(target_node):
		return
	if not is_instance_valid(pulse_shader):
		return

	var sprite: Variant = target_node.get_node_or_null("Sprite2D")
	if sprite:
		sprite.material = pulse_shader.duplicate()
		sprite.material.set_shader_parameter("pulse_color", color)


func _clear_pulse_shader(target_node: Node) -> void:
	if not is_instance_valid(target_node):
		return

	var sprite: Variant = target_node.get_node_or_null("Sprite2D")
	if sprite:
		sprite.material = null
