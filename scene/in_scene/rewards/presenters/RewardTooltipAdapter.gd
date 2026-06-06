extends RefCounted


## RewardTooltipAdapter 只负责奖励页卡牌 Tooltip 的初始化、显示和隐藏。
## 它不判断奖励是否可领取，不读取或修改牌组，也不创建奖励卡牌。


const TOOLTIP_OPTIONS := {
	"show_keywords": false,
	"fallback_text": "无效果文本",
}


func ensure_presenter(
	current_presenter: CardTooltipPresenter,
	host: Node,
	tooltip_config: TooltipConfig
) -> CardTooltipPresenter:
	if current_presenter != null:
		return current_presenter
	return CardTooltipPresenter.new(host, tooltip_config, false)


func show_card_tooltip(
	current_presenter: CardTooltipPresenter,
	host: Node,
	tooltip_config: TooltipConfig,
	card: Control
) -> CardTooltipPresenter:
	var presenter := ensure_presenter(current_presenter, host, tooltip_config)
	presenter.show_card_tooltip(card, TOOLTIP_OPTIONS)
	return presenter


func hide_tooltip(current_presenter: CardTooltipPresenter) -> void:
	if current_presenter != null:
		current_presenter.hide_tooltip()
