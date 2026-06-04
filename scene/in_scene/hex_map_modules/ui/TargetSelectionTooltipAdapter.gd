class_name TargetSelectionTooltipAdapter
extends RefCounted


## TargetSelectionTooltipAdapter 负责把地图目标 hover 的展示计划翻译给 MainBoard。
## TargetAoeHoverPresenter 只负责生成 plan，HexMap 只负责转交 plan；
## 具体仍调用 MainBoard 旧接口，是为了本批不改 UI 节点和旧函数名。

## 根据展示计划刷新 MainBoard 的目标选择 tooltip。
## 当前保持旧行为：只有 plan 明确提供了有效 Area2D stack 时，才调用
## `update_target_selection_hover(center_stack, card)`。
func apply(main_board: Node, display_plan: Dictionary) -> void:
	if not is_instance_valid(main_board):
		return
	if not main_board.has_method("update_target_selection_hover"):
		return

	var tooltip_stack: Variant = display_plan.get("tooltip_stack", null)
	var tooltip_card: Variant = display_plan.get("tooltip_card", null)
	if is_instance_valid(tooltip_stack) and tooltip_stack is Area2D:
		main_board.update_target_selection_hover(tooltip_stack, tooltip_card)
