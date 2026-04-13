extends Control

@onready var title_label = $Title
@onready var data_flow = $DataFlow

## 播放大标题动画：现在它会返回并允许 await 直到动画结束
func play_main_title(text: String, font: Font, size: int, color: Color, out_s: int, slow_s: float):
	title_label.text = text
	title_label.add_theme_font_size_override("font_size", size)
	title_label.add_theme_color_override("font_color", color)
	title_label.add_theme_constant_override("outline_size", out_s + 6)
	if font: title_label.add_theme_font_override("font", font)
	
	title_label.pivot_offset = title_label.size / 2
	title_label.scale.x = 0  
	title_label.modulate.a = 0
	
	var tw = create_tween().set_parallel(true).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	tw.tween_property(title_label, "modulate:a", 1.0, 0.4 * slow_s)
	tw.tween_property(title_label, "scale:x", 1.0, 0.6 * slow_s).set_trans(Tween.TRANS_QUART).set_ease(Tween.EASE_OUT)
	
	# 核心：必须等待 Tween 完成
	await tw.finished
	
	# 标题显示完成后稍微停顿，增强视觉冲击力
	await get_tree().create_timer(0.4 * slow_s).timeout

## 结算数据流出逻辑
func pour_stats(stats: Dictionary, font: Font, size: int, color: Color, out_s: int, s: float):
	# 先清空旧的数据（防止重复测试堆积）
	for child in data_flow.get_children():
		child.queue_free()
	
	for key in stats.keys():
		var label = Label.new()
		label.text = str(key) + " : " + str(stats[key])
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.add_theme_font_size_override("font_size", size)
		label.add_theme_color_override("font_color", color)
		label.add_theme_constant_override("outline_size", out_s)
		if font: label.add_theme_font_override("font", font)
		
		label.modulate.a = 0
		data_flow.add_child(label)
		
		# 等待一帧以计算正确位置
		await get_tree().process_frame 
		
		var target_y = label.position.y
		label.position.y -= 40 
		
		var tw = create_tween().set_parallel(true).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
		tw.tween_property(label, "modulate:a", 1.0, 0.3 * s)
		tw.tween_property(label, "position:y", target_y, 0.6 * s).set_trans(Tween.TRANS_BOUNCE)
		
		# 每个条目之间的小间隔
		await get_tree().create_timer(0.15 * s).timeout
