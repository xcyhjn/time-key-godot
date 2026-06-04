class_name SettlementRewardExitController
extends RefCounted


## SettlementRewardExitController 只负责读取奖励页退出状态并关闭奖励页实例。
## 它不写 HexMap 奖励状态、不恢复主 UI，也不改变战斗阶段。


func close_reward_scene(scene_instance: Node) -> Dictionary:
	var result: Dictionary = {
		"should_consume_settlement_reward": false,
		"reward_context": {},
	}

	if is_instance_valid(scene_instance) and scene_instance.has_meta("settlement_reward_context"):
		var context_variant: Variant = scene_instance.get_meta("settlement_reward_context")
		if typeof(context_variant) == TYPE_DICTIONARY:
			result["reward_context"] = context_variant

		var committed: bool = bool(scene_instance.get_meta("settlement_reward_committed", false))
		var consume_on_exit: bool = bool(scene_instance.get_meta("settlement_reward_consume_on_exit", false))
		result["should_consume_settlement_reward"] = committed or consume_on_exit

	if is_instance_valid(scene_instance):
		scene_instance.hide()
		scene_instance.queue_free()

	return result
