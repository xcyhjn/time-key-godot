extends RefCounted


## RewardRealCardCleaner 只负责从奖励页临时幽灵牌堆移除真实卡牌并释放节点。
## 它不读取卡牌数据，不修改 DraftCard，也不管理奖励页状态。


func cleanup_real_card(temp_pile: Node, real_card: Node) -> void:
	if is_instance_valid(temp_pile) and real_card != null:
		temp_pile.remove_card(real_card)

	if is_instance_valid(real_card):
		real_card.queue_free()
