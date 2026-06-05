class_name CraftConnectionLinePresenter
extends RefCounted

## CraftConnectionLinePresenter 只负责合成面板槽位之间的连接线显示。
## 它不判断配方，不创建卡牌，也不修改合成选择状态。


func update_connection_lines(
	connection_lines: Line2D,
	slot1_card: Control,
	slot2_card: Control,
	result_card: Control,
	slot1: Control,
	slot2: Control,
	result_slot: Control
) -> void:
	if not is_instance_valid(connection_lines):
		return

	if (
		not is_instance_valid(slot1_card)
		or not is_instance_valid(slot2_card)
		or not is_instance_valid(result_card)
	):
		connection_lines.points = PackedVector2Array()
		return

	connection_lines.points = PackedVector2Array([
		_get_center(slot1),
		_get_center(result_slot),
		_get_center(slot2),
		_get_center(result_slot),
	])


func _get_center(control: Control) -> Vector2:
	if not is_instance_valid(control):
		return Vector2.ZERO
	return control.position + (control.size * 0.5)
