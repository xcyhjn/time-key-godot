extends RefCounted

## RemoveDeckCardRemovalProcessor 只负责删除奖励页从牌组数据中移除一张指定卡。
## 它不同步运行时抽牌堆，不关闭奖励页，也不处理删除动画或 UI 节点。


func remove_card_from_deck(deck_manager, card_id: String) -> void:
	if deck_manager != null and deck_manager.has_method("remove_card_from_deck"):
		deck_manager.remove_card_from_deck(card_id)
		print("✅ 已从牌组中移除卡牌: %s" % card_id)
		return

	print("⚠️ 移除卡牌 %s (需要对接牌组管理系统)" % card_id)
	if GlobalDB and GlobalDB.player_deck.has(card_id):
		GlobalDB.player_deck.erase(card_id)
		print("✅ 已从 GlobalDB.player_deck 移除卡牌: %s" % card_id)
