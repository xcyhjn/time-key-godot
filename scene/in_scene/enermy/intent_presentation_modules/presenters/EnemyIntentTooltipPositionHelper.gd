extends RefCounted


## EnemyIntentTooltipPositionHelper 只负责计算敌人意图主 tooltip 的屏幕位置。
## 它不读取节点树，不写入 MainBoard，不创建或销毁 tooltip，也不处理状态关键词副 tooltip。


func calculate_position(anchor_position: Vector2, fallback_position: Vector2, has_anchor: bool, offset: Vector2, panel_size: Vector2, screen_size: Vector2, margin: Vector2) -> Vector2:
	var tooltip_pos := anchor_position + offset if has_anchor else fallback_position
	tooltip_pos.x = clampf(tooltip_pos.x, margin.x, screen_size.x - panel_size.x - margin.x)
	tooltip_pos.y = clampf(tooltip_pos.y, margin.y, screen_size.y - panel_size.y - margin.y)
	return tooltip_pos
