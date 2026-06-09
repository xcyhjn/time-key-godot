extends RefCounted


## TimecoinHourglassShaderController 只负责沙漏图标的 shader 材质准备与震动参数写入。
## 它不负责连接时间币信号、不刷新数量文本，也不播放获得、消耗或不足动画。


const HOURGLASS_SHAKE_SHADER: Shader = preload("res://shaders/hourglass_shake.gdshader")


func initialize_material(hourglass_icon: TextureRect) -> ShaderMaterial:
	if not is_instance_valid(hourglass_icon):
		return null

	if hourglass_icon.material == null:
		var new_material := ShaderMaterial.new()
		new_material.shader = HOURGLASS_SHAKE_SHADER
		hourglass_icon.material = new_material
		print("[TimecoinUI] 已为沙漏图标创建Shader材质")
		return new_material

	if hourglass_icon.material is ShaderMaterial:
		print("[TimecoinUI] 已获取现有Shader材质")
		return hourglass_icon.material as ShaderMaterial

	push_warning("[TimecoinUI] 沙漏图标已有材质，但不是ShaderMaterial")
	return null


func start_shake(material: ShaderMaterial, intensity: float, frequency: float) -> void:
	if material == null:
		return

	material.set_shader_parameter("shake_enabled", 1.0)
	material.set_shader_parameter("shake_intensity", intensity)
	material.set_shader_parameter("shake_frequency", frequency)
	print("[TimecoinUI] 已启动沙漏震动效果")


func stop_shake(material: ShaderMaterial) -> void:
	if material == null:
		return

	material.set_shader_parameter("shake_enabled", 0.0)
	print("[TimecoinUI] 已停止沙漏震动效果")


func set_shake_intensity(material: ShaderMaterial, intensity: float) -> void:
	if material:
		material.set_shader_parameter("shake_intensity", intensity)


func set_shake_frequency(material: ShaderMaterial, frequency: float) -> void:
	if material:
		material.set_shader_parameter("shake_frequency", frequency)
