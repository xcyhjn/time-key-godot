extends RefCounted

## CraftSelectionCardStatePresenter 只负责合成选择列表卡牌的视觉状态和点击入口。
## 它不创建卡牌，不读取真实卡数据，不写选择状态，也不修改牌组。


func apply_selection_card_state(
	draft_card: Control,
	entry: Dictionary,
	incompatible_card_alpha: float,
	card_clicked_callback: Callable
) -> void:
	if entry["selected"] and draft_card.has_method("set_selected"):
		draft_card.set_selected(true)
		if draft_card.has_method("set_tooltip_enabled"):
			draft_card.set_tooltip_enabled(true)

	if entry["dimmed"]:
		if draft_card.has_method("set_dimmed"):
			draft_card.set_dimmed(true, 1.0 - incompatible_card_alpha)
		if draft_card.has_method("set_hover_effect_enabled"):
			draft_card.set_hover_effect_enabled(false)
	else:
		if draft_card.has_method("set_dimmed"):
			draft_card.set_dimmed(false)
		if draft_card.has_method("set_hover_effect_enabled"):
			draft_card.set_hover_effect_enabled(true)

	if entry["disabled"]:
		draft_card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		if draft_card.has_method("set_tooltip_enabled"):
			draft_card.set_tooltip_enabled(false)
	else:
		draft_card.card_clicked.connect(card_clicked_callback.bind(entry))
