extends RefCounted


## EnemyIntentTooltipTextBuilder 只负责组装敌人意图主 tooltip 的文本行。
## 它不写入 MainBoard.cursor_tooltip、不创建状态关键词副 tooltip、不定位 tooltip，也不判断地图或时间轴 hover 规则。


const DEFAULT_INVALID_REASON := "无可用目标"


func build_lines(intent_data: EnemyIntentData, source_status_lines: Array[String]) -> Array[String]:
	var lines: Array[String] = []
	if intent_data == null:
		return lines

	lines.append(intent_data.description)
	lines.append_array(source_status_lines)

	if not intent_data.is_valid:
		var invalid_reason := intent_data.invalid_reason if intent_data.invalid_reason != "" else DEFAULT_INVALID_REASON
		lines.append("[color=#ff5555]%s[/color]" % invalid_reason)

	return lines
