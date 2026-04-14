# 文件名: RemoveReward.gd
# 功能: 删除卡牌奖励场景 - 基于"数据窃取"架构
# 设计原则: 模仿 ShopManager.gd 和 reward_manager.gd 的代码风格

extends CanvasLayer

## 卡牌数据池单例引用
var CardDataPool = preload("res://scene/global/CardDataPool.gd")
## CardManager 类型引用 (用于类型检查)
var CardManager = preload("res://addons/card-framework/card_manager.gd")

## ==========================================
## ★ 节点引用 - 必须在场景中正确连接
## ==========================================

@onready var background_mask: ColorRect = $BackgroundMask
@onready var title_label: Label = $TitleLabel
@onready var deck_scroll_container: ScrollContainer = $DeckScrollContainer
@onready var deck_grid: GridContainer = $DeckScrollContainer/DeckGrid
@onready var selected_card_display: Control = $SelectedCardDisplay
@onready var btn_back: Button = $BtnBack
@onready var btn_confirm: Button = $BtnConfirm

## 外部依赖注入 (必须由主场景在 _ready 中赋值)
var deck_manager = null  # 必须提供 card_factory 访问
var draft_card_scene = preload("res://scene/card/DraftCard.tscn")

## ==========================================
## ★ 场景状态变量
## ==========================================

# 当前显示的牌组卡牌列表 (DraftCard 实例)
var current_deck_cards: Array = []
# 当前选中的卡牌 (用于删除)
var selected_draft_card: Control = null
# 牌组原始卡牌ID列表 (用于还原)
var original_deck_card_ids: Array[String] = []

## ==========================================
## ★ 导出参数 - 可在检查器中动态调整
## ==========================================

@export_group("场景配置")
@export var deck_grid_columns: int = 4  # 牌网格列数

@export_group("卡牌排版配置")
@export var card_display_size: Vector2 = Vector2(125, 175)  # 动态控制生成的卡牌大小
@export var card_spacing_x: int = 20  # 卡牌水平间距
@export var card_spacing_y: int = 20  # 卡牌垂直间距

## ==========================================
## ★ 核心生命周期方法
## ==========================================

func _ready():
	# 绑定按钮信号
	btn_back.pressed.connect(_on_back_pressed)
	btn_confirm.pressed.connect(_on_confirm_pressed)
	
	# 初始隐藏确认按钮（未选择卡牌时不可用）
	btn_confirm.disabled = true
	
	# ★ 延迟一帧确保所有节点完成初始化
	await get_tree().process_frame
	
	# ★ 自动尝试查找 CardManager 作为 deck_manager
	if not deck_manager:
		_try_find_card_manager()
	
	# 设置标题
	if title_label:
		title_label.text = "选择一张卡牌从牌组中移除"
	
	# 配置牌网格
	deck_grid.columns = deck_grid_columns
	
	# 应用卡牌间距配置
	if deck_grid:
		deck_grid.add_theme_constant_override("h_separation", card_spacing_x)
		deck_grid.add_theme_constant_override("v_separation", card_spacing_y)

## 打开删除卡牌场景
func open():
	# 显示场景
	self.show()
	background_mask.show()
	
	# 重置状态
	_clear_deck_display()
	selected_draft_card = null
	btn_confirm.disabled = true
	
	# 生成牌组显示
	_generate_deck_display()
	
	# 显示UI元素
	deck_scroll_container.show()
	btn_back.show()
	btn_confirm.show()
	btn_confirm.disabled = true  # 未选择卡牌时不可用

## 关闭场景
func close():
	# 隐藏场景
	self.hide()
	background_mask.hide()
	
	# 清理资源
	_clear_deck_display()
	
	# ★ 模仿商店页面退出逻辑: 通知父场景恢复UI
	# 尝试查找父节点中的 _on_external_scene_exit_pressed 方法
	var parent = get_parent()
	if parent and parent.has_method("_on_external_scene_exit_pressed"):
		parent._on_external_scene_exit_pressed(self)
	else:
		# 备用方案: 尝试通过树查找主Project节点
		var main = get_tree().root.find_child("Project", true, false)
		if main and main.has_method("_on_external_scene_exit_pressed"):
			main._on_external_scene_exit_pressed(self)
		elif main and main.has_method("restore_ui_after_external_scene"):
			# 直接恢复UI，但不移除场景实例
			main.restore_ui_after_external_scene()
			# 延迟一帧后移除自己，避免立即queue_free导致问题
			await get_tree().process_frame
			self.queue_free()

## ==========================================
## ★ 牌组显示逻辑
## ==========================================

## 生成牌组显示 (显示当前牌组中的所有卡牌)
func _generate_deck_display():
	# 清空现有显示
	_clear_deck_display()
	
	# 检查必要依赖
	if not deck_manager:
		push_warning("RemoveReward: deck_manager 未设置! 牌组显示将为空。请确保已调用 set_deck_manager() 或 CardManager 已在场景中。")
		return
	
	# ★ 核心步骤1: 获取当前牌组中的卡牌
	# 这里需要对接你的牌组管理系统
	# 假设 deck_manager 有方法获取牌组卡牌列表
	var deck_card_ids = _get_current_deck_card_ids()
	original_deck_card_ids = deck_card_ids.duplicate()
	
	if deck_card_ids.size() == 0:
		print("牌组为空，无法删除卡牌")
		return
	
	print("删除场景 - 牌组中共有 %d 张卡牌" % deck_card_ids.size())
	
	# 创建临时幽灵牌堆 (用于数据窃取)
	var temp_pile = preload("res://addons/card-framework/pile.tscn").instantiate()
	add_child(temp_pile)
	temp_pile.visible = false
	
	# 为每张卡创建 DraftCard 预览
	for card_id in deck_card_ids:
		await _create_deck_card_display(card_id, temp_pile)
	
	# 销毁幽灵牌堆
	temp_pile.queue_free()

## 创建单个牌组卡牌显示
func _create_deck_card_display(card_id: String, temp_pile: Node):
	# 创建轻量级 DraftCard
	var draft_card = draft_card_scene.instantiate()
	draft_card.card_id = card_id
	
	# ★ 应用自定义尺寸设置
	draft_card.custom_set_size = card_display_size
	
	# ★ 数据窃取 - 从真实卡牌提取属性
	await _steal_card_data(card_id, draft_card, temp_pile)
	
	# 添加到牌网格
	deck_grid.add_child(draft_card)
	
	# 绑定点击事件
	draft_card.card_clicked.connect(_on_deck_card_clicked)
	
	# 记录到列表
	current_deck_cards.append(draft_card)
	
	# 等待一帧后记录原始缩放
	draft_card.call_deferred("set", "original_scale", draft_card.scale)

## 数据窃取核心函数 (模仿 ShopManager)
func _steal_card_data(card_id: String, draft_card: Control, temp_pile: Node):
	# 检查必要依赖
	if not deck_manager or not deck_manager.card_factory:
		return
	
	# 让工厂真实生产一张牌
	deck_manager.card_factory.create_card(card_id, temp_pile)
	
	# 等待一帧确保卡牌创建完成
	await get_tree().process_frame
	
	# 安全检查：确保 temp_pile 仍然有效
	if not is_instance_valid(temp_pile):
		print("❌ _steal_card_data: temp_pile 已被释放，无法继续")
		return
	
	# 获取刚创建的真实卡牌
	var real_card = temp_pile._held_cards[-1] if temp_pile._held_cards.size() > 0 else null
	if not real_card:
		print("警告: 无法为卡牌 %s 创建真实实例" % card_id)
		return
	
	# 1. 提取卡牌描述文本
	if "raw_description" in real_card and real_card.raw_description != "":
		draft_card.raw_description = real_card.raw_description
	elif "card_info" in real_card and typeof(real_card.card_info) == TYPE_DICTIONARY:
		draft_card.raw_description = real_card.card_info.get("效果", "")
	
	# 尝试调用卡牌的解析方法
	if real_card.has_method("get_parsed_description"):
		draft_card.raw_description = real_card.get_parsed_description()
	
	# 2. 提取关键词词条
	if "active_keywords" in real_card:
		draft_card.active_keywords = real_card.active_keywords.duplicate()
	
	# 3. 偷取真牌贴图
	if real_card.has_node("FrontFace/TextureRect"):
		draft_card.texture = real_card.get_node("FrontFace/TextureRect").texture
	elif "front_face_texture" in real_card and real_card.front_face_texture != null:
		if real_card.front_face_texture is TextureRect:
			draft_card.texture = real_card.front_face_texture.texture
		elif real_card.front_face_texture is Texture2D:
			draft_card.texture = real_card.front_face_texture
	
	# 4. 从牌堆移除临时卡牌
	temp_pile.remove_card(real_card)
	real_card.queue_free()

## ==========================================
## ★ 事件处理
## ==========================================

## 牌组卡牌点击事件 (单选)
func _on_deck_card_clicked(clicked_card: Control):
	selected_draft_card = clicked_card
	
	# 更新所有卡牌的选中状态
	for card in current_deck_cards:
		card.set_selected(card == clicked_card)
	
	# 启用确认按钮
	btn_confirm.disabled = false
	
	# 显示选中卡牌详细信息
	_update_selected_card_display(clicked_card)

## 更新选中卡牌显示区域
func _update_selected_card_display(card: Control):
	# 清空显示区域
	for child in selected_card_display.get_children():
		child.queue_free()
	
	# 创建选中卡牌的副本用于显示
	var display_card = draft_card_scene.instantiate()
	display_card.card_id = card.card_id
	display_card.raw_description = card.raw_description
	display_card.active_keywords = card.active_keywords.duplicate() if card.active_keywords else []
	display_card.texture = card.texture
	
	# 设置显示属性
	display_card.scale = Vector2(1.2, 1.2)  # 稍微放大
	# 禁用交互（TextureRect没有disabled属性，使用mouse_filter）
	display_card.mouse_filter = Control.MOUSE_FILTER_IGNORE
	
	# 添加到显示区域
	selected_card_display.add_child(display_card)

## 返回按钮
func _on_back_pressed():
	print("返回主选项...")
	close()

## 确认删除按钮
func _on_confirm_pressed():
	if not selected_draft_card:
		print("错误: 没有选中的卡牌!")
		return
	
	print("确认删除卡牌：%s" % selected_draft_card.card_id)
	
	# 禁用按钮防止重复点击
	btn_confirm.disabled = true
	btn_back.disabled = true
	
	# 1. 执行删除动画 (卡牌缩小淡出)
	var tw = create_tween()
	tw.tween_property(selected_draft_card, "scale", Vector2.ZERO, 0.4)
	tw.parallel().tween_property(selected_draft_card, "modulate:a", 0.0, 0.4)
	
	# 2. 动画结束后执行实际删除
	tw.tween_callback(func():
		# ★ 核心数据操作: 从牌组中移除卡牌
		_remove_card_from_deck(selected_draft_card.card_id)
		
		# 从显示中移除卡牌
		deck_grid.remove_child(selected_draft_card)
		selected_draft_card.queue_free()
		current_deck_cards.erase(selected_draft_card)
		
		# 清空选中显示
		for child in selected_card_display.get_children():
			child.queue_free()
		
		selected_draft_card = null
		
		# 如果牌组为空，自动关闭场景
		if current_deck_cards.size() == 0:
			print("牌组已空，自动关闭删除场景")
			close()
		else:
			# 重新启用返回按钮
			btn_back.disabled = false
	)

## ==========================================
## ★ 核心数据操作
## ==========================================

## 从牌组中移除卡牌 (需要对接你的牌组管理系统)
func _remove_card_from_deck(card_id: String):
	# ★ 这里需要对接你的牌组管理系统
	# 示例: 假设 deck_manager 有 remove_card_from_deck 方法
	if deck_manager and deck_manager.has_method("remove_card_from_deck"):
		deck_manager.remove_card_from_deck(card_id)
		print("✅ 已从牌组中移除卡牌: %s" % card_id)
	else:
		# 备用方案: 从全局牌组列表中移除
		print("⚠️ 移除卡牌 %s (需要对接牌组管理系统)" % card_id)
		
		# 这里可以对接你的全局牌组数据
		# 例如: GlobalDeck.remove_card(card_id)

## 获取当前牌组卡牌ID列表 (需要对接你的牌组管理系统)
func _get_current_deck_card_ids() -> Array[String]:
	# ★ 这里需要对接你的牌组管理系统
	# 示例: 假设 deck_manager 有 get_deck_card_ids 方法
	if deck_manager and deck_manager.has_method("get_deck_card_ids"):
		return deck_manager.get_deck_card_ids()
	
	# 备用方案: 返回模拟数据用于测试
	print("⚠️ 使用模拟牌组数据 (需要对接牌组管理系统)")
	return ["1", "2", "3"]  # 示例卡牌ID（已移除不存在的pot和hailong）

## ==========================================
## ★ 工具函数
## ==========================================

## 清空牌组显示
func _clear_deck_display():
	# 清空所有动态生成的卡牌
	for card in current_deck_cards:
		if is_instance_valid(card):
			card.queue_free()
	
	current_deck_cards.clear()
	selected_draft_card = null
	original_deck_card_ids.clear()
	
	# 清空网格容器
	for child in deck_grid.get_children():
		child.queue_free()
	
	# 清空选中显示区域
	for child in selected_card_display.get_children():
		child.queue_free()

## 尝试自动查找 CardManager 节点
func _try_find_card_manager():
	# 方案1: 通过元数据查找 (CardManager 在 _ready() 中将自己注册到场景根)
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		var card_manager = tree_root.get_meta("card_manager")
		if card_manager is CardManager:
			deck_manager = card_manager
			print("✅ RemoveReward: 通过场景根元数据找到 CardManager")
			return
	
	# 方案2: 通过当前场景元数据查找
	var scene_root = get_tree().current_scene
	if scene_root and scene_root.has_meta("card_manager"):
		var card_manager = scene_root.get_meta("card_manager")
		if card_manager is CardManager:
			deck_manager = card_manager
			print("✅ RemoveReward: 通过当前场景元数据找到 CardManager")
			return
	
	# 方案3: 通过父节点链查找 (如果 RemoveReward 是 CardManager 的子节点)
	var parent = get_parent()
	while parent:
		if parent is CardManager:
			deck_manager = parent
			print("✅ RemoveReward: 通过父节点链找到 CardManager: %s" % parent.name)
			return
		parent = parent.get_parent()
	
	# 方案4: 通过节点名查找 (回退方案)
	var scene_root_node = get_tree().root
	var card_manager_node = scene_root_node.find_child("CardManager", true, false)
	if card_manager_node and card_manager_node is CardManager:
		deck_manager = card_manager_node
		print("✅ RemoveReward: 通过节点名找到 CardManager: %s" % card_manager_node.name)
		return
	
	print("⚠️ RemoveReward: 未能自动找到 CardManager，需要手动调用 set_deck_manager()")

## ==========================================
## ★ 外部接口
## ==========================================

## 设置 deck_manager 引用 (必须由主场景调用)
func set_deck_manager(manager):
	deck_manager = manager
	print("RemoveReward: deck_manager 已设置")

## ==========================================
## ★ Tooltip系统 (简化版)
## ==========================================

var _tooltip_panel: PanelContainer = null
var _tooltip_label: RichTextLabel = null
var _current_tooltip_card: Control = null

# 显示tooltip
func show_tooltip(card: Control):
	# 确保tooltip面板已创建
	if _tooltip_panel == null:
		_create_tooltip_panel()
	
	_current_tooltip_card = card
	
	# 获取描述文本
	var description_text = ""
	if card.has_method("get_parsed_description"):
		description_text = card.get_parsed_description()
	elif card.has("raw_description"):
		description_text = card.raw_description
	
	if description_text == null:
		description_text = ""
	
	_tooltip_label.text = description_text
	
	# 重置面板尺寸
	_tooltip_panel.size = Vector2.ZERO
	_tooltip_panel.modulate = Color(1, 1, 1, 0)
	_tooltip_panel.show()
	
	# 等待一帧计算尺寸
	await get_tree().process_frame
	if _current_tooltip_card != card:
		return
	
	# 计算位置
	var screen_size = get_viewport().get_visible_rect().size
	var actual_card_width = card.size.x * card.scale.x
	
	var panel_w = _tooltip_panel.size.x
	var panel_h = _tooltip_panel.size.y
	var panel_x = card.global_position.x + actual_card_width + 15  # tooltip_offset_x
	var panel_y = card.global_position.y + 0  # tooltip_offset_y
	
	# 边界检查
	if panel_x + panel_w > screen_size.x:
		panel_x = card.global_position.x - panel_w - 15
	
	if panel_y + panel_h > screen_size.y - 2:  # tooltip_bottom_margin
		panel_y = screen_size.y - panel_h - 2
	
	_tooltip_panel.global_position = Vector2(panel_x, panel_y)
	_tooltip_panel.modulate = Color(1, 1, 1, 1)

# 隐藏tooltip
func hide_tooltip(card: Control = null):
	_current_tooltip_card = null
	if _tooltip_panel != null and is_instance_valid(_tooltip_panel):
		_tooltip_panel.hide()

# 创建tooltip面板
func _create_tooltip_panel():
	# 创建tooltip画布层
	var tooltip_canvas = CanvasLayer.new()
	tooltip_canvas.layer = 2000  # 最高层级
	add_child(tooltip_canvas)
	
	# 面板
	_tooltip_panel = PanelContainer.new()
	_tooltip_panel.z_index = 1000
	_tooltip_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_tooltip_panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	_tooltip_panel.hide()
	tooltip_canvas.add_child(_tooltip_panel)
	
	# 样式设置
	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.12, 0.12, 0.95)  # tooltip_bg_color
	style.border_width_left = 2  # tooltip_border_width
	style.border_width_top = 2
	style.border_width_right = 2
	style.border_width_bottom = 2
	style.border_color = Color(0.8, 0.6, 0.2, 1.0)  # tooltip_border_color
	style.set_corner_radius_all(6)
	_tooltip_panel.add_theme_stylebox_override("panel", style)
	
	# 边距容器
	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 15)
	margin.add_theme_constant_override("margin_right", 15)
	margin.add_theme_constant_override("margin_top", 15)
	margin.add_theme_constant_override("margin_bottom", 15)
	_tooltip_panel.add_child(margin)
	
	# 文本标签
	_tooltip_label = RichTextLabel.new()
	_tooltip_label.bbcode_enabled = true
	_tooltip_label.fit_content = true
	_tooltip_label.custom_minimum_size = Vector2(240, 0)  # effect_panel_width
	_tooltip_label.add_theme_font_size_override("normal_font_size", 16)  # tooltip_effect_font_size
	_tooltip_label.add_theme_color_override("default_color", Color(0.95, 0.95, 0.95, 1.0))  # tooltip_effect_font_color
	margin.add_child(_tooltip_label)
