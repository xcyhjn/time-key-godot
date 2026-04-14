# 文件名: AcquireReward.gd
# 功能: 获取卡牌奖励场景 - 基于"数据窃取"与"飞入"架构
# 设计原则: 模仿 ShopManager.gd 和 reward_manager.gd 的代码风格

extends CanvasLayer

## 卡牌数据池单例引用
var CardDataPool = preload("res://gd_db/CardDataPool.gd")
## CardManager 类型引用 (用于类型检查)
var CardManager = preload("res://addons/card-framework/card_manager.gd")

## ==========================================
## ★ 节点引用 - 必须在场景中正确连接
## ==========================================

@onready var background_mask: ColorRect = $BackgroundMask
@onready var title_label: Label = $TitleLabe  # 注意: 场景中节点名为 TitleLabe
@onready var card_container: HBoxContainer = $CardContainer
@onready var btn_back: Button = $BtnBack
@onready var btn_confirm: Button = $BtnConfirm

## 外部依赖注入 (必须由主场景在 _ready 中赋值)
var deck_manager = null  # 必须提供 card_factory 访问
var draft_card_scene = preload("res://scenes/DraftCard.tscn")

## ==========================================
## ★ 场景状态变量
## ==========================================

# 当前显示的候选卡牌列表 (DraftCard 实例)
var current_draft_cards: Array = []
# 当前选中的卡牌
var selected_draft_card: Control = null

## ==========================================
## ★ 导出参数 - 可在检查器中动态调整
## ==========================================

@export_group("飞入牌库特效")
@export var trail_color: Color = Color(1.0, 0.2, 0.2, 0.8)  # 红色拖影
@export var trail_width: float = 15.0
@export var fly_duration: float = 0.6

@export_group("卡牌选择配置")
@export var card_selection_count: int = 3  # 显示的卡牌数量

@export_group("卡牌排版配置")
@export var card_display_size: Vector2 = Vector2(125, 175)  # 动态控制生成的卡牌大小
@export var card_spacing_x: int = 20  # 卡牌水平间距 (HBoxContainer使用)
# 注意：AcquireReward 使用 HBoxContainer，垂直间距忽略

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
		title_label.text = "选择一张卡牌加入牌组"
	
	# 应用卡牌间距配置
	if card_container:
		card_container.add_theme_constant_override("separation", card_spacing_x)

## 打开获取卡牌场景
func open():
	# 显示场景
	self.show()
	background_mask.show()
	
	# 重置状态
	_close_current_selection()
	
	# 生成候选卡牌
	_generate_candidate_cards()
	
	# 显示UI元素
	card_container.show()
	btn_back.show()
	btn_confirm.show()
	btn_confirm.disabled = true  # 未选择卡牌时不可用

## 关闭场景
func close():
	# 隐藏场景
	self.hide()
	background_mask.hide()
	
	# 清理资源
	_close_current_selection()
	
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
## ★ 卡牌生成逻辑 - 核心"数据窃取"架构
## ==========================================

## 生成候选卡牌 (模仿 reward_manager._on_acquire_pressed)
func _generate_candidate_cards():
	# 清空现有卡牌
	_close_current_selection()
	
	# 检查必要依赖
	if not deck_manager or not deck_manager.card_factory:
		push_warning("AcquireReward: deck_manager 或 card_factory 未设置! 卡牌选择将显示为空。请确保已调用 set_deck_manager() 或 CardManager 已在场景中。")
		return
	
	# ★ 核心步骤1: 获取当前时代值
	var current_era = _get_current_era()
	print("获取卡牌场景 - 当前时代: %d" % current_era)
	
	# 从卡牌数据池获取当前时代的卡牌列表
	var card_pool = CardDataPool.get_instance()
	if not card_pool:
		push_error("AcquireReward: 无法获取 CardDataPool 单例!")
		return
	
	var era_cards = card_pool.get_cards_by_era(current_era)
	if era_cards.size() == 0:
		push_error("AcquireReward: 时代 %d 没有可用的卡牌!" % current_era)
		return
	
	# 随机选择卡牌
	era_cards.shuffle()
	var selected_cards = era_cards.slice(0, min(card_selection_count, era_cards.size()))
	
	# 创建临时幽灵牌堆 (完全隐形)
	var temp_pile = preload("res://addons/card-framework/pile.tscn").instantiate()
	add_child(temp_pile)
	temp_pile.visible = false
	
	# 为每张卡创建 DraftCard 预览
	for card_id in selected_cards:
		await _create_draft_card(card_id, temp_pile)
	
	# 销毁幽灵牌堆
	temp_pile.queue_free()

## 创建单个 DraftCard 预览 (数据窃取)
func _create_draft_card(card_id: String, temp_pile: Node):
	# 创建商店专用轻量级 DraftCard
	var draft_card = draft_card_scene.instantiate()
	draft_card.card_id = card_id
	
	# ★ 应用自定义尺寸设置
	draft_card.custom_set_size = card_display_size
	
	# ★ 核心步骤: 数据窃取 - 从真实卡牌提取属性
	await _steal_card_data(card_id, draft_card, temp_pile)
	
	# 添加到容器
	card_container.add_child(draft_card)
	
	# 绑定点击事件
	draft_card.card_clicked.connect(_on_draft_card_clicked)
	
	# 记录到列表
	current_draft_cards.append(draft_card)
	
	# 等待一帧后记录原始缩放，防止动画错乱
	draft_card.call_deferred("set", "original_scale", draft_card.scale)

## 数据窃取核心函数 (模仿 ShopManager)
func _steal_card_data(card_id: String, draft_card: Control, temp_pile: Node):
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

## 卡牌点击事件 (单选互斥)
func _on_draft_card_clicked(clicked_card: Control):
	selected_draft_card = clicked_card
	
	# 更新所有卡牌的选中状态
	for card in current_draft_cards:
		card.set_selected(card == clicked_card)
	
	# 启用确认按钮
	btn_confirm.disabled = false

## 返回按钮
func _on_back_pressed():
	print("返回主选项...")
	close()

## 确认选择按钮
func _on_confirm_pressed():
	if not selected_draft_card:
		print("错误: 没有选中的卡牌!")
		return
	
	print("确认获取卡牌：%s" % selected_draft_card.card_id)
	
	# 禁用按钮防止重复点击
	btn_confirm.disabled = true
	btn_back.disabled = true
	
	# 1. 未被选中的牌淡出消失
	for card in current_draft_cards:
		if card != selected_draft_card:
			var tw = create_tween()
			tw.tween_property(card, "modulate:a", 0.0, 0.3)
	
	# 2. 剥离选中的卡牌，准备自由飞翔
	var global_pos = selected_draft_card.global_position
	card_container.remove_child(selected_draft_card)
	self.add_child(selected_draft_card)
	selected_draft_card.global_position = global_pos
	
	# 3. 执行飞入动画
	_fly_to_deck_pile(selected_draft_card)

## 飞入牌库动画 (模仿 ShopManager)
func _fly_to_deck_pile(card: Control):
	# 1. 创建红色尾焰 Line2D 拖影
	var trail = Line2D.new()
	trail.width = trail_width
	trail.default_color = trail_color
	trail.z_index = card.z_index - 1
	
	# 让拖影头部尖锐，尾部变细
	var curve = Curve.new()
	curve.add_point(Vector2(0, 0))
	curve.add_point(Vector2(1, 1))
	trail.width_curve = curve
	self.add_child(trail)
	
	# 2. 飞行 Tween 动画
	# 假设抽牌堆在屏幕左下角偏下的位置
	var deck_target_pos = Vector2(100, get_viewport().get_visible_rect().size.y + 100)
	var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
	
	tw.tween_property(card, "global_position", deck_target_pos, fly_duration)
	tw.tween_property(card, "scale", Vector2.ZERO, fly_duration)
	tw.tween_property(card, "rotation", PI * 2, fly_duration)  # 旋转效果
	
	# 3. 实时更新红尾焰轨迹
	var trail_timer = Timer.new()
	trail_timer.wait_time = 0.01
	trail_timer.autostart = true
	self.add_child(trail_timer)
	
	trail_timer.timeout.connect(func():
		trail.add_point(card.global_position + card.size / 2)
		# 保持拖影长度不超过 20 个点
		if trail.get_point_count() > 20:
			trail.remove_point(0)
	)
	
	# 4. 动画结束后的清理与实质数据添加
	tw.chain().tween_callback(func():
		trail_timer.queue_free()
		trail.queue_free()
		card.queue_free()
		
		# ★ 核心数据打通：让工厂真正生产这张牌进抽牌堆！
		if deck_manager != null and deck_manager.card_factory != null:
			var main = get_tree().get_first_node_in_group("MainBoard")
			if main and main.deck_pile:
				deck_manager.card_factory.create_card(card.card_id, main.deck_pile)
				print("✅ 已成功将 %s 加入抽牌堆！" % card.card_id)
		
		# 关闭场景
		close()
	)

## ==========================================
## ★ 工具函数
## ==========================================

## 清理当前选择的卡牌
func _close_current_selection():
	# 清空所有动态生成的卡牌
	for card in current_draft_cards:
		if is_instance_valid(card):
			card.queue_free()
	
	current_draft_cards.clear()
	selected_draft_card = null
	
	# 清空容器
	for child in card_container.get_children():
		child.queue_free()

## 获取当前时代 (需要对接你的全局时代系统)
func _get_current_era() -> int:
	# 尝试查找 GlobalClock 单例
	if Engine.has_singleton("GlobalClock"):
		var global_clock = Engine.get_singleton("GlobalClock")
		if global_clock and global_clock.has_method("get_current_era"):
			return global_clock.get_current_era()
	
	# 备用方案: 从场景中查找
	var root = Engine.get_main_loop().root
	var clock = root.find_child("GlobalClock", true, false)
	if clock and clock.has_method("get_current_era"):
		return clock.get_current_era()
	
	# 默认值
	return 1

## 尝试自动查找 CardManager 节点
func _try_find_card_manager():
	# 方案1: 通过元数据查找 (CardManager 在 _ready() 中将自己注册到场景根)
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		var card_manager = tree_root.get_meta("card_manager")
		if card_manager is CardManager:
			deck_manager = card_manager
			print("✅ AcquireReward: 通过场景根元数据找到 CardManager")
			return
	
	# 方案2: 通过当前场景元数据查找
	var scene_root = get_tree().current_scene
	if scene_root and scene_root.has_meta("card_manager"):
		var card_manager = scene_root.get_meta("card_manager")
		if card_manager is CardManager:
			deck_manager = card_manager
			print("✅ AcquireReward: 通过当前场景元数据找到 CardManager")
			return
	
	# 方案3: 通过父节点链查找 (如果 AcquireReward 是 CardManager 的子节点)
	var parent = get_parent()
	while parent:
		if parent is CardManager:
			deck_manager = parent
			print("✅ AcquireReward: 通过父节点链找到 CardManager: %s" % parent.name)
			return
		parent = parent.get_parent()
	
	# 方案4: 通过节点名查找 (回退方案)
	var scene_root_node = get_tree().root
	var card_manager_node = scene_root_node.find_child("CardManager", true, false)
	if card_manager_node and card_manager_node is CardManager:
		deck_manager = card_manager_node
		print("✅ AcquireReward: 通过节点名找到 CardManager: %s" % card_manager_node.name)
		return
	
	print("⚠️ AcquireReward: 未能自动找到 CardManager，需要手动调用 set_deck_manager()")

## ==========================================
## ★ 外部接口
## ==========================================

## 设置 deck_manager 引用 (必须由主场景调用)
func set_deck_manager(manager):
	deck_manager = manager
	print("AcquireReward: deck_manager 已设置")

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
