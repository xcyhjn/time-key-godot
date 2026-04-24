## tooltip_config.gd
## -------------------------------------------------------------------
## 这个 Resource 的职责只有一个：收拢 Tooltip 的“静态配置”。
##
## 为什么要单独抽成 Resource：
## 1. 让主战斗、商店、奖励页可以共享同一套样式定义。
## 2. 让不同场景只通过替换 .tres，就能切换不同的边距、偏移和字体。
## 3. 让 TooltipPresenter 专注于“显示逻辑”，而不是把一堆颜色/字号硬编码在脚本里。
##
## 注意：
## - 这个文件不负责 add_child / show / hide / await process_frame。
## - 它只负责“告诉 presenter 应该长什么样、摆在哪里”。
## -------------------------------------------------------------------
class_name TooltipConfig
extends Resource


@export_group("Content Width")
## 右侧关键词解释框的最小宽度。
@export var tooltip_width: int = 220
## 主效果框的最小宽度。
@export var effect_panel_width: int = 240

@export_group("Stack Layout")
## 同一列关键词面板之间的垂直间距。
@export var tooltip_spacing: int = 8
## 主效果框与关键词列、以及关键词列与列之间的横向间距。
@export var tooltip_gap_x: int = 15
## Tooltip 相对于卡牌右侧的水平偏移量。
@export var tooltip_offset_x: int = 15
## Tooltip 相对于卡牌顶部的垂直偏移量。
@export var tooltip_offset_y: int = 0
## Tooltip 距离屏幕底边的安全距离。
@export var tooltip_bottom_margin: int = 2
## 每一列最多放多少个关键词面板。
@export var max_keywords_per_column: int = 3

@export_group("Frame Style")
## 边框粗细。
@export var tooltip_border_width: int = 2
## 边框颜色。
@export var tooltip_border_color: Color = Color(0.8, 0.6, 0.2, 1.0)
## 面板背景色。
@export var tooltip_bg_color: Color = Color(0.12, 0.12, 0.12, 0.95)
## 圆角大小。
@export var tooltip_corner_radius: int = 6

@export_group("Main Effect Text")
## 主效果文本字号。
@export var tooltip_effect_font_size: int = 16
## 主效果文本颜色。
@export var tooltip_effect_font_color: Color = Color(0.95, 0.95, 0.95, 1.0)
## 主效果文本字体，可选。
@export var tooltip_effect_font: Font

@export_group("Keyword Text")
## 关键词标题字号。
@export var tooltip_title_size: int = 18
## 关键词描述字号。
@export var tooltip_desc_size: int = 14
## 关键词描述文字颜色。
@export var tooltip_desc_color: Color = Color(0.9, 0.9, 0.9, 1.0)
## 关键词标题字体，可选。
@export var tooltip_title_font: Font
## 关键词描述字体，可选。
@export var tooltip_desc_font: Font

@export_group("Effect Panel Margin")
## 主效果框的四向内边距。
@export var effect_margin_left: int = 15
@export var effect_margin_right: int = 15
@export var effect_margin_top: int = 15
@export var effect_margin_bottom: int = 15

@export_group("Keyword Panel Margin")
## 关键词面板的四向内边距。
@export var keyword_margin_left: int = 12
@export var keyword_margin_right: int = 12
@export var keyword_margin_top: int = 10
@export var keyword_margin_bottom: int = 10


## 统一生成 Tooltip 外框的 StyleBox。
## presenter 每次给不同的 PanelContainer 套用时，都应该拿一个新的副本，
## 避免多个 Tooltip Panel 共用同一个 StyleBox 实例后出现联动污染。
func create_panel_stylebox() -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = tooltip_bg_color
	style.border_width_left = tooltip_border_width
	style.border_width_top = tooltip_border_width
	style.border_width_right = tooltip_border_width
	style.border_width_bottom = tooltip_border_width
	style.border_color = tooltip_border_color
	style.set_corner_radius_all(tooltip_corner_radius)
	return style


## 给主效果框的 MarginContainer 套用统一边距。
func apply_effect_margin(margin: MarginContainer) -> void:
	if margin == null:
		return
	margin.add_theme_constant_override("margin_left", effect_margin_left)
	margin.add_theme_constant_override("margin_right", effect_margin_right)
	margin.add_theme_constant_override("margin_top", effect_margin_top)
	margin.add_theme_constant_override("margin_bottom", effect_margin_bottom)


## 给关键词面板的 MarginContainer 套用统一边距。
func apply_keyword_margin(margin: MarginContainer) -> void:
	if margin == null:
		return
	margin.add_theme_constant_override("margin_left", keyword_margin_left)
	margin.add_theme_constant_override("margin_right", keyword_margin_right)
	margin.add_theme_constant_override("margin_top", keyword_margin_top)
	margin.add_theme_constant_override("margin_bottom", keyword_margin_bottom)


## 主效果富文本标签的视觉配置。
func apply_effect_label_style(label: RichTextLabel) -> void:
	if label == null:
		return
	label.bbcode_enabled = true
	label.fit_content = true
	label.scroll_active = false
	label.custom_minimum_size = Vector2(effect_panel_width, 0)
	label.add_theme_font_size_override("normal_font_size", tooltip_effect_font_size)
	label.add_theme_color_override("default_color", tooltip_effect_font_color)
	if tooltip_effect_font != null:
		label.add_theme_font_override("normal_font", tooltip_effect_font)
		label.add_theme_font_override("bold_font", tooltip_effect_font)
		label.add_theme_font_override("bold_italics_font", tooltip_effect_font)
		label.add_theme_font_override("italics_font", tooltip_effect_font)
		label.add_theme_font_override("mono_font", tooltip_effect_font)


## 关键词标题标签的视觉配置。
func apply_keyword_title_style(label: Label, title_color: String) -> void:
	if label == null:
		return
	label.add_theme_font_size_override("font_size", tooltip_title_size)
	label.add_theme_color_override("font_color", Color(title_color))
	if tooltip_title_font != null:
		label.add_theme_font_override("font", tooltip_title_font)


## 关键词描述富文本标签的视觉配置。
func apply_keyword_desc_style(label: RichTextLabel) -> void:
	if label == null:
		return
	label.bbcode_enabled = true
	label.fit_content = true
	label.scroll_active = false
	label.autowrap_mode = TextServer.AUTOWRAP_ARBITRARY
	label.custom_minimum_size = Vector2(tooltip_width, 0)
	label.add_theme_font_size_override("normal_font_size", tooltip_desc_size)
	label.add_theme_color_override("default_color", tooltip_desc_color)
	if tooltip_desc_font != null:
		label.add_theme_font_override("normal_font", tooltip_desc_font)
		label.add_theme_font_override("bold_font", tooltip_desc_font)
		label.add_theme_font_override("bold_italics_font", tooltip_desc_font)
		label.add_theme_font_override("italics_font", tooltip_desc_font)
		label.add_theme_font_override("mono_font", tooltip_desc_font)
