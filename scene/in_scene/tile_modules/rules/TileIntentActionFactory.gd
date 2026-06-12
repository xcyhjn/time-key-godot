extends RefCounted


## TileIntentActionFactory 只负责为 Tile 默认敌人意图创建 TimelineAction。
## 它不选择目标、不判断意图是否合法、不解析时间轴 shape，也不修改 tile 血量、贴图或地图拓扑。


const DEFAULT_INTENT_COLOR: Color = Color(0.8, 0.2, 0.2, 0.8)


func create_default_action(source_tile: Node, target_tile: Node, shape_coords: Array[Vector2i], landform_name: String, location: Vector2i) -> TimelineAction:
	var action_data: Dictionary = {
		"效果": "地形实体行动",
		"类型": landform_name,
		"位置": location,
		"目标": target_tile.position if target_tile else Vector2.ZERO
	}

	return TimelineAction.new(
		TimelineAction.Type.ENEMY,
		source_tile,
		target_tile,
		shape_coords,
		DEFAULT_INTENT_COLOR,
		action_data
	)
