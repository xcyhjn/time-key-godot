extends RefCounted


## RewardDeckSyncBridge 只负责奖励页把新增卡牌写入 deck_manager 并同步局内抽牌堆。
## 它不关闭奖励页，不修改商店列表，也不决定移除或合成如何改写牌组数据。


func add_card_and_sync(owner: Node, deck_manager, card_id: String) -> void:
	if deck_manager != null and deck_manager.has_method("add_card_to_deck"):
		deck_manager.add_card_to_deck(card_id)

	sync_runtime_deck(owner, deck_manager)
	print("✅ 已成功将 %s 加入全局牌组并同步抽牌堆！" % card_id)


func sync_runtime_deck(owner: Node, deck_manager) -> void:
	var main = owner.get_tree().get_first_node_in_group("MainBoard")
	if (
		deck_manager != null
		and deck_manager.has_method("sync_runtime_deck_from_global")
		and main
		and main.deck_pile
	):
		deck_manager.sync_runtime_deck_from_global(main.deck_pile)
		if main.has_method("update_counts_and_ui"):
			main.update_counts_and_ui()
