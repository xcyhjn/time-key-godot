extends RefCounted

## CraftSelectionTitlePresenter 只负责合成选择面板标题文案。
## 它不生成选择卡，不排序牌组条目，也不修改合成状态。


func build_selection_title(
	slot_index: int,
	slot_entries: Dictionary,
	slot_1: int,
	slot_2: int,
	select_slot_1_text: String,
	select_slot_2_text: String
) -> String:
	var current_entry = slot_entries[slot_index]
	var other_slot_index = slot_2 if slot_index == slot_1 else slot_1
	var other_entry = slot_entries[other_slot_index]

	if current_entry != null:
		return "重新选择槽位 %d 卡牌（当前卡已置顶并高亮）" % slot_index

	if other_entry != null:
		return "请选择与槽位 %d 可融合的卡牌（可融合卡在前，不可融合卡已变暗）" % other_slot_index

	return select_slot_1_text if slot_index == slot_1 else select_slot_2_text
