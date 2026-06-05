class_name DragCardEffectPreviewTextResolver
extends RefCounted


## DragCardEffectPreviewTextResolver 只负责从卡牌描述生成拖拽效果预览文案和数值。
## 它不显示 tooltip，不查找敌人或血条，也不触发任何实际卡牌效果。


func get_preview_text(card: Control) -> String:
	if not is_instance_valid(card):
		return ""

	var raw_description: String = _get_raw_description(card)
	if raw_description.is_empty():
		raw_description = _strip_simple_bbcode(card.get_parsed_description())

	if "摧毁" in raw_description:
		return "摧毁目标：-10 生命值"
	elif "治疗" in raw_description:
		return "治疗目标：+8 生命值"
	elif "攻击" in raw_description:
		return "攻击目标：-5 生命值"

	var clean_text: String = raw_description.replace("[", "").replace("]", "")
	var preview: String = clean_text.substr(0, 20)
	if clean_text.length() > 20:
		preview += "..."
	return preview


func get_damage_amount(card: Control) -> int:
	if not is_instance_valid(card) or not card.has_method("get_parsed_description"):
		return 0

	var card_description: String = card.get_parsed_description()
	if "摧毁" in card_description:
		return 10
	elif "攻击" in card_description:
		return 5
	elif "治疗" in card_description:
		return 8
	return 0


func _get_raw_description(card: Control) -> String:
	if not card.has_method("get_parsed_description"):
		return ""

	if card.get("raw_description") != null:
		return str(card.raw_description)

	if card.get("card_info") != null and card.card_info is Dictionary and "效果" in card.card_info:
		return str(card.card_info["效果"])

	return ""


func _strip_simple_bbcode(text: String) -> String:
	return text.replace("[color=", "").replace("]", "").replace("[/color]", "")
