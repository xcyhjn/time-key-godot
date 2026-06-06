extends RefCounted


## ShopCardPoolBridge 只负责商店按时代读取 CardDataPool 候选卡牌。
## 它不选择时代权重，不创建卡牌，也不处理商店商品 UI。


func get_cards_by_era(card_data_pool_script: GDScript, era: int) -> Array[String]:
	var card_data_pool = card_data_pool_script.get_instance()
	if card_data_pool:
		var cards: Array[String] = card_data_pool.get_cards_by_era(era)
		print("🃏 ShopManager: 从 CardDataPool 获取时代 %d 的卡牌，共 %d 张" % [era, cards.size()])
		return cards

	push_error("❌ ShopManager: 无法获取 CardDataPool 单例")
	var era_pools := {
		1: ["1"],
		2: ["2"],
		3: ["3"],
		4: [""],
	}
	var fallback_cards: Array[String] = []
	if era_pools.has(era):
		fallback_cards.assign(era_pools[era])
	return fallback_cards
