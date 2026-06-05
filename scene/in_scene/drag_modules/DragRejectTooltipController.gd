class_name DragRejectTooltipController
extends RefCounted


## DragRejectTooltipController 只负责拖拽拒绝提示的文本、位置和显隐。
## 它不判断放置是否合法，不播放卡牌抖动动画，也不修改拖拽状态。


const TOOLTIP_OFFSET: Vector2 = Vector2(20.0, -30.0)
const SCREEN_PADDING: float = 10.0
const REJECT_COLOR: String = "#ff5555"


func show_tooltip(tooltip: RichTextLabel, message: String) -> void:
	if not is_instance_valid(tooltip):
		return
	tooltip.text = "[color=%s]%s[/color]" % [REJECT_COLOR, message]
	tooltip.size = Vector2.ZERO
	tooltip.show()


func update_position(tooltip: RichTextLabel, mouse_pos: Vector2, screen_size: Vector2) -> void:
	if not is_instance_valid(tooltip):
		return

	var tooltip_size: Vector2 = tooltip.size
	var target_pos: Vector2 = mouse_pos + TOOLTIP_OFFSET

	if target_pos.x + tooltip_size.x > screen_size.x:
		target_pos.x = mouse_pos.x - tooltip_size.x - abs(TOOLTIP_OFFSET.x)

	if target_pos.y + tooltip_size.y > screen_size.y:
		target_pos.y = screen_size.y - tooltip_size.y - SCREEN_PADDING

	tooltip.global_position = target_pos


func hide_tooltip(tooltip: RichTextLabel) -> void:
	if is_instance_valid(tooltip):
		tooltip.hide()
