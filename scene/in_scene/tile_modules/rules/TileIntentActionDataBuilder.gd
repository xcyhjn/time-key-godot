extends RefCounted

## TileIntentActionDataBuilder 只负责组装 Tile 敌人意图使用的 action_data 字典。
## 它不创建 TimelineAction、不选择目标、不判断意图是否合法，也不读取或修改地图、血量、贴图和状态组件。


func build_standard_action_data(
	description: String,
	landform_name: String,
	location: Vector2i,
	target_tile: Node,
	effect_range: Variant,
	invalid_reason: String = ""
) -> Dictionary:
	return {
		"效果": description,
		"类型": landform_name,
		"位置": location,
		"目标": target_tile.position if target_tile else Vector2.ZERO,
		"effect_range": effect_range,
		"invalid_reason": invalid_reason
	}
