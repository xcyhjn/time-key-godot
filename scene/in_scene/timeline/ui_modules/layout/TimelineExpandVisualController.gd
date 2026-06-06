extends RefCounted

## TimelineExpandVisualController 只负责 TimelineUI 展开/收起时的遮罩和地图交互表现。
## 它不处理时间轴行动数据，不创建行动方块，也不参与拖拽放置判断。


func create_background_mask(owner: Control, mask_color: Color, mask_layer: int) -> ColorRect:
	if not is_instance_valid(owner):
		return null

	var existing := owner.get_node_or_null("BackgroundMask") as ColorRect
	if is_instance_valid(existing):
		return existing

	var mask := ColorRect.new()
	mask.name = "BackgroundMask"
	mask.anchors_preset = Control.PRESET_FULL_RECT
	mask.grow_horizontal = Control.GROW_DIRECTION_BOTH
	mask.grow_vertical = Control.GROW_DIRECTION_BOTH
	mask.color = mask_color
	mask.z_index = mask_layer
	mask.mouse_filter = Control.MOUSE_FILTER_IGNORE
	mask.visible = false

	owner.add_child(mask)
	owner.move_child(mask, 0)
	return mask


func animate_background_mask(
	tween: Tween,
	background_mask: ColorRect,
	is_expanded: bool,
	anim_duration: float,
	should_hide_mask: Callable
) -> void:
	if not is_instance_valid(tween) or not is_instance_valid(background_mask):
		return

	if is_expanded:
		background_mask.visible = true
		background_mask.modulate = Color.TRANSPARENT
		tween.parallel().tween_property(background_mask, "modulate", Color.WHITE, anim_duration * 0.5)
	else:
		tween.parallel().tween_property(background_mask, "modulate", Color.TRANSPARENT, anim_duration * 0.5)
		tween.tween_callback(func():
			if is_instance_valid(background_mask) and should_hide_mask.is_valid() and bool(should_hide_mask.call()):
				background_mask.visible = false
		)


func set_map_interaction_for_expand(owner: Control, is_expanded: bool) -> void:
	if not is_instance_valid(owner):
		return

	var tree := owner.get_tree()
	if tree == null:
		return

	var map_node := tree.root.find_child("map", true, false)
	if map_node and map_node is Control:
		map_node.mouse_filter = Control.MOUSE_FILTER_IGNORE if is_expanded else Control.MOUSE_FILTER_PASS
