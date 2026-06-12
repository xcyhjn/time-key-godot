extends RefCounted


## TimecoinShakeTweenBuilder 只负责把时间币 UI 的位置抖动片段追加到传入 Tween。
## 它不创建 Tween、不管理 active_tweens，也不连接时间币信号或修改数值显示。


const SHAKE_POINTS: int = 5
const VERTICAL_SHAKE_RATIO: float = 0.3
const STEP_DURATION_RATIO: float = 0.8
const RETURN_DURATION_RATIO: float = 0.2


func append_shake(tween: Tween, target: Control, original_position: Vector2, shake_amount: float, duration: float) -> void:
	if tween == null:
		return
	if not is_instance_valid(target):
		return

	var safe_duration: float = maxf(duration, 0.0)
	if safe_duration <= 0.0 or shake_amount <= 0.0:
		tween.tween_property(target, "position", original_position, 0.0)
		return

	for i in range(SHAKE_POINTS):
		var time_point: float = float(i) / float(SHAKE_POINTS) * safe_duration
		var shake_direction := Vector2(
			randf_range(-shake_amount, shake_amount),
			randf_range(-shake_amount * VERTICAL_SHAKE_RATIO, shake_amount * VERTICAL_SHAKE_RATIO)
		)

		tween.tween_property(
			target,
			"position",
			original_position + shake_direction,
			safe_duration / float(SHAKE_POINTS) * STEP_DURATION_RATIO
		).set_delay(time_point).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

	tween.tween_property(
		target,
		"position",
		original_position,
		safe_duration / float(SHAKE_POINTS) * RETURN_DURATION_RATIO
	).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
