class_name EnemyIntentMapPresenter
extends RefCounted

const TARGET_OVERLAY_NAME := "EnemyIntentTargetOverlay"

var _source_stack: Area2D = null
var _target_stacks: Array[Area2D] = []
var _source_restore_state: Dictionary = {}


## Display one enemy intent on the battle map.
## This method owns only map-side presentation: source tile shader highlight and target tile ripple overlays.
## `hex_map.gd` passes the latest exported tuning values through `config`, so designers still tune in the HexMap inspector.
func show(
	intent_data: EnemyIntentData,
	stack_nodes: Dictionary,
	source_color: Color,
	target_color: Color,
	config: Dictionary
) -> void:
	clear()
	if intent_data == null:
		return

	if stack_nodes.has(intent_data.source_coord):
		var source_stack: Variant = stack_nodes[intent_data.source_coord]
		if is_instance_valid(source_stack) and source_stack is Area2D:
			_source_stack = source_stack
			var blend_config := _get_source_blend_config(intent_data, config)
			_show_source_highlight(
				_source_stack,
				source_color,
				float(blend_config.get("highlight_blend", 0.0)),
				float(blend_config.get("selected_blend", 0.0))
			)

	var seen_targets: Dictionary = {}
	for coord in intent_data.target_coords:
		if seen_targets.has(coord):
			continue
		seen_targets[coord] = true
		var target_stack: Variant = stack_nodes.get(coord, null)
		if not is_instance_valid(target_stack) or not (target_stack is Area2D):
			continue
		_target_stacks.append(target_stack)
		_show_target_overlay(target_stack, target_color, config)


## Clear the current map-side enemy intent presentation.
## Source tiles restore their previous shader instance parameters; target overlays are hidden, not freed, for reuse.
func clear() -> void:
	if is_instance_valid(_source_stack):
		_restore_source_highlight(_source_stack)

	for stack in _target_stacks:
		if is_instance_valid(stack):
			_hide_overlay(stack, TARGET_OVERLAY_NAME)

	_source_stack = null
	_target_stacks.clear()


## Choose source highlight strength from the intent state.
## Colors are provided by the higher-level presentation controller; this module only picks blend amounts.
func _get_source_blend_config(intent_data: EnemyIntentData, config: Dictionary) -> Dictionary:
	if not intent_data.is_valid:
		return {
			"highlight_blend": config.get("source_invalid_highlight_blend", 0.55),
			"selected_blend": config.get("source_invalid_selected_blend", 0.7),
		}
	if intent_data.includes_self:
		return {
			"highlight_blend": config.get("source_self_highlight_blend", 1.0),
			"selected_blend": config.get("source_self_selected_blend", 1.0),
		}
	return {
		"highlight_blend": config.get("source_valid_highlight_blend", 0.85),
		"selected_blend": config.get("source_valid_selected_blend", 0.95),
	}


## Apply source intent highlight by writing the existing tile sprite shader parameters.
## The previous values are cached so `clear()` can restore whatever card-hover or tile state was active before.
func _show_source_highlight(stack: Area2D, color: Color, highlight_blend: float, selected_blend: float) -> void:
	if not is_instance_valid(stack):
		return

	var sprites = stack.get_meta("sprites") as Array
	if sprites == null or sprites.is_empty():
		return

	_source_restore_state.clear()
	_source_restore_state["stack"] = stack
	_source_restore_state["values"] = []

	for sprite in sprites:
		if not is_instance_valid(sprite) or not sprite.material:
			continue
		_source_restore_state["values"].append({
			"sprite": sprite,
			"highlight_color": sprite.get_instance_shader_parameter("highlight_color"),
			"highlight_blend": sprite.get_instance_shader_parameter("highlight_blend"),
			"is_selected_blend": sprite.get_instance_shader_parameter("is_selected_blend"),
		})
		sprite.set_instance_shader_parameter("highlight_color", color)
		sprite.set_instance_shader_parameter("highlight_blend", highlight_blend)
		sprite.set_instance_shader_parameter("is_selected_blend", selected_blend)


## Show or create the target ripple overlay for one target stack.
## Overlay nodes stay attached to their stack and are reused across future hover previews.
func _show_target_overlay(stack: Area2D, color: Color, config: Dictionary) -> void:
	var overlay := _ensure_overlay(stack, TARGET_OVERLAY_NAME, config)
	if overlay == null:
		return
	_update_overlay_transform(stack, overlay, config)
	overlay.visible = true
	if overlay.material:
		overlay.material.set_shader_parameter("ripple_color", color)


## Hide an overlay by name without freeing it.
## Keeping the node avoids repeated allocation when hovering many enemy intents.
func _hide_overlay(stack: Area2D, overlay_name: String) -> void:
	if not is_instance_valid(stack):
		return
	var overlay := stack.get_node_or_null(overlay_name)
	if overlay and overlay is Sprite2D:
		overlay.visible = false


## Restore the source tile shader values captured in `_show_source_highlight()`.
## If the stack changed or the cache is empty, this method exits without touching unrelated tile state.
func _restore_source_highlight(stack: Area2D) -> void:
	if not is_instance_valid(stack):
		return
	if _source_restore_state.is_empty():
		return
	if _source_restore_state.get("stack") != stack:
		return

	var restore_values: Array = _source_restore_state.get("values", [])
	for item in restore_values:
		var sprite = item["sprite"]
		if not is_instance_valid(sprite) or not sprite.material:
			continue
		sprite.set_instance_shader_parameter("highlight_color", item["highlight_color"])
		sprite.set_instance_shader_parameter("highlight_blend", item["highlight_blend"])
		sprite.set_instance_shader_parameter("is_selected_blend", item["is_selected_blend"])

	_source_restore_state.clear()


## Ensure an overlay sprite exists on the stack and has its own ShaderMaterial.
## The texture, shader, z-index, and shader constants come from `config`.
func _ensure_overlay(stack: Area2D, overlay_name: String, config: Dictionary) -> Sprite2D:
	if not is_instance_valid(stack):
		return null

	var overlay := stack.get_node_or_null(overlay_name) as Sprite2D
	if overlay == null:
		overlay = Sprite2D.new()
		overlay.name = overlay_name
		overlay.texture = config.get("frame_texture", null) as Texture2D
		overlay.centered = true
		overlay.z_index = int(config.get("overlay_z_index", 200))
		overlay.visible = false

		var material := ShaderMaterial.new()
		material.shader = config.get("target_shader", null) as Shader
		overlay.material = material
		stack.add_child(overlay)

	_update_overlay_transform(stack, overlay, config)
	_configure_overlay_material(overlay_name, overlay.material, config)
	return overlay


## Align an overlay to the current top collision area of a stack.
## This keeps enemy intent ripples attached to the clickable tile top in both normal and flat view.
func _update_overlay_transform(stack: Area2D, overlay: Sprite2D, config: Dictionary) -> void:
	var collision = stack.get_meta("collision_node") if stack.has_meta("collision_node") else null
	if is_instance_valid(collision):
		overlay.position = collision.position
	else:
		overlay.position = Vector2(
			float(config.get("hitbox_offset_x", 0.0)),
			float(config.get("hitbox_offset_y", 0.0))
		)

	var frame_texture := config.get("frame_texture", null) as Texture2D
	var tex_size := frame_texture.get_size() if frame_texture != null else Vector2.ONE
	if tex_size.x > 0 and tex_size.y > 0:
		overlay.scale = Vector2(
			(float(config.get("hitbox_width", 168.0)) / tex_size.x) * float(config.get("overlay_scale", 1.0)),
			(float(config.get("hitbox_base_height", 60.0)) / tex_size.y) * float(config.get("overlay_scale", 1.0))
		)


## Write exported HexMap tuning values into the overlay shader material.
## At the moment target overlays use ripple parameters; source highlighting writes directly to tile sprite shaders.
func _configure_overlay_material(overlay_name: String, material: Material, config: Dictionary) -> void:
	if not (material is ShaderMaterial):
		return

	var shader_material := material as ShaderMaterial
	if overlay_name == TARGET_OVERLAY_NAME:
		shader_material.set_shader_parameter("ripple_speed", config.get("target_ripple_speed", 3.2))
		shader_material.set_shader_parameter("ripple_density", config.get("target_ripple_density", 20.0))
		shader_material.set_shader_parameter("min_alpha", config.get("target_min_alpha", 0.2))
		shader_material.set_shader_parameter("max_alpha", config.get("target_max_alpha", 0.95))
