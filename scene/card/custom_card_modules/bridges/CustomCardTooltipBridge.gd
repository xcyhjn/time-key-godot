extends RefCounted


## CustomCardTooltipBridge 只负责为 CustomCard 查找 MainBoard 并转发 tooltip 显隐请求。
## 它不负责生成 tooltip 内容、不判断卡牌状态、不创建 tween，也不处理选中、拖拽或出牌流程。

func request_tooltip(card: Control, should_show: bool) -> void:
	if not is_instance_valid(card):
		return

	var tree: SceneTree = card.get_tree()
	if tree == null:
		return

	var main: Node = tree.get_first_node_in_group("MainBoard")
	if main == null or not main.has_method("show_tooltip"):
		return

	if should_show:
		main.show_tooltip(card)
	else:
		main.hide_tooltip(card)
