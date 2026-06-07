extends RefCounted


## DragSuccessCardVisualRestorer 只负责成功放置后恢复卡牌自身视觉状态。
## 它不发射拖拽信号，不移动卡牌到弃牌区，也不收起时间轴。


func restore(card: Control) -> void:
	if not is_instance_valid(card):
		return

	if card.has_method("force_reset_visuals"):
		card.force_reset_visuals()

	if _object_has_property(card, &"card_current_state"):
		card.set("card_current_state", 0)

	card.mouse_filter = Control.MOUSE_FILTER_STOP
	card.modulate = Color.WHITE
	card.scale = Vector2.ONE

	if _object_has_property(card, &"original_material"):
		var original_material: Variant = card.get("original_material")
		card.set("material", original_material)

		var tex_node: Object = card.get("front_face_texture") as Object
		if tex_node != null and _object_has_property(tex_node, &"material"):
			tex_node.set("material", original_material)

	if card.has_method("_set_shader"):
		card._set_shader(false)
	if card.has_method("set_card_transparency"):
		card.set_card_transparency(1.0)


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info: Dictionary in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
