class_name CraftSlotPreviewLayoutPresenter
extends RefCounted

## CraftSlotPreviewLayoutPresenter 只负责合成槽位预览锚点的边距和尺寸兜底。
## 它不创建预览卡，不读取合成配方，也不修改槽位选择状态。


func apply_preview_padding(slot1_anchor: Control, slot2_anchor: Control, result_anchor: Control, slot_preview_padding: Vector2) -> void:
	apply_padding_to_anchor(slot1_anchor, slot_preview_padding)
	apply_padding_to_anchor(slot2_anchor, slot_preview_padding)
	apply_padding_to_anchor(result_anchor, slot_preview_padding)


func apply_padding_to_anchor(anchor: Control, slot_preview_padding: Vector2) -> void:
	anchor.offset_left = slot_preview_padding.x
	anchor.offset_top = slot_preview_padding.y
	anchor.offset_right = -slot_preview_padding.x
	anchor.offset_bottom = -slot_preview_padding.y


func get_anchor_preview_size(anchor: Control, fallback_size: Vector2) -> Vector2:
	var size = anchor.size
	if size == Vector2.ZERO:
		return fallback_size
	return size
