class_name DragFreeDragPresenter
extends RefCounted


## DragFreeDragPresenter 只负责普通拖拽时让卡牌自由跟随鼠标并恢复拖拽视觉状态。
## 它不处理 clear 模式预览，不判断放置合法性，也不修改时间轴、手牌或弃牌区。


func update_free_drag(card: Control, mouse_pos: Vector2, drag_offset: Vector2) -> void:
	if not is_instance_valid(card):
		return

	var target_center: Vector2 = mouse_pos - drag_offset
	var card_top_left: Vector2 = target_center
	if card.has_method("get_size"):
		var card_size: Vector2 = card.get_size()
		var card_scale: Vector2 = card.scale
		card_top_left -= card_size * card_scale / 2.0

	card.global_position = card_top_left
	if card.material:
		card.material.set_shader_parameter("is_invalid", false)
		card.material.set_shader_parameter("drag_visual_state", 0)
