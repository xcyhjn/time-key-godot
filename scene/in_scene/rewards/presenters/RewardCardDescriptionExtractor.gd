extends RefCounted


## RewardCardDescriptionExtractor 只负责从真实卡牌节点读取奖励页展示用效果文本。
## 它不创建卡牌，不修改 DraftCard，也不参与 Tooltip 或奖励确认流程。


func extract_description(real_card: Node, owner: Node) -> String:
	if real_card == null:
		return ""

	if real_card.has_method("setup_card_data"):
		real_card.setup_card_data()
		if owner != null and owner.is_inside_tree():
			await owner.get_tree().process_frame

	if real_card.has_method("get_parsed_description"):
		var parsed = real_card.get_parsed_description()
		if parsed != "":
			return parsed

	if _object_has_property(real_card, &"raw_description"):
		var raw_description = real_card.get("raw_description")
		if typeof(raw_description) == TYPE_STRING and raw_description != "":
			return raw_description

	var card_info = real_card.get("card_info")
	if typeof(card_info) == TYPE_DICTIONARY and card_info.has("效果"):
		return str(card_info.get("效果", ""))

	return ""


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false

	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true

	return false
