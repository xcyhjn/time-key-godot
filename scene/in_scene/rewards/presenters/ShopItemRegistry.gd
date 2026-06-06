extends RefCounted

## ShopItemRegistry 只负责注册已构建好的商店商品槽和购买点击信号。
## 它不选择卡牌，不计算价格，也不读取或修改牌组数据。


func register_item(
	shop_grid: GridContainer,
	shop_cards: Array,
	card_price_map: Dictionary,
	shop_card: Control,
	slot_data: Dictionary,
	click_handler: Callable
) -> void:
	shop_grid.add_child(slot_data["container"])
	shop_cards.append(shop_card)
	card_price_map[shop_card] = slot_data
	shop_card.card_clicked.connect(click_handler)
