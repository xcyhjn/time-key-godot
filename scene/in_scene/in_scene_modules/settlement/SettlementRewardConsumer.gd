class_name SettlementRewardConsumer
extends RefCounted


## SettlementRewardConsumer 只负责把“奖励建筑已被使用”的事实写回 HexMap。
## 它不打开奖励页、不关闭奖励页，也不恢复 UI。


func consume(reward_context: Dictionary, hex_map: Variant) -> bool:
	if reward_context.is_empty():
		return false
	if not is_instance_valid(hex_map):
		return false

	var reward_stack: Variant = reward_context.get("stack")
	if not is_instance_valid(reward_stack):
		return false

	hex_map.mark_settlement_reward_used(reward_stack)
	return true
