class_name SettlementRewardExitFlowController
extends RefCounted


## SettlementRewardExitFlowController 编排奖励页退出后的主场景收尾。
## 它只关闭奖励页、按退出结果消费奖励建筑并恢复局内 UI，不打开奖励页、不改变战斗阶段。


func handle_exit(config: Dictionary) -> Dictionary:
	var exit_result: Dictionary = _close_reward_scene(
		config.get("exit_controller"),
		config.get("scene_instance") as Node
	)
	var consumed: bool = _consume_reward_if_needed(
		exit_result,
		config.get("reward_consumer"),
		config.get("hex_map")
	)
	_call_if_valid(config.get("restore_ui_after_external_scene"))

	return {
		"closed": true,
		"consumed": consumed,
		"exit_result": exit_result,
	}


func _close_reward_scene(exit_controller: Variant, scene_instance: Node) -> Dictionary:
	if not is_instance_valid(exit_controller) or not exit_controller.has_method("close_reward_scene"):
		return _empty_exit_result()

	var result_variant: Variant = exit_controller.close_reward_scene(scene_instance)
	if typeof(result_variant) != TYPE_DICTIONARY:
		return _empty_exit_result()

	return result_variant


func _consume_reward_if_needed(exit_result: Dictionary, reward_consumer: Variant, hex_map: Variant) -> bool:
	if not bool(exit_result.get("should_consume_settlement_reward", false)):
		return false
	if not is_instance_valid(reward_consumer) or not reward_consumer.has_method("consume"):
		return false

	var reward_context_variant: Variant = exit_result.get("reward_context", {})
	if typeof(reward_context_variant) != TYPE_DICTIONARY:
		return false

	var reward_context: Dictionary = reward_context_variant
	return bool(reward_consumer.consume(reward_context, hex_map))


func _empty_exit_result() -> Dictionary:
	return {
		"should_consume_settlement_reward": false,
		"reward_context": {},
	}


func _call_if_valid(callback: Variant) -> void:
	if callback is Callable:
		var callable: Callable = callback
		if callable.is_valid():
			callable.call()
