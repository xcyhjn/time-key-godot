extends RefCounted


## RewardRealCardSpawner 只负责把 card_factory 生成的真实卡牌临时放入奖励页幽灵牌堆。
## 它不复制卡牌数据，不修改 DraftCard，也不清理临时真实卡牌。


func spawn_into_temp_pile(card_id: String, card_factory, temp_pile: Node, owner: Node) -> Node:
	card_factory.create_card(card_id, temp_pile)

	if owner != null and owner.is_inside_tree():
		await owner.get_tree().process_frame

	if not is_instance_valid(temp_pile):
		return null

	return temp_pile._held_cards[-1] if temp_pile._held_cards.size() > 0 else null
