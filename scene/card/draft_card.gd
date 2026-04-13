# 原文件名: draft_card(局外收获).gd
# 功能: 局外收获卡牌
extends TextureRect

@export_group("发光与交互")
@export var hover_scale: float = 1.1
@export var glow_color: Color = Color(1.0, 0.9, 0.2, 1.0)  # 经典的黄光
@export var glow_size: int = 20  # 发光扩散范围
@export var corner_radius: int = 6  # 如果你的卡牌有圆角，调这个让发光也变圆

var raw_description: String = ""
var active_keywords: Array = []
var card_id: String = ""

var original_scale: Vector2 = Vector2.ONE
var is_selected: bool = false
var glow_style: StyleBoxFlat  # 记录发光材质

signal card_clicked(card_node)


func _ready():
	# 1. 锁死尺寸，防止面条卡
	custom_minimum_size = Vector2(125, 175)
	size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	size_flags_vertical = Control.SIZE_SHRINK_CENTER
	mouse_filter = Control.MOUSE_FILTER_STOP

	# ==========================================
	# ★ 核心修复：用代码构建绝对稳定的原生发光底层
	# ==========================================
	var glow_panel = Panel.new()
	glow_panel.show_behind_parent = true  # 藏在卡牌贴图后面
	glow_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	glow_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE  # 防止抢焦点

	glow_style = StyleBoxFlat.new()
	glow_style.bg_color = Color(0, 0, 0, 0)  # 中心透明
	glow_style.set_corner_radius_all(corner_radius)

	# 设置发光参数（初始透明度为 0）
	var hidden_color = glow_color
	hidden_color.a = 0.0
	glow_style.shadow_color = hidden_color
	glow_style.shadow_size = glow_size
	#在 Godot 4 中，StyleBoxFlat 没有一个叫做 border_width_all 的直接可赋值属性。
	#相反，它提供的是四个方向的独立属性（border_width_left, border_width_top 等）
	#以及一个叫做 set_border_width_all() 的快捷方法。
	glow_style.set_border_width_all(2)
	glow_style.border_color = hidden_color

	glow_panel.add_theme_stylebox_override("panel", glow_style)
	add_child(glow_panel)
	# ==========================================

	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)
	gui_input.connect(_on_gui_input)


func get_parsed_description() -> String:
	return raw_description


func _on_mouse_entered():
	if is_selected: return

	z_index = 10
	var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "scale", original_scale * hover_scale, 0.1)

	# 点亮发光！
	tw.tween_property(glow_style, "shadow_color:a", glow_color.a, 0.1)
	tw.tween_property(glow_style, "border_color:a", glow_color.a, 0.1)

	var main = get_tree().get_first_node_in_group("MainBoard")
	if main and main.has_method("show_tooltip"):
		main.show_tooltip(self)


func _on_mouse_exited():
	if is_selected: return

	z_index = 0
	var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(self, "scale", original_scale, 0.1)

	# 熄灭发光！
	tw.tween_property(glow_style, "shadow_color:a", 0.0, 0.1)
	tw.tween_property(glow_style, "border_color:a", 0.0, 0.1)

	var main = get_tree().get_first_node_in_group("MainBoard")
	if main and main.has_method("hide_tooltip"):
		main.hide_tooltip(self)


func _on_gui_input(event: InputEvent):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		emit_signal("card_clicked", self)
		get_viewport().set_input_as_handled()


func set_selected(selected: bool):
	is_selected = selected
	if is_selected:
		var tw = create_tween().set_parallel(true)
		tw.tween_property(self, "scale", original_scale * hover_scale, 0.1)
		tw.tween_property(glow_style, "shadow_color:a", glow_color.a, 0.1)
		tw.tween_property(glow_style, "border_color:a", glow_color.a, 0.1)
	else:
		_on_mouse_exited()
