extends RefCounted

## TimelineActionRemovalGhostBuilder 只负责从即将移除的时间轴行动容器生成清理动画残影。
## 它不启动 Tween，不修改 TimelineManager 数据，不维护 action_containers，也不清理原行动容器。


func create_ghost(source_container: Control, action_id: int, reason: String, preview_z_index: int) -> Control:
	if not is_instance_valid(source_container):
		return null

	var parent := source_container.get_parent()
	if parent == null:
		return null

	var ghost := Control.new()
	ghost.name = "ActionRemovalGhost_%s" % action_id
	ghost.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ghost.size = source_container.size
	ghost.position = source_container.position
	ghost.pivot_offset = source_container.pivot_offset
	ghost.scale = Vector2.ONE
	ghost.modulate = Color.WHITE
	ghost.z_index = max(source_container.z_index, preview_z_index + 1)
	ghost.set_meta("action_id", action_id)
	ghost.set_meta("removal_reason", reason)
	parent.add_child(ghost)
	parent.move_child(ghost, parent.get_child_count() - 1)

	for child in source_container.get_children():
		if child is TimelineActionShapeVisual:
			var source_visual := child as TimelineActionShapeVisual
			ghost.add_child(source_visual.clone_visual())
			continue

		if not (child is Panel):
			continue

		var source_block := child as Panel
		ghost.add_child(_create_ghost_block(source_block))

	return ghost


func _create_ghost_block(source_block: Panel) -> Panel:
	var ghost_block := Panel.new()
	ghost_block.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ghost_block.position = source_block.position
	ghost_block.size = source_block.size
	ghost_block.custom_minimum_size = source_block.custom_minimum_size
	ghost_block.pivot_offset = source_block.pivot_offset
	ghost_block.scale = source_block.scale
	ghost_block.rotation = source_block.rotation

	var source_style: StyleBox = source_block.get_theme_stylebox("panel")
	if source_style != null:
		ghost_block.add_theme_stylebox_override("panel", source_style.duplicate())

	return ghost_block
