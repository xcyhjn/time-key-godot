class_name ShopPricingPresenter
extends RefCounted

## ShopPricingPresenter 只负责商店价格计算和价格标签显示。
## 它不消费时间币，不生成商品，也不处理购买或刷新升级流程。


func calculate_card_price(slot_index: int, base_price: int) -> int:
	return base_price + (slot_index * 5)


func calculate_refresh_cost(refresh_base_cost: int, refresh_count: int, price_increment: int) -> int:
	return refresh_base_cost + (refresh_count * price_increment)


func calculate_upgrade_cost(upgrade_base_cost: int, upgrade_count: int, price_increment: int) -> int:
	return upgrade_base_cost + (upgrade_count * price_increment)


func update_price_labels(
	label_refresh_cost: Label,
	label_upgrade_cost: Label,
	refresh_cost: int,
	upgrade_cost: int
) -> void:
	if label_refresh_cost:
		label_refresh_cost.text = "%d时间币" % refresh_cost
		print("✅ label_refresh_cost 已更新 (位于 UpgradeGroup)")
	else:
		print("❌ label_refresh_cost 为 null!")

	if label_upgrade_cost:
		label_upgrade_cost.text = "%d时间币" % upgrade_cost
		print("✅ label_upgrade_cost 已更新 (位于 RefreshGroup)")
	else:
		print("❌ label_upgrade_cost 为 null!")
