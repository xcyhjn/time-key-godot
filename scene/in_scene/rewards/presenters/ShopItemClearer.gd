extends RefCounted


## ShopItemClearer 只负责清空商店商品 UI 和商品记录。
## 它不生成商品，不计算价格，也不处理购买流程。


func clear_items(shop_grid: GridContainer, shop_cards: Array, card_price_map: Dictionary) -> void:
	if is_instance_valid(shop_grid):
		for child in shop_grid.get_children():
			if is_instance_valid(child):
				shop_grid.remove_child(child)
				child.queue_free()

	shop_cards.clear()
	card_price_map.clear()
