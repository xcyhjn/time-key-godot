extends RefCounted

## RewardDeckCardIdProvider 只负责读取奖励页当前可用的牌组卡牌 ID 列表。
## 它不修改牌组，不创建卡牌，也不同步运行时抽牌堆。


func get_current_deck_card_ids(deck_manager) -> Array[String]:
	if deck_manager != null and deck_manager.has_method("get_deck_card_ids"):
		return deck_manager.get_deck_card_ids()

	if GlobalDB:
		return GlobalDB.player_deck.duplicate()

	return []
