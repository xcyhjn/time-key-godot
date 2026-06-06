extends RefCounted


## ShopDebugLogger 只负责商店页面初始化和商品生成阶段的调试输出。
## 它不计算价格，不生成商品，也不改变商店流程。


var enabled: bool = true


func log_initial_layout(shop_slots_count: int, shop_columns: int, actual_columns: int) -> void:
	if not enabled:
		return
	print("🛒 商店初始化 - shop_slots_count: %d, shop_columns: %d, 实际列数: %d" % [
		shop_slots_count, shop_columns, actual_columns
	])


func log_exported_prices(base_price: int, price_increment: int, refresh_base_cost: int, upgrade_base_cost: int) -> void:
	if not enabled:
		return
	print("🛒 导出参数检查 - base_price: %d, price_increment: %d, refresh_base_cost: %d, upgrade_base_cost: %d" % [
		base_price, price_increment, refresh_base_cost, upgrade_base_cost
	])


func log_label_refs(has_refresh_label: bool, has_upgrade_label: bool) -> void:
	if not enabled:
		return
	print("🛒 标签引用检查 - label_refresh_cost: %s, label_upgrade_cost: %s" % [
		"有效" if has_refresh_label else "null",
		"有效" if has_upgrade_label else "null"
	])


func log_generation_skipped() -> void:
	if enabled:
		print("🔄 商店生成已在进行中，跳过重复调用")


func log_generation_start(child_count: int) -> void:
	if enabled:
		print("🔄 开始生成商店商品 - 清理前shop_grid子节点数: %d" % child_count)


func log_generation_cleanup(child_count: int) -> void:
	if enabled:
		print("🧹 清理完成 - shop_grid子节点数: %d" % child_count)


func log_physical_cleanup(child_count: int) -> void:
	if enabled:
		print("🧹 已执行物理清理，容器当前子节点数: %d" % child_count)


func log_generation_layout(shop_slots_count: int, shop_columns: int, actual_columns: int) -> void:
	if not enabled:
		return
	print("🔍 商店生成参数 - shop_slots_count: %d, shop_columns: %d, 实际列数: %d" % [
		shop_slots_count, shop_columns, actual_columns
	])


func log_generation_era(current_era: int, global_era: int, local_era_offset: int) -> void:
	if not enabled:
		return
	print("商店生成 - 基础时代: %d (全局时代: %d + 本地偏移: %d)" % [
		current_era, global_era, local_era_offset
	])


func log_slot_generation_start(shop_slots_count: int) -> void:
	if enabled:
		print("📊 开始生成商品位，总数: %d，当前循环索引: 0 到 %d" % [shop_slots_count, shop_slots_count - 1])


func log_slot_generation(slot_index: int, shop_slots_count: int) -> void:
	if enabled:
		print("  🔸 生成商品位 %d/%d" % [slot_index + 1, shop_slots_count])


func log_generation_done(shop_slots_count: int, grid_child_count: int, shop_cards_count: int) -> void:
	if not enabled:
		return
	print("✅ 商店商品生成完成 - 生成数量: %d, 实际shop_grid子节点数: %d, shop_cards记录数: %d" % [
		shop_slots_count, grid_child_count, shop_cards_count
	])
