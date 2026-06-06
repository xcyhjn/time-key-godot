extends RefCounted


## ShopPurchasedItemRecordRemover 只负责购买飞行动画完成后移除商店记录。
## 它不写入牌组，不同步抽牌堆，也不释放或移动卡牌节点。


func remove_record(card: Control, shop_cards: Array, card_price_map: Dictionary) -> void:
	var idx := shop_cards.find(card)
	if idx == -1:
		return

	shop_cards.remove_at(idx)
	card_price_map.erase(card)
