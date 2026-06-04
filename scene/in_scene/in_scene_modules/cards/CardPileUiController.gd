class_name CardPileUiController
extends RefCounted


## CardPileUiController 集中处理抽牌堆/弃牌堆按钮与计数显示。
## 它不移动卡牌、不洗牌、不改变战斗阶段，只把 UI 输入翻译成主脚本可以执行的动作。

const ACTION_NONE: String = "none"
const ACTION_OPEN_DECK: String = "open_deck"
const ACTION_DRAW_FROM_DECK: String = "draw_from_deck"


## 根据牌堆当前内容刷新两个数量标签。
## 返回最新计数，主脚本继续维护原来的 current_deck_count/current_discard_count。
func update_counts(deck_pile: Pile, discard_pile: Pile, deck_count_label: Label, discard_count_label: Label) -> Dictionary:
	var deck_count: int = _get_pile_count(deck_pile)
	var discard_count: int = _get_pile_count(discard_pile)

	if is_instance_valid(deck_count_label):
		deck_count_label.text = str(deck_count)
	if is_instance_valid(discard_count_label):
		discard_count_label.text = str(discard_count)

	return {
		"deck_count": deck_count,
		"discard_count": discard_count,
	}


## 把抽牌堆按钮输入转成动作。
## settlement 阶段永远打开查看器；战斗阶段是否左键抽牌由主脚本的导出开关决定。
func resolve_deck_button_action(event: InputEvent, config: Dictionary) -> Dictionary:
	if bool(config.get("is_processing_deck", false)):
		return {"action": ACTION_NONE, "handled": true}

	if not (event is InputEventMouseButton):
		return {"action": ACTION_NONE, "handled": false}

	var mouse_event: InputEventMouseButton = event as InputEventMouseButton
	if not mouse_event.pressed:
		return {"action": ACTION_NONE, "handled": false}

	if bool(config.get("is_settlement", false)):
		return {"action": ACTION_OPEN_DECK, "handled": true}

	if mouse_event.button_index == MOUSE_BUTTON_LEFT:
		if bool(config.get("enable_left_click_draw_from_deck", false)):
			return {
				"action": ACTION_DRAW_FROM_DECK,
				"handled": true,
				"draw_count": int(config.get("left_click_deck_draw_count", 1)),
			}
		return {"action": ACTION_OPEN_DECK, "handled": true}

	if mouse_event.button_index == MOUSE_BUTTON_RIGHT:
		return {"action": ACTION_OPEN_DECK, "handled": true}

	return {"action": ACTION_NONE, "handled": false}


## 打开抽牌堆查看器。
## settlement 阶段沿用旧参数，让查看器进入只读/结算视图。
func open_deck_viewer(deck_pile: Pile, manager_instance: CardManager, pile_viewer: Variant, is_settlement: bool) -> void:
	if not is_instance_valid(deck_pile):
		return
	if deck_pile._held_cards.size() <= 0:
		return
	if not is_instance_valid(pile_viewer):
		return
	pile_viewer.open_pile_view(deck_pile, manager_instance, is_settlement, is_settlement)


## 打开弃牌堆查看器。
func open_discard_viewer(discard_pile: Pile, manager_instance: CardManager, pile_viewer: Variant) -> void:
	if not is_instance_valid(discard_pile):
		return
	if discard_pile._held_cards.size() <= 0:
		return
	if not is_instance_valid(pile_viewer):
		return
	pile_viewer.open_pile_view(discard_pile, manager_instance)


func _get_pile_count(pile: Pile) -> int:
	if is_instance_valid(pile) and pile._held_cards != null:
		return pile._held_cards.size()
	return 0
