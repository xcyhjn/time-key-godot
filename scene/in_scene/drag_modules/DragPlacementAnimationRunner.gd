class_name DragPlacementAnimationRunner
extends RefCounted


## DragPlacementAnimationRunner 只负责播放卡牌飞向时间轴格子的放置动画。
## 它不判断放置是否合法，不执行时间轴放置，也不改变卡牌归属或拖拽状态。


func play_placement_animation(
	card: Control,
	target_top_left: Vector2,
	finished_callback: Callable,
	tween_factory: Callable,
	target_scale: Vector2 = Vector2(0.9, 0.9),
	target_alpha: float = 0.7,
	duration: float = 0.3
) -> void:
	if not is_instance_valid(card):
		return

	if not tween_factory.is_valid():
		return

	var tween = tween_factory.call()
	if tween == null:
		return

	tween.set_parallel(true)
	tween.tween_property(card, "global_position", target_top_left, duration) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(card, "scale", target_scale, duration) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(card, "modulate:a", target_alpha, duration) \
		.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	if finished_callback.is_valid():
		tween.finished.connect(finished_callback)
