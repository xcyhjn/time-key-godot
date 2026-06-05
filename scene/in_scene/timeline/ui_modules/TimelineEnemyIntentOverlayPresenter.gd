class_name TimelineEnemyIntentOverlayPresenter
extends RefCounted

## TimelineEnemyIntentOverlayPresenter 只负责时间轴敌方意图 Overlay 的材质和显示状态。
## 它不创建行动块，不修改 TimelineManager 数据，也不决定何时进入或退出预览。


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
	var overlay_material = ShaderMaterial.new()
	overlay_material.shader = shader
	overlay_material.set_shader_parameter("pulse_speed", pulse_speed)
	overlay_material.set_shader_parameter("min_alpha", min_alpha)
	overlay_material.set_shader_parameter("max_alpha", max_alpha)
	configure_material(
		overlay_material,
		stripe_color,
		stripe_speed,
		stripe_density,
		stripe_width,
		stripe_strength
	)
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
	for block in container.get_children():
		if block is Panel:
			var overlay = block.get_node_or_null("EnemyIntentOverlay")
			if overlay and overlay is ColorRect:
				overlay.visible = visible
				if visible and overlay.material:
					overlay.material.set_shader_parameter("pulse_color", color)
					configure_material(
						overlay.material,
						stripe_color,
						stripe_speed,
						stripe_density,
						stripe_width,
						stripe_strength
					)


func configure_material(
	material: Material,
	stripe_color: Color,
	stripe_speed: float,
	stripe_density: float,
	stripe_width: float,
	stripe_strength: float
) -> void:
	if not (material is ShaderMaterial):
		return

	var shader_material = material as ShaderMaterial
	shader_material.set_shader_parameter("stripe_color", stripe_color)
	shader_material.set_shader_parameter("stripe_speed", stripe_speed)
	shader_material.set_shader_parameter("stripe_density", stripe_density)
	shader_material.set_shader_parameter("stripe_width", stripe_width)
	shader_material.set_shader_parameter("stripe_strength", stripe_strength)
