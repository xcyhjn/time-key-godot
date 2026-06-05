class_name TimelineBlockPlacementAnimator
extends RefCounted

## TimelineBlockPlacementAnimator 只负责单个时间轴行动方格的放置入场动画。
## 它不创建行动容器，不修改 TimelineManager 数据，也不处理敌方意图入场动画。


func animate_block_placement(block: Panel, target_color: Color, tween_factory: Callable) -> void:
	if not is_instance_valid(block):
		return

	var original_style = block.get_theme_stylebox("panel") as StyleBoxFlat
	if not original_style:
		return

	var style = original_style.duplicate() as StyleBoxFlat
	block.add_theme_stylebox_override("panel", style)

	var transparent_color = Color(target_color.r, target_color.g, target_color.b, 0.0)
	style.bg_color = transparent_color
	style.border_color = Color.BLACK

	var tw = tween_factory.call().set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tw.tween_property(style, "bg_color", target_color, 0.5)

	block.scale = Vector2(0.8, 0.8)
	tw.parallel().tween_property(block, "scale", Vector2.ONE, 0.3).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
