extends RefCounted

## ShopGenerationDependencyGuard 只负责检查商店生成前的必要依赖是否可用。
## 它不生成商品，不修改生成锁，也不访问商店 UI。

const CAN_GENERATE_KEY := "can_generate"
const WARNING_KEY := "warning"


func check_dependencies(deck_manager) -> Dictionary:
	var result: Dictionary = {
		CAN_GENERATE_KEY: true,
		WARNING_KEY: "",
	}

	if not deck_manager or not deck_manager.card_factory:
		result[CAN_GENERATE_KEY] = false
		result[WARNING_KEY] = "ShopManager: deck_manager 或 card_factory 未设置! 商店将显示为空。请确保已调用 set_deck_manager() 或 CardManager 已在场景中。"

	return result
