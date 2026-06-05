class_name DragRejectAnimationRunner
extends RefCounted


## DragRejectAnimationRunner 只负责拖拽放置失败时的卡牌抖动动画和提示隐藏计时。
## 它不判断是否可放置，不结束拖拽，也不修改时间轴、卡牌归属或回合状态。


func play_reject_animation(
	card: Control,
	show_tooltip_callback: Callable,
	hide_tooltip_callback: Callable,
	tween_factory: Callable,
	timer_factory: Callable,
	message: String = "无法放置"
) -> void:
	if not is_instance_valid(card):
		return

	if show_tooltip_callback.is_valid():
		show_tooltip_callback.call(message)

	if tween_factory.is_valid():
		var tw = tween_factory.call()
		if tw != null:
			var orig_x: float = card.position.x
			tw.tween_property(card, "position:x", orig_x - 10.0, 0.05)
			tw.tween_property(card, "position:x", orig_x + 10.0, 0.05)
			tw.tween_property(card, "position:x", orig_x, 0.05)

	if timer_factory.is_valid() and hide_tooltip_callback.is_valid():
		var timer = timer_factory.call(2.0)
		if timer != null and timer.has_signal("timeout"):
			timer.timeout.connect(hide_tooltip_callback)
