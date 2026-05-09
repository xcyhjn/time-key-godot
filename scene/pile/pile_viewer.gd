# 原文件名: pile_viewer(生成局外收获卡牌).gd
# 功能: 局内抽牌堆/弃牌堆查看器
# 核心逻辑: open_pile_view() 根据真实牌堆生成只读卡牌副本；背景遮罩吞掉下层输入；show_tooltip()/hide_tooltip() 复用通用卡牌 tooltip。
extends CanvasLayer

@onready var card_grid = $ScrollContainer/card
@onready var background = $background

# 引用 CardManager 来使用工厂生产展示用的卡牌
var card_manager: CardManager
var tooltip_presenter: CardTooltipPresenter = null
var _previous_tree_paused: bool = false
var selected_visual_card: Control = null

@export_group("Layer And Input")
@export var top_layer: int = 9000
@export var block_underlying_input: bool = true

@export_group("Card Grid")
@export var grid_horizontal_separation: int = 10
@export var grid_vertical_separation: int = 28
@export var randomize_view_order: bool = true

@export_group("Tooltip")
@export var tooltip_config: TooltipConfig = preload("res://scene/shared/tooltip/reward_card_tooltip_config.tres")
@export var show_keyword_tooltips: bool = false
@export var tooltip_fallback_text: String = "无效果文本"

@export_group("Selection Visual")
@export var selected_glow_color: Color = Color(1.0, 0.86, 0.25, 1.0)
@export var selected_glow_size: int = 22
@export var selected_border_width: int = 3


func _ready():
	# 初始隐藏
	layer = top_layer
	process_mode = Node.PROCESS_MODE_ALWAYS
	if is_instance_valid(background):
		background.mouse_filter = Control.MOUSE_FILTER_STOP if block_underlying_input else Control.MOUSE_FILTER_IGNORE
	_apply_grid_spacing()
	hide()
	$back.pressed.connect(_on_close_pressed)


func open_pile_view(source_pile: Pile, manager: CardManager):
	card_manager = manager
	layer = top_layer
	_pause_underlying_scene()
	hide_tooltip()
	show()
	if get_parent():
		get_parent().move_child(self, get_parent().get_child_count() - 1)

	# 1. 清理旧的显示内容
	selected_visual_card = null
	for child in card_grid.get_children():
		child.queue_free()

	# 2. 遍历源牌堆的所有卡牌
	# 注意：我们不能直接把 source_pile 里的卡牌移动过来，那样会破坏游戏逻辑。
	# 我们需要根据数据生成“视觉副本”。
	var cards_to_show := source_pile._held_cards.duplicate()
	if randomize_view_order:
		cards_to_show.shuffle()
	for real_card in cards_to_show:
		_create_visual_copy(real_card)


func _unhandled_input(event: InputEvent) -> void:
	if not visible or not block_underlying_input:
		return
	if event is InputEventMouseButton or event is InputEventMouseMotion:
		get_viewport().set_input_as_handled()


func _create_visual_copy(real_card: Card):
	# 1. 获取卡牌的基础场景（我们需要手动实例化，不能用工厂的 create_card）
	# 注意：这里需要引用 factory 里的 default_card_scene
	var card_scene = card_manager.card_factory.default_card_scene
	if not card_scene:
		push_error("CardFactory 没有设置 default_card_scene！")
		return
	# 2. 手动实例化卡牌
	var visual_card = card_scene.instantiate()
	# 3. 手动填充数据 (模仿工厂的 _create_card_node 逻辑)
	# 获取原卡牌的数据和图片
	var card_info = real_card.card_info
	var card_name = card_info.get("name", "unknown")

	# 设置卡牌基本属性
	visual_card.card_info = card_info
	visual_card.card_name = card_name
	_apply_visual_card_tooltip_data(real_card, visual_card, card_info)
		# 如果没有便捷方法，尝试从 card_info 重新加载（慢一点但稳妥）
		# 注意：这需要 factory 暴露加载图片的方法，或者我们自己加载
		# 简单起见，我们直接尝试复制原卡牌的 TextureRect/Sprite 的贴图
		# 请根据你的 Card 场景结构调整下面的路径，比如 $FrontSprite 或 $TextureRect
	var real_front_node = real_card.get_node_or_null("FrontFace/TextureRect")
	var front_texture = null

	if real_front_node and real_front_node.texture:
		front_texture = real_front_node.texture
	else:
		# 如果真卡没加载出来（极少情况），尝试从 factory 缓存拿，或者打印个警告
		pass

	# 2. 将图片“贴”到【副本卡】上
	# 我们直接操作副本卡的 TextureRect 节点，这样最直接，不依赖 set_faces
	var visual_front_node = visual_card.get_node_or_null("FrontFace/TextureRect")
	if visual_front_node:
		visual_front_node.texture = front_texture
		# 确保 TextureRect 撑满卡牌大小 (根据你的设置可能需要)
		visual_front_node.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		visual_front_node.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		visual_front_node.size = Vector2(125, 175)  # 强制对齐大小

	# 3. 处理背面 (给副本卡也贴上背面图)
	var back_texture = card_manager.card_factory.back_image
	var visual_back_node = visual_card.get_node_or_null("BackFace/TextureRect")
	if visual_back_node:
		visual_back_node.texture = back_texture
		visual_back_node.size = Vector2(125, 175)

	# --- 【关键】确保副本卡显示的是“正面” ---
	# 如果你的 Card 脚本里有 show_front 变量，设为 true
	if "show_front" in visual_card:
		visual_card.show_front = true

	# 手动强制显示正面节点，隐藏背面节点 (双重保险)
	if visual_front_node: visual_front_node.get_parent().visible = true
	if visual_back_node: visual_back_node.get_parent().visible = false
# --- 【关键修复 1】强制设置卡牌在 UI 中的大小 ---
	# GridContainer 需要知道子节点有多大，否则会把它压扁成 0x0
	visual_card.custom_minimum_size = Vector2(125, 175)

	# --- 【关键修复 2】修正偏移 ---
	# 很多卡牌设计的中心点在图片中心(0,0)，但在 UI 容器里，中心点需要在左上角
	# 我们用一个 Control 节点把卡牌位置“推”回来（如果你的卡牌显示只有 1/4，就调整这里）
	# visual_card.position = Vector2(62.5, 87.5) # 可选：如果卡牌只有右下角可见，取消这行注释

	# --- 【关键修复 3】确保卡牌是“正面朝上”的 ---
	if "card_face_up" in visual_card:
		visual_card.card_face_up = true
	# 调用可能存在的更新视觉的方法
	if visual_card.has_method("_update_visuals"):
		visual_card._update_visuals()
	# 4. 添加到 GridContainer 中
	card_grid.add_child(visual_card)

	# 5. 只保留 hover tooltip，禁用点击和拖拽。
	_disconnect_card_framework_input(visual_card)
	visual_card.set_process_input(false)
	visual_card.mouse_filter = Control.MOUSE_FILTER_PASS
	if "can_be_interacted_with" in visual_card:
		visual_card.can_be_interacted_with = false
	if visual_card.has_method("_set_shader"):
		visual_card._set_shader(false)
	_add_selection_highlight(visual_card)
	visual_card.mouse_entered.connect(_on_visual_card_mouse_entered.bind(visual_card))
	visual_card.mouse_exited.connect(_on_visual_card_mouse_exited.bind(visual_card))
	visual_card.gui_input.connect(_on_visual_card_gui_input.bind(visual_card))
	_disable_input_recursive(visual_card, visual_card)


func _disable_input_recursive(node: Node, root_card: Node = null):
	if node == root_card:
		for child in node.get_children():
			_disable_input_recursive(child, root_card)
		return

	if node is Control:
		node.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if node is CollisionObject2D:  # 如果卡牌用了物理检测
		node.input_pickable = false


	for child in node.get_children():
		_disable_input_recursive(child, root_card)


func _apply_visual_card_tooltip_data(real_card: Card, visual_card: Node, card_info: Dictionary) -> void:
	if "raw_description" in visual_card:
		var description := ""
		var real_description: Variant = real_card.get("raw_description")
		if real_description != null and str(real_description) != "":
			description = str(real_description)
		else:
			description = str(card_info.get("效果", ""))
		visual_card.set("raw_description", description)

	var real_keywords: Variant = real_card.get("active_keywords")
	if "active_keywords" in visual_card and real_keywords is Array:
		visual_card.set("active_keywords", real_keywords.duplicate())


func _disconnect_card_framework_input(card: Control) -> void:
	var gui_callable := Callable(card, "_on_gui_input")
	if card.gui_input.is_connected(gui_callable):
		card.gui_input.disconnect(gui_callable)

	var mouse_enter_callable := Callable(card, "_on_mouse_enter")
	if card.mouse_entered.is_connected(mouse_enter_callable):
		card.mouse_entered.disconnect(mouse_enter_callable)

	var mouse_exit_callable := Callable(card, "_on_mouse_exit")
	if card.mouse_exited.is_connected(mouse_exit_callable):
		card.mouse_exited.disconnect(mouse_exit_callable)


func _on_visual_card_mouse_entered(card: Control) -> void:
	show_tooltip(card)


func _on_visual_card_mouse_exited(card: Control) -> void:
	hide_tooltip(card)


func _on_visual_card_gui_input(event: InputEvent, card: Control) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_set_selected_visual_card(card)
		get_viewport().set_input_as_handled()


func show_tooltip(card: Control) -> void:
	_setup_tooltip_presenter()
	tooltip_presenter.show_card_tooltip(card, {
		"show_keywords": show_keyword_tooltips,
		"fallback_text": tooltip_fallback_text,
	})


func hide_tooltip(_card: Control = null) -> void:
	if tooltip_presenter != null:
		tooltip_presenter.hide_tooltip()


func _setup_tooltip_presenter() -> void:
	if tooltip_presenter != null:
		return
	tooltip_presenter = CardTooltipPresenter.new(self, tooltip_config, show_keyword_tooltips)
	tooltip_presenter.ensure_ui_created()
	if is_instance_valid(tooltip_presenter.tooltip_canvas):
		tooltip_presenter.tooltip_canvas.layer = top_layer + 1


func _pause_underlying_scene() -> void:
	if not block_underlying_input:
		return
	_previous_tree_paused = get_tree().paused
	get_tree().paused = true


func _restore_underlying_scene_pause() -> void:
	if not block_underlying_input:
		return
	get_tree().paused = _previous_tree_paused


func _apply_grid_spacing() -> void:
	if not is_instance_valid(card_grid):
		return
	card_grid.add_theme_constant_override("h_separation", grid_horizontal_separation)
	card_grid.add_theme_constant_override("v_separation", grid_vertical_separation)


func _add_selection_highlight(card: Control) -> void:
	var highlight := Panel.new()
	highlight.name = "SelectionHighlight"
	highlight.show_behind_parent = true
	highlight.mouse_filter = Control.MOUSE_FILTER_IGNORE
	highlight.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0, 0, 0, 0)
	style.border_color = Color(selected_glow_color.r, selected_glow_color.g, selected_glow_color.b, 0.0)
	style.shadow_color = Color(selected_glow_color.r, selected_glow_color.g, selected_glow_color.b, 0.0)
	style.shadow_size = selected_glow_size
	style.set_border_width_all(selected_border_width)
	style.set_corner_radius_all(6)
	highlight.add_theme_stylebox_override("panel", style)
	card.add_child(highlight)


func _set_selected_visual_card(card: Control) -> void:
	if selected_visual_card == card:
		_set_card_selected_visual(card, false)
		selected_visual_card = null
		return

	if is_instance_valid(selected_visual_card):
		_set_card_selected_visual(selected_visual_card, false)
	selected_visual_card = card
	_set_card_selected_visual(card, true)


func _set_card_selected_visual(card: Control, selected: bool) -> void:
	if not is_instance_valid(card):
		return
	var highlight := card.get_node_or_null("SelectionHighlight") as Panel
	if not is_instance_valid(highlight):
		return
	var style := highlight.get_theme_stylebox("panel") as StyleBoxFlat
	if not is_instance_valid(style):
		return

	var alpha := selected_glow_color.a if selected else 0.0
	var tw := create_tween().set_parallel(true).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
	tw.tween_property(style, "border_color:a", alpha, 0.12)
	tw.tween_property(style, "shadow_color:a", alpha, 0.12)
	tw.tween_property(card, "scale", Vector2(1.06, 1.06) if selected else Vector2.ONE, 0.12)


func _on_close_pressed():
	hide_tooltip()
	hide()
	_restore_underlying_scene_pause()
	selected_visual_card = null
	# 关闭时清理，节省内存
	for child in card_grid.get_children():
		child.queue_free()
