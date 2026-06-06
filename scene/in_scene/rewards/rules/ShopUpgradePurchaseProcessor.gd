extends RefCounted


## ShopUpgradePurchaseProcessor 只负责商店升级按钮的费用结算和升级状态结果。
## 它不生成商品，不更新价格标签，也不读取或修改全局时代。


func process_upgrade(
	upgrade_base_cost: int,
	upgrade_count: int,
	price_increment: int,
	local_era_offset: int,
	consume_timecoins: Callable
) -> Dictionary:
	var upgrade_cost := upgrade_base_cost + (upgrade_count * price_increment)

	if not consume_timecoins.call(upgrade_cost):
		print("升级失败: 时间币不足! 需要: %d" % upgrade_cost)
		return {
			"success": false,
			"upgrade_count": upgrade_count,
			"local_era_offset": local_era_offset,
		}

	var next_upgrade_count := upgrade_count + 1
	var next_local_era_offset := local_era_offset + 1
	print("时代升级成功! 消耗: %d 时间币, 升级次数: %d, 本地时代偏移: %d" % [
		upgrade_cost, next_upgrade_count, next_local_era_offset
	])
	return {
		"success": true,
		"upgrade_count": next_upgrade_count,
		"local_era_offset": next_local_era_offset,
	}
