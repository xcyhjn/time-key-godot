extends RefCounted


## ShopPurchaseCardDetachPresenter 只负责购买成功后把卡牌从商品槽脱离出来。
## 它不消费时间币，不播放飞行动画，也不写入或同步牌库。


func detach_for_flight(shop_layer: Node, clicked_card: Control, price_label: Node, slot_container: Node) -> void:
	price_label.queue_free()

	var global_pos := clicked_card.global_position
	slot_container.remove_child(clicked_card)
	shop_layer.add_child(clicked_card)
	clicked_card.global_position = global_pos

	if is_instance_valid(slot_container) and slot_container.get_parent():
		slot_container.get_parent().remove_child(slot_container)
	slot_container.queue_free()
