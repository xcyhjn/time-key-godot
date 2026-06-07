extends RefCounted

## TimelineActionRemovalAnimator 只负责播放时间轴行动残影的移除动画。
## 它不创建残影，不修改 TimelineManager 数据，不维护 action_containers，也不处理原行动容器释放。


func animate_removal(
	ghost: Control,
	tween_factory: Callable,
	fade_color: Color,
	drop_distance: float,
	target_scale: Vector2,
	duration: float
) -> void:
	if not is_instance_valid(ghost):
		return
	if not tween_factory.is_valid():
		ghost.queue_free()
		return

	var tw = tween_factory.call()
	if not (tw is Tween):
		ghost.queue_free()
		return

	tw.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN)
	tw.tween_property(ghost, "modulate", fade_color, duration)
	tw.parallel().tween_property(ghost, "position:y", ghost.position.y + drop_distance, duration)
	tw.parallel().tween_property(ghost, "scale", target_scale, duration)
	tw.tween_callback(func():
		if is_instance_valid(ghost):
			ghost.queue_free()
	)
