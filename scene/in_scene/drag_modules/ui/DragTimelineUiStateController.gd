extends RefCounted


## DragTimelineUiStateController 只负责拖拽期间时间轴 UI 的展开、收起和点击展开开关。
## 它不处理预览格子，不判断放置是否合法，也不移动卡牌。


func enter_drag_mode(timeline_ui: Control) -> void:
	if not is_instance_valid(timeline_ui):
		return

	if timeline_ui.has_method("set_allow_click_to_expand"):
		timeline_ui.set_allow_click_to_expand(true)

	if timeline_ui.has_method("toggle_expand"):
		timeline_ui.is_expanded = false
		timeline_ui.toggle_expand()

	timeline_ui.mouse_filter = Control.MOUSE_FILTER_PASS


func exit_drag_mode(timeline_ui: Control) -> void:
	if not is_instance_valid(timeline_ui):
		return

	collapse(timeline_ui)

	if timeline_ui.has_method("set_allow_click_to_expand"):
		timeline_ui.set_allow_click_to_expand(false)


func collapse(timeline_ui: Control) -> void:
	if not is_instance_valid(timeline_ui):
		return

	if timeline_ui.has_method("collapse"):
		timeline_ui.collapse()
	elif timeline_ui.has_method("toggle_expand"):
		timeline_ui.toggle_expand()
