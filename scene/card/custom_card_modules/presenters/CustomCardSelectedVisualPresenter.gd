extends RefCounted


## CustomCardSelectedVisualPresenter 只负责卡牌选中状态下的倾斜和阴影跟随表现。
## 它不负责修改选中状态、请求 tooltip、处理出牌/拖拽，也不调整手牌布局。

func update_selected_follow_visual(
	card: Control,
	shadow: TextureRect,
	delta: float,
	tilt_intensity: float,
	tilt_follow_speed: float,
	shadow_follow_speed: float,
	shadow_base_offset: Vector2,
	shadow_parallax_ratio: float
) -> void:
	if not is_instance_valid(card):
		return

	var center: Vector2 = card.size / 2.0
	var mouse_local: Vector2 = card.get_local_mouse_position() - center
	var clamped_offset: Vector2 = mouse_local.clamp(Vector2(-200.0, -200.0), Vector2(200.0, 200.0))
	var target_rot: float = clamped_offset.x * (tilt_intensity * 0.01)

	card.rotation = lerp(card.rotation, target_rot, delta * tilt_follow_speed)

	if is_instance_valid(shadow):
		var target_shadow_pos: Vector2 = shadow_base_offset - (clamped_offset * shadow_parallax_ratio)
		shadow.position = lerp(shadow.position, target_shadow_pos, delta * shadow_follow_speed)
