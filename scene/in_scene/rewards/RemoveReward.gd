# 文件名: RemoveReward.gd
# 功能: 删除卡牌奖励场景 - 基于"数据窃取"架构
# 设计原则: 模仿 ShopManager.gd 和 reward_manager.gd 的代码风格

extends CanvasLayer

signal reward_scene_close_requested(scene_instance: Node)

## 卡牌数据池单例引用
var CardDataPool = preload("res://scene/global/CardDataPool.gd")
## CardManager 类型引用 (用于类型检查)
var CardManager = preload("res://addons/card-framework/card_manager.gd")
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
const RemoveDeckCardSelectionPresenterScript = preload("res://scene/in_scene/rewards/presenters/RemoveDeckCardSelectionPresenter.gd")
const RemoveDeckDisplayCleanerScript = preload("res://scene/in_scene/rewards/presenters/RemoveDeckDisplayCleaner.gd")
const RemoveDeckCardRemovalProcessorScript = preload("res://scene/in_scene/rewards/rules/RemoveDeckCardRemovalProcessor.gd")
const RewardDeckCardIdProviderScript = preload("res://scene/in_scene/rewards/rules/RewardDeckCardIdProvider.gd")

## ==========================================
## ★ 节点引用 - 必须在场景中正确连接
## ==========================================

@onready var background_mask: Panel = $BackgroundMask
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
## 标题相对场景里 TitleLabel 初始位置的偏移。
## 如果导出版标题看起来偏右，可在检查器里把 x 调成负数，例如 Vector2(-80, 0)。
@export var title_offset: Vector2 = Vector2(-80.0, 0.0)

@export_group("卡牌排版配置")
@export var card_display_size: Vector2 = Vector2(125, 175)  # 动态控制生成的卡牌大小
@export var card_spacing_x: int = 20  # 卡牌水平间距
@export var card_spacing_y: int = 20  # 卡牌垂直间距

@export_group("Tooltip资源配置")
@export var tooltip_config: TooltipConfig = preload("res://scene/shared/tooltip/reward_card_tooltip_config.tres")

## 删除卡牌奖励页使用通用 Tooltip presenter。
var tooltip_presenter: CardTooltipPresenter = null
var _title_base_position: Vector2 = Vector2.ZERO
var _card_manager_locator = null
var _tooltip_adapter = null
var _draft_card_factory = null
var _temp_pile_factory = null
var _card_description_extractor = null
var _card_texture_extractor = null
var _real_card_spawner = null
var _real_card_cleaner = null
var _draft_card_data_applier = null
var _deck_sync_bridge = null
var _deck_card_selection_presenter = null
var _deck_display_cleaner = null
var _deck_card_removal_processor = null
var _deck_card_id_provider = null


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


func _get_deck_sync_bridge():
	if _deck_sync_bridge == null:
		_deck_sync_bridge = RewardDeckSyncBridgeScript.new()
	return _deck_sync_bridge


func _get_deck_card_selection_presenter():
	if _deck_card_selection_presenter == null:
		_deck_card_selection_presenter = RemoveDeckCardSelectionPresenterScript.new()
	return _deck_card_selection_presenter


func _get_deck_display_cleaner():
	if _deck_display_cleaner == null:
		_deck_display_cleaner = RemoveDeckDisplayCleanerScript.new()
	return _deck_display_cleaner


func _get_deck_card_removal_processor():
	if _deck_card_removal_processor == null:
		_deck_card_removal_processor = RemoveDeckCardRemovalProcessorScript.new()
	return _deck_card_removal_processor


func _get_deck_card_id_provider():
	if _deck_card_id_provider == null:
		_deck_card_id_provider = RewardDeckCardIdProviderScript.new()
	return _deck_card_id_provider


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
	if is_instance_valid(title_label):
		_title_base_position = title_label.position
		_apply_title_offset()

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
		title_label.text = "选择一张卡牌从牌组中移除"
		_apply_title_offset()
	
	# 配置牌网格
	deck_grid.columns = deck_grid_columns
	
	# 应用卡牌间距配置
	if deck_grid:
		deck_grid.add_theme_constant_override("h_separation", card_spacing_x)
		deck_grid.add_theme_constant_override("v_separation", card_spacing_y)
	
	if is_instance_valid(selected_card_display):
		selected_card_display.hide()

	_setup_tooltip_presenter()

## 打开删除卡牌场景
func open():
	# 显示场景
	self.show()
	background_mask.show()
	_apply_title_offset()
	
	# 重置状态
	_clear_deck_display()
	selected_draft_card = null
	btn_confirm.disabled = true
	
	# 生成牌组显示
	_generate_deck_display()
	if is_instance_valid(selected_card_display):
		selected_card_display.hide()
	
	# 显示UI元素
	deck_scroll_container.show()
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
	_clear_deck_display()
	
	reward_scene_close_requested.emit(self)

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
	var temp_pile = _create_temp_pile()
	if temp_pile == null:
		return
	temp_pile.visible = false
	
	# 为每张卡创建 DraftCard 预览
	for card_id in deck_card_ids:
		await _create_deck_card_display(card_id, temp_pile)
	
	# 销毁幽灵牌堆
	temp_pile.queue_free()

## 创建单个牌组卡牌显示
func _create_deck_card_display(card_id: String, temp_pile: Node):
	# 创建轻量级 DraftCard
	var draft_card = _get_draft_card_factory().create_draft_card(draft_card_scene, card_id, card_display_size)
	
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

## 牌组卡牌点击事件 (单选)
func _on_deck_card_clicked(clicked_card: Control):
	selected_draft_card = _get_deck_card_selection_presenter().apply_selection(
		clicked_card,
		selected_draft_card,
		current_deck_cards,
		btn_confirm,
		btn_back
	)

## 返回按钮
func _on_back_pressed():
	if btn_back.disabled:
		return
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
		
		# 确认删除后，本次建筑奖励已经被领取，随后直接返回收获界面。
		set_meta("settlement_reward_committed", true)
		close()
	)

## ==========================================
## ★ 核心数据操作
## ==========================================

## 从牌组中移除卡牌 (需要对接你的牌组管理系统)
func _remove_card_from_deck(card_id: String):
	_get_deck_card_removal_processor().remove_card_from_deck(deck_manager, card_id)
	_get_deck_sync_bridge().sync_runtime_deck(self, deck_manager)

## 获取当前牌组卡牌ID列表 (需要对接你的牌组管理系统)
func _get_current_deck_card_ids() -> Array[String]:
	return _get_deck_card_id_provider().get_current_deck_card_ids(deck_manager)

## ==========================================
## ★ 工具函数
## ==========================================

## 清空牌组显示
func _clear_deck_display():
	var clear_state: Dictionary = _get_deck_display_cleaner().clear_deck_display(
		current_deck_cards,
		original_deck_card_ids,
		deck_grid,
		selected_card_display
	)
	selected_draft_card = clear_state["selected_draft_card"] as Control


func _create_temp_pile() -> Pile:
	return _get_temp_pile_factory().create_temp_pile(deck_manager, "RemoveReward")


func _extract_front_texture(real_card: Node, card_id: String) -> Texture2D:
	return await _get_card_texture_extractor().extract_front_texture(real_card, card_id, deck_manager, self)


func _extract_card_description(real_card: Node) -> String:
	return await _get_card_description_extractor().extract_description(real_card, self)

## 尝试自动查找 CardManager 节点
func _try_find_card_manager():
	var card_manager = _get_card_manager_locator().find_card_manager(self, CardManager)
	if card_manager != null:
		deck_manager = card_manager
		print("✅ RemoveReward: 自动找到 CardManager: %s" % card_manager.name)
		return

	print("⚠️ RemoveReward: 未能自动找到 CardManager，需要手动调用 set_deck_manager()")


## 应用标题偏移。
## 核心逻辑：保留场景文件中的初始位置作为基准，只叠加导出的 title_offset，方便导出版微调。
func _apply_title_offset() -> void:
	if not is_instance_valid(title_label):
		return
	title_label.position = _title_base_position + title_offset

## ==========================================
## ★ 外部接口
## ==========================================

## 设置 deck_manager 引用 (必须由主场景调用)
func set_deck_manager(manager):
	deck_manager = manager
	print("RemoveReward: deck_manager 已设置")

## 初始化共享 Tooltip presenter。
func _setup_tooltip_presenter() -> void:
	tooltip_presenter = _get_tooltip_adapter().ensure_presenter(tooltip_presenter, self, tooltip_config)

# 显示tooltip
func show_tooltip(card: Control):
	tooltip_presenter = _get_tooltip_adapter().show_card_tooltip(tooltip_presenter, self, tooltip_config, card)

# 隐藏tooltip
func hide_tooltip(card: Control = null):
	_get_tooltip_adapter().hide_tooltip(tooltip_presenter)
