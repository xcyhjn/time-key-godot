extends RefCounted

## TimelineActionBlockPresenter 只负责创建单个时间轴行动方块的视觉节点。
## 它不连接 hover，不播放动画，不修改 TimelineManager 数据，也不决定方块生命周期。


func build_config(slot_size: float, style: StyleBox, enemy_overlay_material: Material) -> Dictionary:
	return {
		"slot_size": slot_size,
		"style": style,
		"enemy_overlay_material": enemy_overlay_material,
	}


func create_action_block_style(action_color: Color, action_group_visual_enabled: bool) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = action_color
	if action_group_visual_enabled:
		style.set_border_width_all(0)
		style.border_color = Color.TRANSPARENT
	else:
		style.set_border_width_all(2)
		style.border_color = Color.BLACK
	return style


func create_action_block(
	target_grid_pos: Vector2i,
	local_position: Vector2,
	slot_size: float,
	style: StyleBox,
	enemy_overlay_material: Material
) -> Panel:
	return create_action_block_from_config(
		target_grid_pos,
		local_position,
		build_config(slot_size, style, enemy_overlay_material)
	)


func create_action_block_from_config(
	target_grid_pos: Vector2i,
	local_position: Vector2,
	config: Dictionary
) -> Panel:
	var slot_size: float = config["slot_size"]
	var style: StyleBox = config["style"]
	var enemy_overlay_material: Material = config["enemy_overlay_material"]
	var block := Panel.new()
	block.name = "ActionBlock_%s_%s" % [target_grid_pos.x, target_grid_pos.y]
	block.add_theme_stylebox_override("panel", style)
	block.size = Vector2(slot_size, slot_size)
	block.custom_minimum_size = Vector2(slot_size, slot_size)
	block.position = local_position
	block.mouse_filter = Control.MOUSE_FILTER_PASS
	block.add_child(_create_enemy_intent_overlay(enemy_overlay_material))
	return block


func _create_enemy_intent_overlay(enemy_overlay_material: Material) -> ColorRect:
	var overlay := ColorRect.new()
	overlay.name = "EnemyIntentOverlay"
	overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
	overlay.visible = false
	overlay.color = Color.WHITE
	overlay.material = enemy_overlay_material
	return overlay
