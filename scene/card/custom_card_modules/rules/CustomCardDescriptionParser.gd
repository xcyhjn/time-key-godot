extends RefCounted


## CustomCardDescriptionParser 只负责把卡牌原始描述解析为 BBCode 文本和关键词列表。
## 它不负责请求 tooltip、不读取或修改卡牌节点状态，也不处理选中、拖拽或出牌流程。


func parse(
	raw_description: String,
	base_stats: Dictionary,
	current_stats: Dictionary,
	keywords: Dictionary,
	icons: Dictionary
) -> Dictionary:
	var final_text: String = raw_description
	var active_keywords: Array = []

	final_text = _apply_stat_placeholders(final_text, base_stats, current_stats)
	var keyword_result: Dictionary = _apply_keywords(final_text, keywords)
	final_text = keyword_result.get("text", final_text)
	active_keywords = keyword_result.get("keywords", [])
	final_text = _apply_icons(final_text, icons)

	return {
		"text": final_text,
		"keywords": active_keywords,
	}


func _apply_stat_placeholders(text: String, base_stats: Dictionary, current_stats: Dictionary) -> String:
	var result: String = text

	for stat_key in current_stats.keys():
		var placeholder: String = "{" + str(stat_key) + "}"
		if not (placeholder in result):
			continue

		var base_val: Variant = base_stats[stat_key]
		var cur_val: Variant = current_stats[stat_key]
		var val_str: String = str(cur_val)
		if cur_val > base_val:
			val_str = "[color=#55ff55]" + val_str + "[/color]"
		elif cur_val < base_val:
			val_str = "[color=#ff5555]" + val_str + "[/color]"
		result = result.replace(placeholder, val_str)

	return result


func _apply_keywords(text: String, keywords: Dictionary) -> Dictionary:
	var result: String = text
	var active_keywords: Array = []

	for keyword_name in keywords.keys():
		var keyword_text: String = str(keyword_name)
		if not (keyword_text in result):
			continue

		if not active_keywords.has(keyword_text):
			active_keywords.append(keyword_text)

		var keyword_data: Dictionary = keywords[keyword_name]
		if keyword_data.has("bbcode_wrap"):
			var format_string: String = str(keyword_data["bbcode_wrap"])
			result = result.replace(keyword_text, format_string % keyword_text)
		else:
			var keyword_color: String = str(keyword_data.get("color", "#ffffff"))
			var colored_keyword: String = "[color=" + keyword_color + "]" + keyword_text + "[/color]"
			result = result.replace(keyword_text, colored_keyword)

	return {
		"text": result,
		"keywords": active_keywords,
	}


func _apply_icons(text: String, icons: Dictionary) -> String:
	var result: String = text

	for icon_tag in icons.keys():
		var icon_text: String = str(icon_tag)
		if not (icon_text in result):
			continue

		var image_path: String = str(icons[icon_tag])
		var image_bbcode: String = "[img=20]" + image_path + "[/img]"
		result = result.replace(icon_text, image_bbcode)

	return result
