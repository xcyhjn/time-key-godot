extends RefCounted


## CustomCardMapConditionalEffectBridge 只负责从 MainBoard 查找 HexMap 并请求刷新地块条件效果。
## 它不负责判断卡牌状态、不计算条件效果、不修改地图数据，也不处理选中、拖拽或出牌流程。

func update_map_conditional_effects(main_board: Node) -> void:
	if not is_instance_valid(main_board):
		return

	var hex_map: Node = main_board.get_node_or_null("../../map/HexMap")
	if hex_map != null and hex_map.has_method("update_all_stack_conditional_effects"):
		hex_map.update_all_stack_conditional_effects()
