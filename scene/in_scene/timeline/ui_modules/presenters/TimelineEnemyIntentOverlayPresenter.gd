extends RefCounted

## TimelineEnemyIntentOverlayPresenter 只负责时间轴敌方意图 Overlay 的材质和显示状态。
## 它不创建行动块，不修改 TimelineManager 数据，也不决定何时进入或退出预览。


func build_config(
	pulse_speed: float,
	min_alpha: float,
	max_alpha: float,
	stripe_color: Color,
	stripe_speed: float,
	stripe_density: float,
	stripe_width: float,
	stripe_strength: float
) -> Dictionary:
	return {
		"pulse_speed": pulse_speed,
		"min_alpha": min_alpha,
		"max_alpha": max_alpha,
		"stripe_color": stripe_color,
		"stripe_speed": stripe_speed,
		"stripe_density": stripe_density,
		"stripe_width": stripe_width,
		"stripe_strength": stripe_strength,
	}


func get_default_config() -> Dictionary:
	return build_config(
		3.0,
		0.15,
		0.75,
		Color(1.0, 0.85, 0.12, 1.0),
		3.8,
		12.0,
		0.12,
		0.75
	)


func create_overlay_material(
	shader: Shader,
	pulse_speed: float,
	min_alpha: float,
	max_alpha: float,
	stripe_color: Color,
	stripe_speed: float,
	stripe_density: float,
	stripe_width: float,
	stripe_strength: float
) -> ShaderMaterial:
	return create_overlay_material_from_config(
		shader,
		build_config(
			pulse_speed,
			min_alpha,
			max_alpha,
			stripe_color,
			stripe_speed,
			stripe_density,
			stripe_width,
			stripe_strength
		)
	)


func create_overlay_material_from_config(shader: Shader, config: Dictionary) -> ShaderMaterial:
	var overlay_material = ShaderMaterial.new()
	overlay_material.shader = shader
	overlay_material.set_shader_parameter("pulse_speed", config.get("pulse_speed", 3.0))
	overlay_material.set_shader_parameter("min_alpha", config.get("min_alpha", 0.15))
	overlay_material.set_shader_parameter("max_alpha", config.get("max_alpha", 0.75))
	configure_material_from_config(overlay_material, config)
	return overlay_material


func set_overlay_visible(
	container: Control,
	visible: bool,
	color: Color,
	stripe_color: Color,
	stripe_speed: float,
	stripe_density: float,
	stripe_width: float,
	stripe_strength: float
) -> void:
	set_overlay_visible_from_config(
		container,
		visible,
		color,
		build_config(
			3.0,
			0.15,
			0.75,
			stripe_color,
			stripe_speed,
			stripe_density,
			stripe_width,
			stripe_strength
		)
	)


func set_overlay_visible_from_config(container: Control, visible: bool, color: Color, config: Dictionary) -> void:
	for block in container.get_children():
		if block is Panel:
			var overlay = block.get_node_or_null("EnemyIntentOverlay")
			if overlay and overlay is ColorRect:
				overlay.visible = visible
				if visible and overlay.material:
					overlay.material.set_shader_parameter("pulse_color", color)
					configure_material_from_config(overlay.material, config)


func apply_preview_state(
	container: Control,
	preview_scale: Vector2,
	preview_z_index: int,
	pulse_color: Color,
	config: Dictionary
) -> void:
	if not is_instance_valid(container):
		return

	container.pivot_offset = container.size * 0.5
	container.modulate = Color.WHITE
	container.scale = preview_scale
	container.z_index = preview_z_index
	set_overlay_visible_from_config(container, true, pulse_color, config)


func clear_preview_state(container: Control, pulse_color: Color, config: Dictionary) -> void:
	if not is_instance_valid(container):
		return

	container.modulate = Color.WHITE
	container.scale = Vector2.ONE
	container.z_index = 0
	set_overlay_visible_from_config(container, false, pulse_color, config)


func configure_material(
	material: Material,
	stripe_color: Color,
	stripe_speed: float,
	stripe_density: float,
	stripe_width: float,
	stripe_strength: float
) -> void:
	configure_material_from_config(
		material,
		build_config(
			3.0,
			0.15,
			0.75,
			stripe_color,
			stripe_speed,
			stripe_density,
			stripe_width,
			stripe_strength
		)
	)


func configure_material_from_config(material: Material, config: Dictionary) -> void:
	if not (material is ShaderMaterial):
		return

	var default_config := get_default_config()
	var shader_material = material as ShaderMaterial
	shader_material.set_shader_parameter("stripe_color", config.get("stripe_color", default_config["stripe_color"]))
	shader_material.set_shader_parameter("stripe_speed", config.get("stripe_speed", default_config["stripe_speed"]))
	shader_material.set_shader_parameter("stripe_density", config.get("stripe_density", default_config["stripe_density"]))
	shader_material.set_shader_parameter("stripe_width", config.get("stripe_width", default_config["stripe_width"]))
	shader_material.set_shader_parameter("stripe_strength", config.get("stripe_strength", default_config["stripe_strength"]))
