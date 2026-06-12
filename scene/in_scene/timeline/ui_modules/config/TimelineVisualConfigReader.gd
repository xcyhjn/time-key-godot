extends RefCounted


## TimelineVisualConfigReader 只负责从时间轴视觉配置 Resource 读取静态视觉参数并提供类型兜底。
## 它不创建或修改 Resource，不读取 TimelineManager，不创建行动块，也不播放或控制任何 UI 动画。


func get_value(visual_config: Resource, property_name: StringName, fallback_value: Variant) -> Variant:
	if visual_config == null:
		return fallback_value
	var value: Variant = visual_config.get(property_name)
	return fallback_value if value == null else value


func get_color(visual_config: Resource, property_name: StringName, fallback_value: Color) -> Color:
	var value: Variant = get_value(visual_config, property_name, fallback_value)
	return value if value is Color else fallback_value


func get_vector2(visual_config: Resource, property_name: StringName, fallback_value: Vector2) -> Vector2:
	var value: Variant = get_value(visual_config, property_name, fallback_value)
	return value if value is Vector2 else fallback_value


func get_float(visual_config: Resource, property_name: StringName, fallback_value: float) -> float:
	return float(get_value(visual_config, property_name, fallback_value))


func get_int(visual_config: Resource, property_name: StringName, fallback_value: int) -> int:
	return int(get_value(visual_config, property_name, fallback_value))


func get_bool(visual_config: Resource, property_name: StringName, fallback_value: bool) -> bool:
	return bool(get_value(visual_config, property_name, fallback_value))


func get_shader(visual_config: Resource, property_name: StringName, fallback_value: Shader) -> Shader:
	var value: Variant = get_value(visual_config, property_name, fallback_value)
	return value if value is Shader else fallback_value
