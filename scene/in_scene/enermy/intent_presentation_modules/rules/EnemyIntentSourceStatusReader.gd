extends RefCounted


## EnemyIntentSourceStatusReader 只负责从敌人意图来源节点读取状态说明行和状态关键词。
## 它不创建 tooltip，不写入 MainBoard，不定位任何 UI，也不驱动地图或时间轴表现。


func get_status_lines(source_node: Node) -> Array[String]:
	if not is_instance_valid(source_node):
		return []
	if not source_node.has_method("get_status_tooltip_lines"):
		return []

	var raw_lines: Variant = source_node.get_status_tooltip_lines()
	if not (raw_lines is Array):
		return []

	var result: Array[String] = []
	for line in raw_lines:
		result.append(str(line))
	return result


func get_status_keywords(source_node: Node) -> Array[String]:
	if not is_instance_valid(source_node):
		return []
	if not source_node.has_method("get_status_keyword_names"):
		return []

	var raw_keywords: Variant = source_node.get_status_keyword_names()
	if not (raw_keywords is Array):
		return []

	var result: Array[String] = []
	for keyword in raw_keywords:
		var keyword_name := str(keyword)
		if keyword_name != "" and not result.has(keyword_name):
			result.append(keyword_name)
	return result
