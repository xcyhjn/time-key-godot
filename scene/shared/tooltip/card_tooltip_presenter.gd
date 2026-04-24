## card_tooltip_presenter.gd
## -------------------------------------------------------------------
## 这个类是“卡牌 Hover Tooltip”的通用表现协调器。
##
## 它解决的问题：
## 1. 不同场景里重复创建 CanvasLayer / PanelContainer / RichTextLabel。
## 2. 不同场景里重复写 show/hide、异步测尺寸、左右翻转、边界裁切逻辑。
## 3. 让业务脚本只保留“什么时候显示 Tooltip”的判断，而不是重复堆 UI 构建代码。
##
## 它不解决的问题：
## 1. 不参与卡牌业务逻辑，不决定某张卡能不能显示 Tooltip。
## 2. 不决定卡牌描述文本如何解析，仍然优先调用 card.get_parsed_description()。
## 3. 不接管 cursor_tooltip / 敌人意图 tooltip，那是另一套锚点体系。
##
## 使用方式：
## - 在场景脚本里持有一个 presenter 实例。
## - scene.show_tooltip(card) -> presenter.show_card_tooltip(card, options)
## - scene.hide_tooltip(card) -> presenter.hide_tooltip()
##
## 这样可以最大限度复用现有 DraftCard / CustomCard 对 show_tooltip 的调用协议，
## 而不用把整条交互链重写一遍。
## -------------------------------------------------------------------
class_name CardTooltipPresenter
extends RefCounted

const TOOLTIP_PANEL_SCENE := preload("res://scene/shared/tooltip/tooltip_panel.tscn")
const KEYWORD_TOOLTIP_PANEL_SCENE := preload("res://scene/shared/tooltip/keyword_tooltip_panel.tscn")

## Tooltip 所属宿主场景。
## presenter 会把 CanvasLayer 动态挂到这个 host 下面。
var host: Node = null
## 当前使用的样式与布局配置。
var config: TooltipConfig = null
## 是否默认显示关键词列。
var default_show_keywords: bool = false

## 运行时创建出来的 UI 节点。
var tooltip_canvas: CanvasLayer = null
var effect_tooltip_panel: PanelContainer = null
var effect_label: RichTextLabel = null
var keywords_tooltip_hbox: HBoxContainer = null

## 当前正在等待异步测尺寸的卡牌。
## 用它来阻断“鼠标已经移走，但上一帧的 tooltip 还在继续布局”的幽灵显示。
var current_hovered_card: Control = null


func _init(host_node: Node, tooltip_config: TooltipConfig, show_keywords_by_default: bool = false) -> void:
	host = host_node
	config = tooltip_config if tooltip_config != null else TooltipConfig.new()
	default_show_keywords = show_keywords_by_default


## 对外统一入口：显示指定卡牌的 Tooltip。
##
## options 支持：
## - show_keywords: bool
##   是否显示右侧关键词解释列。
## - fallback_text: String
##   当卡牌没有任何描述时，用什么文本兜底。
func show_card_tooltip(card: Control, options: Dictionary = {}) -> void:
	if not is_instance_valid(host) or not is_instance_valid(card):
		return

	ensure_ui_created()
	if not is_instance_valid(effect_tooltip_panel) or not is_instance_valid(effect_label):
		return

	current_hovered_card = card

	var show_keywords: bool = options.get("show_keywords", default_show_keywords)
	var fallback_text := str(options.get("fallback_text", ""))
	var description_text := _extract_description_text(card)
	if description_text == "" and fallback_text != "":
		description_text = fallback_text

	effect_label.clear()
	if description_text != "":
		effect_label.append_text(description_text)

	var keywords := _extract_keywords(card) if show_keywords else []
	_rebuild_keywords_panel(keywords)

	effect_tooltip_panel.size = Vector2.ZERO
	if is_instance_valid(keywords_tooltip_hbox):
		keywords_tooltip_hbox.size = Vector2.ZERO

	effect_tooltip_panel.modulate = Color(1, 1, 1, 0)
	effect_tooltip_panel.show()

	var has_keywords := keywords.size() > 0
	if has_keywords and is_instance_valid(keywords_tooltip_hbox):
		keywords_tooltip_hbox.modulate = Color(1, 1, 1, 0)
		keywords_tooltip_hbox.show()
	elif is_instance_valid(keywords_tooltip_hbox):
		keywords_tooltip_hbox.hide()

	await host.get_tree().process_frame
	if current_hovered_card != card:
		return

	_position_tooltip(card, has_keywords)


## 统一隐藏入口。
## 业务层仍然可以保留原有 hide_tooltip(card) 接口，只要内部转发到这里即可。
func hide_tooltip() -> void:
	current_hovered_card = null
	if is_instance_valid(effect_tooltip_panel):
		effect_tooltip_panel.hide()
	if is_instance_valid(keywords_tooltip_hbox):
		keywords_tooltip_hbox.hide()


## 懒加载创建 Tooltip UI。
## 只有第一次真的需要显示 Tooltip 时，才会把 CanvasLayer 和 Panel 节点建出来。
func ensure_ui_created() -> void:
	if is_instance_valid(effect_tooltip_panel) and is_instance_valid(effect_label) and is_instance_valid(keywords_tooltip_hbox):
		return
	if not is_instance_valid(host):
		return

	tooltip_canvas = CanvasLayer.new()
	tooltip_canvas.name = "CardTooltipCanvas"
	tooltip_canvas.layer = 2000
	host.add_child(tooltip_canvas)

	effect_tooltip_panel = TOOLTIP_PANEL_SCENE.instantiate() as PanelContainer
	effect_tooltip_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	effect_tooltip_panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	effect_tooltip_panel.z_index = 1000
	effect_tooltip_panel.hide()
	effect_tooltip_panel.add_theme_stylebox_override("panel", config.create_panel_stylebox())
	tooltip_canvas.add_child(effect_tooltip_panel)

	var effect_margin := effect_tooltip_panel.get_node_or_null("Margin") as MarginContainer
	effect_label = effect_tooltip_panel.get_node_or_null("Margin/ContentLabel") as RichTextLabel
	config.apply_effect_margin(effect_margin)
	config.apply_effect_label_style(effect_label)

	keywords_tooltip_hbox = HBoxContainer.new()
	keywords_tooltip_hbox.name = "KeywordsTooltipHBox"
	keywords_tooltip_hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	keywords_tooltip_hbox.z_index = 1000
	keywords_tooltip_hbox.add_theme_constant_override("separation", config.tooltip_gap_x)
	keywords_tooltip_hbox.hide()
	tooltip_canvas.add_child(keywords_tooltip_hbox)


## 对外暴露主效果面板，方便旧脚本在少量兼容场景下读取尺寸或可见性。
func get_effect_panel() -> PanelContainer:
	return effect_tooltip_panel


## 对外暴露关键词容器，方便旧脚本在少量兼容场景下读取尺寸或可见性。
func get_keywords_container() -> HBoxContainer:
	return keywords_tooltip_hbox


## 从卡牌对象里提取最终要显示的描述文本。
## 这里会优先调用 get_parsed_description()，因为 CustomCard 会在这个过程中同步刷新 active_keywords。
func _extract_description_text(card: Control) -> String:
	if not is_instance_valid(card):
		return ""

	var description_text: Variant = ""
	if card.has_method("get_parsed_description"):
		description_text = card.get_parsed_description()
	elif _object_has_property(card, &"raw_description"):
		description_text = card.get("raw_description")

	return "" if description_text == null else str(description_text)


## 读取卡牌当前解析出的关键词列表。
func _extract_keywords(card: Control) -> Array:
	if not is_instance_valid(card):
		return []
	if not _object_has_property(card, &"active_keywords"):
		return []

	var keywords_variant: Variant = card.get("active_keywords")
	return keywords_variant if keywords_variant is Array else []


## 重新生成右侧关键词列。
## 这一层只关心排版，不关心关键词数据是怎么来的。
func _rebuild_keywords_panel(keywords: Array) -> void:
	if not is_instance_valid(keywords_tooltip_hbox):
		return

	for child in keywords_tooltip_hbox.get_children():
		keywords_tooltip_hbox.remove_child(child)
		child.queue_free()

	if keywords.is_empty():
		return

	var current_column: VBoxContainer = null
	for i in range(keywords.size()):
		var keyword_name := str(keywords[i])
		if not GlobalDB.KEYWORDS.has(keyword_name):
			continue

		if i % max(1, config.max_keywords_per_column) == 0:
			current_column = VBoxContainer.new()
			current_column.add_theme_constant_override("separation", config.tooltip_spacing)
			keywords_tooltip_hbox.add_child(current_column)

		var keyword_data = GlobalDB.KEYWORDS[keyword_name]
		var desc_text := str(keyword_data.get("desc", ""))
		var title_color := str(keyword_data.get("color", "#ffffff"))
		var panel := _create_keyword_panel(keyword_name, desc_text, title_color)
		current_column.add_child(panel)


## 基于模板场景动态创建一个关键词面板。
func _create_keyword_panel(title_text: String, desc_text: String, title_color: String) -> PanelContainer:
	var panel := KEYWORD_TOOLTIP_PANEL_SCENE.instantiate() as PanelContainer
	panel.add_theme_stylebox_override("panel", config.create_panel_stylebox())

	var margin := panel.get_node_or_null("Margin") as MarginContainer
	var title_label := panel.get_node_or_null("Margin/ContentVBox/TitleLabel") as Label
	var desc_label := panel.get_node_or_null("Margin/ContentVBox/DescriptionLabel") as RichTextLabel

	config.apply_keyword_margin(margin)
	config.apply_keyword_title_style(title_label, title_color)
	config.apply_keyword_desc_style(desc_label)

	if is_instance_valid(title_label):
		title_label.text = title_text
	if is_instance_valid(desc_label):
		desc_label.clear()
		if desc_text != "":
			desc_label.append_text(desc_text)

	return panel


## 统一处理主效果框与关键词列的最终摆放。
## 这里完整复用了你现有主战斗脚本里的双面板翻转思路。
func _position_tooltip(card: Control, has_keywords: bool) -> void:
	if not is_instance_valid(card) or not is_instance_valid(effect_tooltip_panel):
		return

	var screen_size := host.get_viewport().get_visible_rect().size
	var actual_card_width := _get_card_display_width(card)
	var is_effect_placed_on_left := false

	var eff_w := effect_tooltip_panel.size.x
	var eff_h := effect_tooltip_panel.size.y
	var eff_x := card.global_position.x + actual_card_width + config.tooltip_offset_x
	var eff_y := card.global_position.y + config.tooltip_offset_y

	if eff_x + eff_w > screen_size.x:
		eff_x = card.global_position.x - eff_w - config.tooltip_offset_x
		is_effect_placed_on_left = true

	if eff_y + eff_h > screen_size.y - config.tooltip_bottom_margin:
		eff_y = screen_size.y - eff_h - config.tooltip_bottom_margin

	effect_tooltip_panel.global_position = Vector2(eff_x, eff_y)
	effect_tooltip_panel.modulate = Color(1, 1, 1, 1)

	if not has_keywords or not is_instance_valid(keywords_tooltip_hbox):
		return

	var kw_w := keywords_tooltip_hbox.size.x
	var kw_h := keywords_tooltip_hbox.size.y
	var kw_x := 0.0
	var kw_y := eff_y

	if not is_effect_placed_on_left:
		kw_x = eff_x + eff_w + config.tooltip_gap_x
		if kw_x + kw_w > screen_size.x:
			kw_x = card.global_position.x - kw_w - config.tooltip_offset_x
	else:
		kw_x = eff_x - kw_w - config.tooltip_gap_x
		if kw_x < 0:
			kw_x = card.global_position.x + actual_card_width + config.tooltip_offset_x

	if kw_y + kw_h > screen_size.y - config.tooltip_bottom_margin:
		kw_y = screen_size.y - kw_h - config.tooltip_bottom_margin

	keywords_tooltip_hbox.global_position = Vector2(kw_x, kw_y)
	keywords_tooltip_hbox.modulate = Color(1, 1, 1, 1)


## 估算卡牌当前真正显示在屏幕上的宽度。
## 某些 DraftCard 在刚进容器时 size 还没稳定，这里会回退到 custom_minimum_size。
func _get_card_display_width(card: Control) -> float:
	var base_width := card.size.x
	if is_zero_approx(base_width) and card.custom_minimum_size.x > 0.0:
		base_width = card.custom_minimum_size.x
	if is_zero_approx(base_width):
		base_width = 125.0
	return base_width * card.scale.x


## 统一判断对象是否声明了某个属性。
## 用这个辅助函数，可以避免直接 get("xxx") 时碰到不存在属性的对象。
func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false
