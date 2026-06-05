class_name DragPlacementVisualStatePreparer
extends RefCounted


## DragPlacementVisualStatePreparer 只负责卡牌进入放置动画前的视觉状态准备。
## 它不计算目标格位置，不播放飞行动画，也不执行时间轴放置或卡牌归属变更。


func prepare_for_placement(card: Control) -> void:
	if not is_instance_valid(card):
		return

	card.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if card.material:
		card.material.set_shader_parameter("is_invalid", false)
		card.material.set_shader_parameter("drag_visual_state", 0)
