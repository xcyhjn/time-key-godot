extends RefCounted


## ShopPurchaseValidator 只负责商店购买前的价格消费校验与结果日志。
## 它不移动卡牌，不修改商品列表，也不执行飞入牌库动画。


func consume_price(card_id: String, price: int, consume_timecoins: Callable) -> bool:
	var consumed = consume_timecoins.call(price)
	if not consumed:
		print("购买失败: 时间币不足! 需要: %d, 当前余额不足" % price)
		return false

	print("成功购买卡牌 %s, 价格: %d 时间币" % [card_id, price])
	return true
