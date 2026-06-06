# 文件名: AcquireReward.gd
# 功能: 获取卡牌奖励场景 - 基于"数据窃取"与"飞入"架构
# 设计原则: 模仿 ShopManager.gd 和 reward_manager.gd 的代码风格

extends CanvasLayer

signal reward_scene_close_requested(scene_instance: Node)

## 卡牌数据池单例引用
var CardDataPool = preload("res://scene/global/CardDataPool.gd")
## CardManager 类型引用 (用于类型检查)
var CardManager = preload("res://addons/card-framework/card_manager.gd")
const RewardCardFlyToDeckAnimatorScript = preload("res://scene/in_scene/rewards/animation/RewardCardFlyToDeckAnimator.gd")
const RewardDeckSyncBridgeScript = preload("res://scene/in_scene/rewards/bridges/RewardDeckSyncBridge.gd")
const RewardCardManagerLocatorScript = preload("res://scene/in_scene/rewards/bridges/RewardCardManagerLocator.gd")
const RewardTooltipAdapterScript = preload("res://scene/in_scene/rewards/presenters/RewardTooltipAdapter.gd")
const RewardDraftCardFactoryScript = preload("res://scene/in_scene/rewards/factory/RewardDraftCardFactory.gd")
const RewardTempPileFactoryScript = preload("res://scene/in_scene/rewards/factory/RewardTempPileFactory.gd")
const RewardCardDescriptionExtractorScript = preload("res://scene/in_scene/rewards/presenters/RewardCardDescriptionExtractor.gd")
const RewardCardTextureExtractorScript = preload("res://scene/in_scene/rewards/presenters/RewardCardTextureExtractor.gd")
const RewardRealCardSpawnerScript = preload("res://scene/in_scene/rewards/factory/RewardRealCardSpawner.gd")
const RewardRealCardCleanerScript = preload("res://scene/in_scene/rewards/factory/RewardRealCardCleaner.gd")
const RewardDraftCardDataApplierScript = preload("res://scene/in_scene/rewards/presenters/RewardDraftCardDataApplier.gd")

## ==========================================
## ★ 节点引用 - 必须在场景中正确连接
## ==========================================

@onready var background_mask: Panel = $BackgroundMask
@onready var title_label: Label = $TitleLabe  # 注意: 场景中节点名为 TitleLabe
@onready var card_container: HBoxContainer = $CardContainer
@onready var btn_back: Button = $BtnBack
@onready var btn_confirm: Button = $BtnConfirm

## 外部依赖注入 (必须由主场景在 _ready 中赋值)
var deck_manager = null  # 必须提供 card_factory 访问
var draft_card_scene = preload("res://scene/card/DraftCard.tscn")

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

@export_group("Tooltip资源配置")
@export var tooltip_config: TooltipConfig = preload("res://scene/shared/tooltip/reward_card_tooltip_config.tres")

## 获取卡牌奖励页也复用通用 Tooltip presenter。
var tooltip_presenter: CardTooltipPresenter = null
var _card_manager_locator = null
var _tooltip_adapter = null
var _draft_card_factory = null
var _temp_pile_factory = null
var _card_description_extractor = null
var _card_texture_extractor = null
var _real_card_spawner = null
var _real_card_cleaner = null
var _draft_card_data_applier = null
var _fly_to_deck_animator = null
var _deck_sync_bridge = null


func _get_card_manager_locator():
	if _card_manager_locator == null:
		_card_manager_locator = RewardCardManagerLocatorScript.new()
	return _card_manager_locator


func _get_tooltip_adapter():
	if _tooltip_adapter == null:
		_tooltip_adapter = RewardTooltipAdapterScript.new()
	return _tooltip_adapter


func _get_draft_card_factory():
	if _draft_card_factory == null:
		_draft_card_factory = RewardDraftCardFactoryScript.new()
	return _draft_card_factory


func _get_temp_pile_factory():
	if _temp_pile_factory == null:
		_temp_pile_factory = RewardTempPileFactoryScript.new()
	return _temp_pile_factory


func _get_card_description_extractor():
	if _card_description_extractor == null:
		_card_description_extractor = RewardCardDescriptionExtractorScript.new()
	return _card_description_extractor


func _get_card_texture_extractor():
	if _card_texture_extractor == null:
		_card_texture_extractor = RewardCardTextureExtractorScript.new()
	return _card_texture_extractor


func _get_real_card_spawner():
	if _real_card_spawner == null:
		_real_card_spawner = RewardRealCardSpawnerScript.new()
	return _real_card_spawner


func _get_real_card_cleaner():
	if _real_card_cleaner == null:
		_real_card_cleaner = RewardRealCardCleanerScript.new()
	return _real_card_cleaner


func _get_draft_card_data_applier():
	if _draft_card_data_applier == null:
		_draft_card_data_applier = RewardDraftCardDataApplierScript.new()
	return _draft_card_data_applier


func _get_fly_to_deck_animator():
	if _fly_to_deck_animator == null:
		_fly_to_deck_animator = RewardCardFlyToDeckAnimatorScript.new()
	return _fly_to_deck_animator


func _get_deck_sync_bridge():
	if _deck_sync_bridge == null:
		_deck_sync_bridge = RewardDeckSyncBridgeScript.new()
	return _deck_sync_bridge


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false

## ==========================================
## ★ 核心生命周期方法
## ==========================================

func _ready():
	# 绑定按钮信号
	btn_back.pressed.connect(_on_back_pressed)
	btn_confirm.pressed.connect(_on_confirm_pressed)
	
	# 初始隐藏确认按钮（未选择卡牌时不可用）
	btn_confirm.disabled = true
	btn_back.disabled = false
	
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

	_setup_tooltip_presenter()

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
	btn_back.disabled = false

## 关闭场景
func close():
	# 隐藏场景
	self.hide()
	background_mask.hide()
	
	# 清理资源
	_close_current_selection()
	
	reward_scene_close_requested.emit(self)

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
	var temp_pile = _create_temp_pile()
	if temp_pile == null:
		return
	temp_pile.visible = false
	
	# 为每张卡创建 DraftCard 预览
	for card_id in selected_cards:
		await _create_draft_card(card_id, temp_pile)
	
	# 销毁幽灵牌堆
	temp_pile.queue_free()

## 创建单个 DraftCard 预览 (数据窃取)
func _create_draft_card(card_id: String, temp_pile: Node):
	# 创建商店专用轻量级 DraftCard
	var draft_card = _get_draft_card_factory().create_draft_card(draft_card_scene, card_id, card_display_size)
	
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
	# 安全检查：确保 temp_pile 仍然有效
	if not is_instance_valid(temp_pile):
		print("❌ _steal_card_data: temp_pile 已被释放，无法继续")
		return
	
	# 获取刚创建的真实卡牌
	var real_card = await _get_real_card_spawner().spawn_into_temp_pile(card_id, deck_manager.card_factory, temp_pile, self)
	if not is_instance_valid(temp_pile):
		print("❌ _steal_card_data: temp_pile 已被释放，无法继续")
		return
	if not real_card:
		print("警告: 无法为卡牌 %s 创建真实实例" % card_id)
		return
	
	# 1-3. 提取描述、关键词和真牌贴图
	await _get_draft_card_data_applier().apply_data(
		draft_card,
		real_card,
		card_id,
		Callable(self, "_extract_card_description"),
		Callable(self, "_extract_front_texture")
	)
	
	# 4. 从牌堆移除临时卡牌
	_get_real_card_cleaner().cleanup_real_card(temp_pile, real_card)

## ==========================================
## ★ 事件处理
## ==========================================

## 卡牌点击事件 (单选互斥)
func _on_draft_card_clicked(clicked_card: Control):
	# 再次点击已选中的卡牌时取消选择。
	# 这样奖励页回到“无操作”状态，退出按钮恢复可用，确认按钮重新禁用。
	if selected_draft_card == clicked_card:
		selected_draft_card = null
		for card in current_draft_cards:
			card.set_selected(false)
		btn_confirm.disabled = true
		btn_back.disabled = false
		return

	selected_draft_card = clicked_card
	
	# 更新所有卡牌的选中状态
	for card in current_draft_cards:
		card.set_selected(card == clicked_card)
	
	# 启用确认按钮
	btn_confirm.disabled = false
	btn_back.disabled = true

## 返回按钮
func _on_back_pressed():
	if btn_back.disabled:
		return
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
	# 假设抽牌堆在屏幕左下角偏下的位置
	var deck_target_pos = Vector2(100, get_viewport().get_visible_rect().size.y + 100)
	var card_id := str(card.card_id)
	_get_fly_to_deck_animator().play(
		self,
		card,
		deck_target_pos,
		fly_duration,
		trail_color,
		trail_width,
		func():
			_on_fly_to_deck_finished(card_id)
	)


func _on_fly_to_deck_finished(card_id: String) -> void:
	# ★ 核心数据打通：先写入全局牌组，再同步刷新当前局内抽牌堆。
	_get_deck_sync_bridge().add_card_and_sync(self, deck_manager, card_id)

	# 只有确认并完成数据写入后，才标记本次建筑奖励已被领取。
	set_meta("settlement_reward_committed", true)

	# 关闭场景
	close()

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
	# 优先从 /root 读取 autoload。
	if has_node("/root/GlobalClock"):
		var global_clock = get_node("/root/GlobalClock")
		if global_clock:
			if global_clock.has_method("get_current_era"):
				return global_clock.get_current_era()
			elif _object_has_property(global_clock, &"era"):
				return int(global_clock.get("era"))

	# 备用方案: 从场景中查找
	var root = Engine.get_main_loop().root
	var clock = root.find_child("GlobalClock", true, false)
	if clock:
		if clock.has_method("get_current_era"):
			return clock.get_current_era()
		elif _object_has_property(clock, &"era"):
			return int(clock.get("era"))
	
	# 默认值
	return 1


func _create_temp_pile() -> Pile:
	return _get_temp_pile_factory().create_temp_pile(deck_manager, "AcquireReward")


func _extract_front_texture(real_card: Node, card_id: String) -> Texture2D:
	return await _get_card_texture_extractor().extract_front_texture(real_card, card_id, deck_manager, self)


func _extract_card_description(real_card: Node) -> String:
	return await _get_card_description_extractor().extract_description(real_card, self)

## 尝试自动查找 CardManager 节点
func _try_find_card_manager():
	var card_manager = _get_card_manager_locator().find_card_manager(self, CardManager)
	if card_manager != null:
		deck_manager = card_manager
		print("✅ AcquireReward: 自动找到 CardManager: %s" % card_manager.name)
		return

	print("⚠️ AcquireReward: 未能自动找到 CardManager，需要手动调用 set_deck_manager()")

## ==========================================
## ★ 外部接口
## ==========================================

## 设置 deck_manager 引用 (必须由主场景调用)
func set_deck_manager(manager):
	deck_manager = manager
	print("AcquireReward: deck_manager 已设置")

## 初始化共享 Tooltip presenter。
func _setup_tooltip_presenter() -> void:
	tooltip_presenter = _get_tooltip_adapter().ensure_presenter(tooltip_presenter, self, tooltip_config)

# 显示tooltip
func show_tooltip(card: Control):
	tooltip_presenter = _get_tooltip_adapter().show_card_tooltip(tooltip_presenter, self, tooltip_config, card)

# 隐藏tooltip
func hide_tooltip(card: Control = null):
	_get_tooltip_adapter().hide_tooltip(tooltip_presenter)
