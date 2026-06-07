extends RefCounted

## CraftPreviewCardConfigurator 只负责配置已完成数据写入的合成预览卡 UI 状态。
## 它不创建预览卡，不读取真实卡数据，也不管理临时牌堆生命周期。


func configure_preview_card(
	draft_card: Control,
	preview_size: Vector2,
	tooltip_enabled: bool
) -> Control:
	if not is_instance_valid(draft_card):
		return null

	draft_card.custom_minimum_size = preview_size
	draft_card.size = preview_size
	draft_card.position = Vector2.ZERO

	if not tooltip_enabled:
		draft_card.mouse_filter = Control.MOUSE_FILTER_IGNORE

	return draft_card
