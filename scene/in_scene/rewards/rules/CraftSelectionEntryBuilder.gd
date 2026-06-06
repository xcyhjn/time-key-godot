extends RefCounted

## CraftSelectionEntryBuilder 只负责生成合成选择列表条目和状态建议。
## 它不创建卡牌节点，不写主脚本状态，也不处理按钮或选择面板 UI。

const ENTRIES_KEY := "entries"
const PENDING_SELECTED_ENTRY_KEY := "pending_selected_entry"
const CAN_CLOSE_WITHOUT_CHOICE_KEY := "can_close_selection_without_choice"


func build_selection_entries(
	slot_index: int,
	deck_ids: Array[String],
	slot_entries: Dictionary,
	slot_1: int,
	slot_2: int,
	get_recipe_result: Callable
) -> Dictionary:
	var other_slot_index = slot_2 if slot_index == slot_1 else slot_1
	var current_entry = slot_entries[slot_index]
	var other_entry = slot_entries[other_slot_index]
	var base_entries: Array = []

	for i in range(deck_ids.size()):
		if other_entry != null and i == other_entry["deck_index"]:
			continue

		base_entries.append({
			"deck_index": i,
			"card_id": deck_ids[i],
			"disabled": false,
			"dimmed": false,
			"selected": false,
		})

	if current_entry != null:
		return _build_current_slot_entries(base_entries, current_entry)

	if other_entry != null:
		return _build_compatible_entries(base_entries, other_entry, get_recipe_result)

	return _make_result(base_entries, {}, false)


func _build_current_slot_entries(base_entries: Array, current_entry: Dictionary) -> Dictionary:
	var ordered_entries: Array = []
	var pending_selected_entry: Dictionary = {}

	for entry in base_entries:
		if entry["deck_index"] == current_entry["deck_index"]:
			entry["selected"] = true
			pending_selected_entry = _copy_entry(entry)
			ordered_entries.append(entry)
			break

	for entry in base_entries:
		if current_entry == null or entry["deck_index"] != current_entry["deck_index"]:
			ordered_entries.append(entry)

	return _make_result(ordered_entries, pending_selected_entry, false)


func _build_compatible_entries(base_entries: Array, other_entry: Dictionary, get_recipe_result: Callable) -> Dictionary:
	var compatible_entries: Array = []
	var incompatible_entries: Array = []

	for entry in base_entries:
		if str(get_recipe_result.call(other_entry["card_id"], entry["card_id"])) != "":
			compatible_entries.append(entry)
		else:
			entry["disabled"] = true
			entry["dimmed"] = true
			incompatible_entries.append(entry)

	return _make_result(
		compatible_entries + incompatible_entries,
		{},
		compatible_entries.is_empty()
	)


func _make_result(entries: Array, pending_selected_entry: Dictionary, can_close_selection_without_choice: bool) -> Dictionary:
	return {
		ENTRIES_KEY: entries,
		PENDING_SELECTED_ENTRY_KEY: pending_selected_entry,
		CAN_CLOSE_WITHOUT_CHOICE_KEY: can_close_selection_without_choice,
	}


func _copy_entry(entry: Dictionary) -> Dictionary:
	return {
		"deck_index": entry["deck_index"],
		"card_id": entry["card_id"],
	}
