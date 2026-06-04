class_name CardTooltipUiAdapter
extends RefCounted


## CardTooltipUiAdapter 是 InScene 与共享 CardTooltipPresenter 之间的薄桥。
## 它保留旧的 card_manager 查找和“选中卡牌不抢 tooltip”规则，不负责绘制 tooltip 内容。


func ensure_presenter(owner: Node, presenter: Variant, tooltip_config: Variant) -> CardTooltipPresenter:
	if presenter != null:
		return presenter as CardTooltipPresenter
	return CardTooltipPresenter.new(owner, tooltip_config, true)


func setup_ui(owner: Node, presenter: Variant, tooltip_config: Variant) -> CardTooltipPresenter:
	var active_presenter: CardTooltipPresenter = ensure_presenter(owner, presenter, tooltip_config)
	active_presenter.ensure_ui_created()
	return active_presenter


func show_tooltip(owner: Node, presenter: Variant, tooltip_config: Variant, card: Control) -> CardTooltipPresenter:
	var card_manager: Variant = _find_card_manager(owner)
	if not card_manager:
		return presenter as CardTooltipPresenter

	var selected_card: Variant = card_manager.get("current_selected_card")
	if selected_card != null and selected_card != card:
		return presenter as CardTooltipPresenter

	var active_presenter: CardTooltipPresenter = ensure_presenter(owner, presenter, tooltip_config)
	active_presenter.show_card_tooltip(card, {
		"show_keywords": true,
		"fallback_text": "",
	})
	return active_presenter


func hide_tooltip(owner: Node, presenter: Variant, _tooltip_config: Variant, _card: Control = null) -> CardTooltipPresenter:
	var card_manager: Variant = _find_card_manager(owner)
	if card_manager and card_manager.get("current_selected_card") != null:
		return presenter as CardTooltipPresenter

	if presenter != null:
		var active_presenter: CardTooltipPresenter = presenter as CardTooltipPresenter
		active_presenter.hide_tooltip()
		return active_presenter
	return null


func _find_card_manager(owner: Node) -> Variant:
	if not is_instance_valid(owner):
		return null

	var tree: SceneTree = owner.get_tree()
	if not is_instance_valid(tree):
		return null

	var tree_root: Window = tree.root
	if tree_root and tree_root.has_meta("card_manager"):
		return tree_root.get_meta("card_manager")

	var current_scene: Node = tree.current_scene
	if current_scene and current_scene.has_meta("card_manager"):
		return current_scene.get_meta("card_manager")

	return null
