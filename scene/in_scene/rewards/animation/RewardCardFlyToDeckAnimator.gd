extends RefCounted


## RewardCardFlyToDeckAnimator 只负责奖励页卡牌飞入牌库的视觉动画。
## 它不写入牌组数据，不同步抽牌堆，也不决定奖励页关闭或商店状态。


const TRAIL_MAX_POINTS := 20
const TRAIL_TIMER_WAIT := 0.01


func play(owner: Node, card: Control, target_position: Vector2, duration: float, color: Color, width: float, finished_callback: Callable) -> void:
	var trail := Line2D.new()
	trail.width = width
	trail.default_color = color
	trail.z_index = card.z_index - 1

	var curve := Curve.new()
	curve.add_point(Vector2(0, 0))
	curve.add_point(Vector2(1, 1))
	trail.width_curve = curve
	owner.add_child(trail)

	var tw := owner.create_tween().set_parallel(true).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
	tw.tween_property(card, "global_position", target_position, duration)
	tw.tween_property(card, "scale", Vector2.ZERO, duration)
	tw.tween_property(card, "rotation", PI * 2, duration)

	var trail_timer := Timer.new()
	trail_timer.wait_time = TRAIL_TIMER_WAIT
	trail_timer.autostart = true
	owner.add_child(trail_timer)

	trail_timer.timeout.connect(func():
		trail.add_point(card.global_position + card.size / 2)
		if trail.get_point_count() > TRAIL_MAX_POINTS:
			trail.remove_point(0)
	)

	tw.chain().tween_callback(func():
		trail_timer.queue_free()
		trail.queue_free()
		card.queue_free()
		finished_callback.call()
	)
