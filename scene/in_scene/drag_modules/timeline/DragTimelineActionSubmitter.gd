extends RefCounted


## DragTimelineActionSubmitter 只负责把已创建的 TimelineAction 提交给 TimelineManager。
## 它不创建行动，不播放失败动画，不清理预览，也不移动卡牌到弃牌区。


func submit_action(
	timeline_manager: Node,
	action: TimelineAction,
	grid_pos: Vector2i,
	success_callback: Callable
) -> bool:
	if not is_instance_valid(timeline_manager):
		return false

	var placement_success: bool = timeline_manager.place_action(action, grid_pos)
	if placement_success and success_callback.is_valid():
		success_callback.call(action)
	return placement_success
