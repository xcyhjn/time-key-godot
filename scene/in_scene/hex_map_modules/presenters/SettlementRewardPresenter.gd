class_name SettlementRewardPresenter
extends RefCounted

const TOOLTIP_NAME := "SettlementRewardTooltip"
const TOOLTIP_LABEL_PATH := "Margin/Label"
const TOOLTIP_TWEEN_META_KEY := "settlement_reward_tooltip_tween"


## Refresh one settlement reward stack from gameplay data.
## This method is the normal entry point after HexMap has already decided whether the reward is available.
## It updates tile shader state, guarantees the tooltip node exists, and writes the latest tooltip text.
func refresh_stack(stack: Area2D, reward_info: Dictionary, is_available: bool, config: Dictionary) -> void:
	apply_shader_state(stack, is_available, false, config)
	ensure_tooltip(stack, config)
	refresh_tooltip(stack, reward_info, config)


## Apply the hover presentation for one available reward stack.
## HexMap owns hover eligibility; this presenter only changes shader strength and tooltip scale.
func set_hover_state(stack: Area2D, is_available: bool, is_hovered: bool, config: Dictionary) -> void:
	apply_shader_state(stack, is_available, is_hovered, config)
	animate_tooltip(stack, is_hovered, config)


## Remove all settlement reward presentation from one stack.
## This is used when leaving reward mode or when a stack no longer has a valid reward landform.
func clear_stack(stack: Area2D, config: Dictionary) -> void:
	apply_shader_state(stack, false, false, config)
	remove_tooltip(stack)


## Write settlement reward shader instance parameters onto stack sprites.
## The block shader filter keeps this from changing unrelated UI sprites that may also be registered on the stack.
func apply_shader_state(stack: Area2D, is_available: bool, is_hovered: bool, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return

	var block_shader := config.get("block_shader", null) as Shader
	for sprite in _get_stack_sprites(stack):
		if not is_instance_valid(sprite) or not (sprite is CanvasItem):
			continue

		var item := sprite as CanvasItem
		if item.material == null or not (item.material is ShaderMaterial):
			continue

		var material := item.material as ShaderMaterial
		if block_shader != null and material.shader != block_shader:
			continue

		if is_available:
			var color: Color = config.get("hover_color", Color.WHITE) if is_hovered else config.get("highlight_color", Color.WHITE)
			item.set_instance_shader_parameter("settlement_highlight_color", color)
			item.set_instance_shader_parameter("settlement_highlight_blend", config.get("highlight_blend", 0.65))
			item.set_instance_shader_parameter("settlement_hover_blend", config.get("hover_blend", 1.0) if is_hovered else 0.0)
			item.set_instance_shader_parameter("highlight_color", color)
			item.set_instance_shader_parameter("is_selected_blend", 1.0 if is_hovered else 0.0)
		else:
			item.set_instance_shader_parameter("settlement_highlight_blend", 0.0)
			item.set_instance_shader_parameter("settlement_hover_blend", 0.0)
			item.set_instance_shader_parameter("is_selected_blend", 0.0)


## Ensure the stack has one reusable tooltip panel.
## Tooltip nodes stay under their stack so they follow map transforms and can be repositioned per height view state.
func ensure_tooltip(stack: Area2D, config: Dictionary) -> PanelContainer:
	if not is_instance_valid(stack):
		return null

	var panel := stack.get_node_or_null(TOOLTIP_NAME) as PanelContainer
	if panel != null:
		update_tooltip_position(stack, config)
		return panel

	panel = PanelContainer.new()
	panel.name = TOOLTIP_NAME
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.z_index = int(config.get("tooltip_z_index", 3000))
	panel.custom_minimum_size = config.get("tooltip_size", Vector2(184.0, 44.0))
	panel.size = config.get("tooltip_size", Vector2(184.0, 44.0))
	panel.pivot_offset = panel.size * 0.5

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.08, 0.04, 0.88)
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.border_color = Color(1.0, 0.78, 0.18, 1.0)
	style.set_corner_radius_all(6)
	panel.add_theme_stylebox_override("panel", style)

	var margin := MarginContainer.new()
	margin.name = "Margin"
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 6)
	margin.add_theme_constant_override("margin_bottom", 6)
	panel.add_child(margin)

	var label := Label.new()
	label.name = "Label"
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", int(config.get("tooltip_font_size", 18)))
	label.add_theme_color_override("font_color", Color(1.0, 0.93, 0.72, 1.0))
	margin.add_child(label)

	stack.add_child(panel)
	update_tooltip_position(stack, config)
	return panel


## Refresh tooltip text from reward info supplied by HexMap.
## If the landform exposes custom tooltip text, that text wins; otherwise the stable reward label fallback is used.
func refresh_tooltip(stack: Area2D, reward_info: Dictionary, config: Dictionary) -> void:
	if reward_info.is_empty():
		return

	var panel := ensure_tooltip(stack, config)
	if panel == null:
		return

	var label := panel.get_node_or_null(TOOLTIP_LABEL_PATH) as Label
	if label == null:
		return

	label.text = _build_tooltip_text(reward_info)


## Reposition one existing tooltip for the current map view state.
## Flat view anchors to the stack top plane; 3D view offsets by the visible stack height.
func update_tooltip_position(stack: Area2D, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return

	var panel := stack.get_node_or_null(TOOLTIP_NAME) as PanelContainer
	if panel == null:
		return

	var height := 1
	if stack.has_meta("height"):
		height = max(1, int(stack.get_meta("height")))

	var current_step_h := float(config.get("current_step_h", 48.0))
	var top_block_y := 0.0 if bool(config.get("is_flat_view", false)) else -float(height - 1) * current_step_h
	var offset: Vector2 = config.get("tooltip_offset", Vector2(-92.0, -230.0))
	panel.position = Vector2(offset.x, top_block_y + offset.y)


## Animate tooltip scale when a reward stack is hovered or unhovered.
## The tween is stored on the stack meta so a new hover transition can cancel the previous one cleanly.
func animate_tooltip(stack: Area2D, is_hovered: bool, config: Dictionary) -> void:
	if not is_instance_valid(stack):
		return

	var panel := stack.get_node_or_null(TOOLTIP_NAME) as PanelContainer
	if panel == null:
		return

	_kill_tooltip_tween(stack)

	var target_scale: Vector2 = config.get("tooltip_hover_scale", Vector2(1.08, 1.08)) if is_hovered else Vector2.ONE
	var tween := panel.create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	stack.set_meta(TOOLTIP_TWEEN_META_KEY, tween)
	tween.tween_property(panel, "scale", target_scale, 0.12)


## Remove the reusable tooltip panel from a stack.
## The active tooltip tween is killed first so it does not keep trying to animate a queued node.
func remove_tooltip(stack: Area2D) -> void:
	if not is_instance_valid(stack):
		return

	_kill_tooltip_tween(stack)
	var panel := stack.get_node_or_null(TOOLTIP_NAME)
	if is_instance_valid(panel):
		panel.queue_free()


## Refresh tooltip positions for all tracked reward stacks.
## HexMap calls this after height-view transitions because the same tooltip must support both 3D and flat views.
func refresh_positions(stacks: Array[Area2D], config: Dictionary) -> void:
	for stack in stacks:
		if is_instance_valid(stack):
			update_tooltip_position(stack, config)


## Read the stack's registered render sprites in a guarded way.
## Some stacks may not have the metadata yet during cleanup paths, so this always returns an array.
func _get_stack_sprites(stack: Area2D) -> Array:
	if not is_instance_valid(stack) or not stack.has_meta("sprites"):
		return []

	var sprites: Variant = stack.get_meta("sprites")
	if sprites is Array:
		return sprites
	return []


## Build the user-facing tooltip text from reward metadata.
## Landform-owned tooltip text remains the extension point for special settlement reward copy.
func _build_tooltip_text(reward_info: Dictionary) -> String:
	var reward_landform = reward_info.get("landform")
	if is_instance_valid(reward_landform) and reward_landform.has_method("get_settlement_reward_tooltip_text"):
		return reward_landform.get_settlement_reward_tooltip_text()

	var reward_label := str(reward_info.get("reward_label", "收获"))
	return "%s 未使用" % reward_label


## Kill the current tooltip tween stored on the stack, if one exists.
## This keeps repeated hover enter/exit events from stacking multiple scale tweens on the same panel.
func _kill_tooltip_tween(stack: Area2D) -> void:
	if not is_instance_valid(stack) or not stack.has_meta(TOOLTIP_TWEEN_META_KEY):
		return

	var old_tween = stack.get_meta(TOOLTIP_TWEEN_META_KEY)
	if is_instance_valid(old_tween) and old_tween.is_valid():
		old_tween.kill()
	stack.remove_meta(TOOLTIP_TWEEN_META_KEY)
