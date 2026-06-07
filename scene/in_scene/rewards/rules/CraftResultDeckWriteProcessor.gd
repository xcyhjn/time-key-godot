extends RefCounted

## CraftResultDeckWriteProcessor 只负责把合成结果写入传入的牌组数组。
## 它不计算素材牌索引，不同步运行时抽牌堆，也不处理奖励页 UI 或关闭流程。


func apply_result(player_deck: Array[String], remove_indices: Array[int], result_card_id: String) -> void:
	for idx in remove_indices:
		if idx >= 0 and idx < player_deck.size():
			player_deck.remove_at(idx)

	player_deck.append(result_card_id)
