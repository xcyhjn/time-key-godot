extends RefCounted


## EnemyIntentStatusKeywordTooltipPresenter 只负责敌人意图状态关键词副 tooltip 的创建、定位和销毁。
## 它不读取敌人状态，不写入主 tooltip 文本，不判断 hover 阶段，也不驱动地图或时间轴表现。


const KEYWORD_TOOLTIP_PANEL_SCENE := preload("res://scene/shared/tooltip/keyword_tooltip_panel.tscn")

var status_keyword_tooltip_hbox: HBoxContainer = null


func rebuild(keywords: Array[String], host: Node, main_panel: Control, screen_size: Vector2, gap: float, width: float, margin: Vector2) -> void:
	hide()

	if keywords.is_empty():
		return
	if not is_instance_valid(host):
		return

	status_keyword_tooltip_hbox = HBoxContainer.new()
	status_keyword_tooltip_hbox.name = "EnemyStatusKeywordTooltips"
	status_keyword_tooltip_hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	status_keyword_tooltip_hbox.add_theme_constant_override("separation", int(gap))
	host.add_child(status_keyword_tooltip_hbox)

	for keyword_name in keywords:
		var panel := create_panel(keyword_name, width)
		if is_instance_valid(panel):
			status_keyword_tooltip_hbox.add_child(panel)

	update_position(main_panel, screen_size, gap, margin)


func create_panel(keyword_name: String, width: float) -> PanelContainer:
	if not GlobalDB.KEYWORDS.has(keyword_name):
		return null

	var keyword_data: Dictionary = GlobalDB.KEYWORDS[keyword_name]
	var panel := KEYWORD_TOOLTIP_PANEL_SCENE.instantiate() as PanelContainer
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.12, 0.12, 0.95)
	style.border_width_left = 2
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.border_color = Color(0.8, 0.6, 0.2, 1.0)
	style.set_corner_radius_all(6)
	panel.add_theme_stylebox_override("panel", style)

	var margin_container := panel.get_node_or_null("Margin") as MarginContainer
	if is_instance_valid(margin_container):
		margin_container.add_theme_constant_override("margin_left", 12)
		margin_container.add_theme_constant_override("margin_right", 12)
		margin_container.add_theme_constant_override("margin_top", 10)
		margin_container.add_theme_constant_override("margin_bottom", 10)

	var title_label := panel.get_node_or_null("Margin/ContentVBox/TitleLabel") as Label
	if is_instance_valid(title_label):
		title_label.text = keyword_name
		title_label.add_theme_color_override("font_color", Color(str(keyword_data.get("color", "#ffffff"))))

	var desc_label := panel.get_node_or_null("Margin/ContentVBox/DescriptionLabel") as RichTextLabel
	if is_instance_valid(desc_label):
		desc_label.custom_minimum_size = Vector2(width, 0.0)
		desc_label.clear()
		desc_label.append_text(str(keyword_data.get("desc", "")))

	return panel


func update_position(main_panel: Control, screen_size: Vector2, gap: float, margin: Vector2) -> void:
	if not is_instance_valid(status_keyword_tooltip_hbox):
		return
	if not is_instance_valid(main_panel):
		return

	var main_pos: Vector2 = main_panel.global_position
	var main_size: Vector2 = main_panel.size if main_panel.size != Vector2.ZERO else main_panel.get_combined_minimum_size()
	var keywords_size: Vector2 = status_keyword_tooltip_hbox.size if status_keyword_tooltip_hbox.size != Vector2.ZERO else status_keyword_tooltip_hbox.get_combined_minimum_size()

	var target_x := main_pos.x + main_size.x + gap
	if target_x + keywords_size.x > screen_size.x - margin.x:
		target_x = main_pos.x - keywords_size.x - gap

	var target_y := clampf(
		main_pos.y,
		margin.y,
		screen_size.y - keywords_size.y - margin.y
	)

	status_keyword_tooltip_hbox.global_position = Vector2(target_x, target_y)


func hide() -> void:
	if is_instance_valid(status_keyword_tooltip_hbox):
		status_keyword_tooltip_hbox.queue_free()
	status_keyword_tooltip_hbox = null
