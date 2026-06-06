extends RefCounted


## RewardDraftCardDataApplier 只负责把真实卡牌读取到的数据写入奖励页 DraftCard。
## 它不创建真实卡牌，不清理临时牌堆，也不处理奖励页选择或确认流程。


func apply_data(draft_card: Control, real_card: Node, card_id: String, description_reader, texture_reader) -> void:
	draft_card.raw_description = await description_reader.call(real_card)

	if _object_has_property(real_card, &"active_keywords") and real_card.get("active_keywords") is Array:
		draft_card.active_keywords = real_card.get("active_keywords").duplicate()

	var front_texture = await texture_reader.call(real_card, card_id)
	if front_texture != null:
		draft_card.texture = front_texture


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false

	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true

	return false
