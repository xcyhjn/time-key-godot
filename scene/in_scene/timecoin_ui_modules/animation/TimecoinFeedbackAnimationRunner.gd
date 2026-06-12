extends RefCounted


## TimecoinFeedbackAnimationRunner 只负责把时间币 UI 的获得、消耗和不足反馈动画片段追加到传入 Tween。
## 它不创建 Tween、不管理 active_tweens、不连接 GlobalTimecoin 信号，也不刷新数值文本或控制沙漏 shader。


const WARNING_FLASH_COLOR: Color = Color(2.0, 0.2, 0.2, 1.0)
const GAIN_HIGHLIGHT_COLOR: Color = Color(1.5, 1.5, 1.0, 1.0)
const WARNING_SHAKE_AMOUNT: float = 3.0
const WARNING_SHAKE_DURATION: float = 0.4
const WARNING_FLASH_DURATION: float = 0.1


func append_gain_animation(
	tween: Tween,
	ui_container: Control,
	hourglass_icon: TextureRect,
	original_scale: Vector2,
	original_icon_color: Color,
	gain_scale_amount: float,
	gain_scale_duration: float,
	gain_shake_amount: float,
	gain_shake_duration: float,
	shake_callback: Callable
) -> void:
	if tween == null:
		return
	if not is_instance_valid(ui_container):
		return

	tween.tween_property(
		ui_container,
		"scale",
		original_scale * gain_scale_amount,
		gain_scale_duration * 0.5
	).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)

	tween.tween_property(
		ui_container,
		"scale",
		original_scale,
		gain_scale_duration * 0.5
	).set_delay(gain_scale_duration * 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)

	_call_shake(shake_callback, tween, gain_shake_amount, gain_shake_duration)

	if is_instance_valid(hourglass_icon):
		tween.tween_property(
			hourglass_icon,
			"modulate",
			GAIN_HIGHLIGHT_COLOR,
			gain_scale_duration * 0.3
		).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

		tween.tween_property(
			hourglass_icon,
			"modulate",
			original_icon_color,
			gain_scale_duration * 0.7
		).set_delay(gain_scale_duration * 0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)


func append_consume_animation(
	tween: Tween,
	hourglass_icon: TextureRect,
	original_icon_color: Color,
	consume_shake_amount: float,
	consume_shake_duration: float,
	consume_color_tint: Color,
	shake_callback: Callable
) -> void:
	if tween == null:
		return

	_call_shake(shake_callback, tween, consume_shake_amount, consume_shake_duration)

	if is_instance_valid(hourglass_icon):
		tween.tween_property(
			hourglass_icon,
			"modulate",
			consume_color_tint,
			consume_shake_duration * 0.2
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

		tween.tween_property(
			hourglass_icon,
			"modulate",
			original_icon_color,
			consume_shake_duration * 0.8
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)


func append_warning_animation(
	tween: Tween,
	hourglass_icon: TextureRect,
	original_icon_color: Color,
	shake_callback: Callable
) -> void:
	if tween == null:
		return

	if is_instance_valid(hourglass_icon):
		tween.tween_property(
			hourglass_icon,
			"modulate",
			WARNING_FLASH_COLOR,
			WARNING_FLASH_DURATION
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

		tween.chain().tween_property(
			hourglass_icon,
			"modulate",
			original_icon_color,
			WARNING_FLASH_DURATION
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

		tween.chain().tween_property(
			hourglass_icon,
			"modulate",
			WARNING_FLASH_COLOR,
			WARNING_FLASH_DURATION
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

		tween.chain().tween_property(
			hourglass_icon,
			"modulate",
			original_icon_color,
			WARNING_FLASH_DURATION
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)

	_call_shake(shake_callback, tween, WARNING_SHAKE_AMOUNT, WARNING_SHAKE_DURATION)


func _call_shake(shake_callback: Callable, tween: Tween, shake_amount: float, duration: float) -> void:
	if shake_callback.is_valid():
		shake_callback.call(tween, shake_amount, duration)
