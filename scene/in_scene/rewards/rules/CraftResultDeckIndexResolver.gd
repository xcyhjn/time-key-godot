extends RefCounted

## CraftResultDeckIndexResolver 只负责计算合成结果写回前需要移除的牌组索引。
## 它不修改 GlobalDB，不添加结果卡，也不同步运行时抽牌堆。


func get_remove_indices(slot_entries: Dictionary, slot_1: int, slot_2: int) -> Array[int]:
	var remove_indices: Array[int] = [
		int(slot_entries[slot_1]["deck_index"]),
		int(slot_entries[slot_2]["deck_index"]),
	]
	remove_indices.sort()
	remove_indices.reverse()
	return remove_indices
