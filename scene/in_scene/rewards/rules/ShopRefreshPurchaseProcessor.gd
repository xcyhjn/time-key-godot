extends RefCounted


## ShopRefreshPurchaseProcessor 只负责商店刷新按钮的费用结算。
## 它不生成商品，不更新价格标签，也不处理时代升级流程。


func process_refresh(
	refresh_base_cost: int,
	refresh_count: int,
	price_increment: int,
	consume_timecoins: Callable
) -> Dictionary:
	var refresh_cost := refresh_base_cost + (refresh_count * price_increment)

	if not consume_timecoins.call(refresh_cost):
		print("刷新失败: 时间币不足! 需要: %d" % refresh_cost)
		return {
			"success": false,
			"refresh_count": refresh_count,
		}

	var next_refresh_count := refresh_count + 1
	print("商店刷新成功! 消耗: %d 时间币, 刷新次数: %d" % [refresh_cost, next_refresh_count])
	return {
		"success": true,
		"refresh_count": next_refresh_count,
	}
