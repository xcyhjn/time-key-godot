extends RefCounted


## ChapterRevealAnimationRunner 只负责局外新章节地块的揭示动画。
## 它不推进 current_tier，不移动镜头，不保存 MapState，也不决定哪些地块需要揭示。


func prepare_tiles(coords_list: Array, tiles: Dictionary, hex_to_pixel: Callable, config: Dictionary) -> void:
	for coords in coords_list:
		if not tiles.has(coords):
			continue

		var tile := tiles[coords] as Sprite2D
		if tile == null:
			continue

		var final_pos: Vector2 = _call_vector2(hex_to_pixel, coords, tile.position)
		var fall_dist: float = randf_range(
			float(config.get("fall_distance_min", 500.0)),
			float(config.get("fall_distance_max", 700.0))
		)
		tile.visible = true
		tile.position = final_pos + Vector2(0.0, fall_dist)
		tile.rotation = randf_range(-1.2, 1.2)
		tile.scale = Vector2.ONE * float(config.get("start_scale", 0.8))
		tile.modulate.a = 0.0
		tile.self_modulate = config.get("start_tint", Color(0.1, 0.1, 0.1, 0.8))


func execute_animation(coords_list: Array, tiles: Dictionary, hex_to_pixel: Callable, tween_owner: Node, config: Dictionary, speed: float) -> void:
	if coords_list.is_empty() or tween_owner == null:
		return

	var delay_random: float = float(config.get("delay_random", 0.2))
	var rise_duration: float = float(config.get("rise_duration", 0.8)) * speed
	var settle_duration: float = float(config.get("settle_duration", 0.25)) * speed

	var rise_tween := tween_owner.create_tween().set_parallel(true)
	for coords in coords_list:
		if not tiles.has(coords):
			continue

		var tile := tiles[coords] as Sprite2D
		if tile == null:
			continue

		var delay: float = randf() * delay_random
		var final_pos: Vector2 = _call_vector2(hex_to_pixel, coords, tile.position)
		rise_tween.tween_property(tile, "modulate:a", 1.0, rise_duration).set_delay(delay)
		rise_tween.tween_property(tile, "position", final_pos, rise_duration)\
			.set_trans(Tween.TRANS_SINE)\
			.set_ease(Tween.EASE_OUT)\
			.set_delay(delay)
		rise_tween.tween_property(tile, "rotation", 0.0, rise_duration).set_delay(delay)
	await rise_tween.finished

	var settle_tween := tween_owner.create_tween().set_parallel(true)
	for coords in coords_list:
		if not tiles.has(coords):
			continue

		var tile := tiles[coords] as Sprite2D
		if tile == null:
			continue

		var delay: float = randf() * min(delay_random, 0.12)
		settle_tween.tween_property(tile, "scale", Vector2.ONE, settle_duration)\
			.set_trans(Tween.TRANS_BACK)\
			.set_ease(Tween.EASE_OUT)\
			.set_delay(delay)
		settle_tween.tween_property(tile, "self_modulate", Color(1, 1, 1, 1), settle_duration).set_delay(delay)
	await settle_tween.finished


func _call_vector2(callback: Callable, coords: Vector2i, fallback: Vector2) -> Vector2:
	if not callback.is_valid():
		return fallback

	var value: Variant = callback.call(coords)
	if value is Vector2:
		return value
	return fallback
