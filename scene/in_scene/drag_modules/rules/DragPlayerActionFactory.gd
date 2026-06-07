extends RefCounted


## DragPlayerActionFactory 只负责从当前拖拽上下文创建玩家 TimelineAction。
## 它不调用 TimelineManager，不发射信号，也不移动卡牌到弃牌区。


func create_action(card: Control, target_tile: Node, shape_coords: Array[Vector2i]) -> TimelineAction:
	return TimelineAction.new(
		TimelineAction.Type.PLAYER,
		card,
		target_tile,
		shape_coords,
		Color.AQUA,
		card.card_info
	)
