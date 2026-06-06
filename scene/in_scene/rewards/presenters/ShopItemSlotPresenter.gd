extends RefCounted


## ShopItemSlotPresenter 只负责把已准备好的商店 DraftCard 包装成商品位 UI。
## 它不选择卡牌，不读取卡牌数据，也不处理购买或价格消费。


func build_slot(shop_card: Control, price: int) -> Dictionary:
	var slot_container := VBoxContainer.new()
	slot_container.alignment = BoxContainer.ALIGNMENT_CENTER
	slot_container.add_child(shop_card)

	var price_label := Label.new()
	price_label.text = "%d 时间币" % price
	price_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	price_label.add_theme_font_size_override("font_size", 14)
	price_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.2, 1.0))
	slot_container.add_child(price_label)

	return {
		"container": slot_container,
		"label": price_label,
		"price": price,
	}
