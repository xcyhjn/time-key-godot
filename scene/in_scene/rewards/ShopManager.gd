# 文件名: ShopManager.gd
# 功能: 时代商店管理系统 - 基于"数据窃取"与"飞入"架构构建
# 设计原则: 完全模仿 reward_manager.gd 的代码风格与架构模式

extends CanvasLayer

signal reward_scene_close_requested(scene_instance: Node)

## 卡牌数据池单例引用 (用于获取时代分类的卡牌)
var CardDataPool = preload("res://scene/global/CardDataPool.gd")
## CardManager 类型引用 (用于类型检查)
var CardManager = preload("res://addons/card-framework/card_manager.gd")
const ShopPricingPresenterScript = preload("res://scene/in_scene/rewards/presenters/ShopPricingPresenter.gd")
const ShopEraWeightSelectorScript = preload("res://scene/in_scene/rewards/rules/ShopEraWeightSelector.gd")
const ShopGlobalNodeFinderScript = preload("res://scene/in_scene/rewards/bridges/ShopGlobalNodeFinder.gd")
const RewardTooltipAdapterScript = preload("res://scene/in_scene/rewards/presenters/RewardTooltipAdapter.gd")
const RewardTempPileFactoryScript = preload("res://scene/in_scene/rewards/factory/RewardTempPileFactory.gd")
const RewardCardDescriptionExtractorScript = preload("res://scene/in_scene/rewards/presenters/RewardCardDescriptionExtractor.gd")
const RewardCardTextureExtractorScript = preload("res://scene/in_scene/rewards/presenters/RewardCardTextureExtractor.gd")
const RewardRealCardSpawnerScript = preload("res://scene/in_scene/rewards/factory/RewardRealCardSpawner.gd")

## ==========================================
## ★ 节点引用 - 必须在场景中正确连接
## ==========================================

@onready var shop_grid: GridContainer = $ShopGrid
@onready var btn_exit: Button = $Sidebar/BtnExit
@onready var btn_refresh: Button = $Sidebar/RefreshGroup/BtnRefresh
@onready var btn_upgrade: Button = $Sidebar/UpgradeGroup/BtnUpgrade
@onready var label_upgrade_cost: Label = $Sidebar/UpgradeGroup/LabelUpgradeCost
@onready var label_refresh_cost: Label = $Sidebar/RefreshGroup/LabelRefreshCost

## 外部依赖注入 (必须由主场景在 _ready 中赋值)
var deck_manager = null  # 必须提供 card_factory 访问
var draft_card_scene = preload("res://scene/card/DraftCard.tscn")

## ==========================================
## ★ 商店状态变量 - 记录刷新与升级次数
## ==========================================

var refresh_count: int = 0
var upgrade_count: int = 0
var local_era_offset: int = 0  # 商店本地时代偏移量
var _initialized: bool = false  # 标记是否已完成初始化
var _is_generating: bool = false  # 防止并发生成锁

## 当前商店中的卡牌列表 (DraftCard 实例)
var shop_cards: Array = []
## 卡牌与价格标签的映射关系
var card_price_map: Dictionary = {}  # key: DraftCard实例, value: 价格标签实例

## ==========================================
## ★ 导出参数 - 可在检查器中动态调整
## ==========================================

@export_group("商店定价策略")
@export var base_price: int = 50  # 基础价格
@export var price_increment: int = 25  # 价格步进增量
@export var refresh_base_cost: int = 50  # 刷新基础消耗
@export var upgrade_base_cost: int = 100  # 升级基础消耗

@export_group("商店布局配置")
@export var shop_slots_count: int = 8  # 商品位总数
@export var shop_columns: int = 4  # 每行列数

@export_group("飞入牌库特效 (完全模仿 reward_manager)")
@export var trail_color: Color = Color(1.0, 0.2, 0.2, 0.8)  # 红色拖影
@export var trail_width: float = 15.0
@export var fly_duration: float = 0.6

@export_group("时代权重算法配置")
@export var weight_current_era: float = 0.85    # 当前时代: 85%
@export var weight_previous_era: float = 0.05   # 前一个时代: 5%
@export var weight_next_era: float = 0.09       # 下一个时代: 9%
@export var weight_next_next_era: float = 0.01  # 下两个时代: 1%

@export_group("Tooltip资源配置")
@export var tooltip_config: TooltipConfig = preload("res://scene/shared/tooltip/reward_card_tooltip_config.tres")

@export_group("卡牌排版配置")
@export var card_display_size: Vector2 = Vector2(125, 175)  # 动态控制生成的卡牌大小
@export var card_spacing_x: int = 20  # 卡牌水平间距
@export var card_spacing_y: int = 20  # 卡牌垂直间距

## 商店页面只保留 Tooltip 触发职责，UI 构建与定位交给共享 presenter。
var tooltip_presenter: CardTooltipPresenter = null
var _pricing_presenter = null
var _era_weight_selector = null
var _global_node_finder = null
var _tooltip_adapter = null
var _temp_pile_factory = null
var _card_description_extractor = null
var _card_texture_extractor = null
var _real_card_spawner = null


func _get_pricing_presenter():
	if _pricing_presenter == null:
		_pricing_presenter = ShopPricingPresenterScript.new()
	return _pricing_presenter


func _get_era_weight_selector():
	if _era_weight_selector == null:
		_era_weight_selector = ShopEraWeightSelectorScript.new()
	return _era_weight_selector


func _get_global_node_finder():
	if _global_node_finder == null:
		_global_node_finder = ShopGlobalNodeFinderScript.new()
	return _global_node_finder


func _get_tooltip_adapter():
	if _tooltip_adapter == null:
		_tooltip_adapter = RewardTooltipAdapterScript.new()
	return _tooltip_adapter


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
	# 1. 绑定信号
	btn_exit.pressed.connect(_on_exit_pressed)
	btn_refresh.pressed.connect(_on_refresh_pressed)
	btn_upgrade.pressed.connect(_on_upgrade_pressed)
	
	# 2. 布局初始化
	shop_grid.columns = shop_columns
	
	# 应用卡牌间距配置
	if shop_grid:
		shop_grid.add_theme_constant_override("h_separation", card_spacing_x)
		shop_grid.add_theme_constant_override("v_separation", card_spacing_y)
	
	# ★ 调试输出：检查初始布局参数
	print("🛒 商店初始化 - shop_slots_count: %d, shop_columns: %d, 实际列数: %d" % [
		shop_slots_count, shop_columns, shop_grid.columns if shop_grid else -1
	])
	print("🛒 导出参数检查 - base_price: %d, price_increment: %d, refresh_base_cost: %d, upgrade_base_cost: %d" % [
		base_price, price_increment, refresh_base_cost, upgrade_base_cost
	])
	print("🛒 标签引用检查 - label_refresh_cost: %s, label_upgrade_cost: %s" % [
		"有效" if label_refresh_cost else "null",
		"有效" if label_upgrade_cost else "null"
	])
	
	# ★ 延迟一帧确保所有节点完成初始化
	await get_tree().process_frame
	
	# 3. 查找依赖
	if not deck_manager:
		_try_find_card_manager()
	
	# ⚠️ 注意：不要在这里调用 _generate_shop_items()
	# 除非你希望游戏一启动商店就在后台偷偷生成好了

	_setup_tooltip_presenter()
	
	_update_price_display()
	_initialized = true
	print("✅ 商店初始化完成")

## ==========================================
## ★ 商店生成逻辑 - 核心"数据窃取"架构
## ==========================================

## 生成商店商品 (模仿 _on_acquire_pressed 的幽灵牌堆机制)
func _generate_shop_items():
	if _is_generating: 
		print("🔄 商店生成已在进行中，跳过重复调用")
		return 
	_is_generating = true
	
	# ★ 调试输出：清理前的状态
	print("🔄 开始生成商店商品 - 清理前shop_grid子节点数: %d" % shop_grid.get_child_count())
	
	# 清空现有商品
	_clear_shop_items()
	
	# ★ 调试输出：清理后的状态
	print("🧹 清理完成 - shop_grid子节点数: %d" % shop_grid.get_child_count())
	
	# 检查必要依赖
	if not deck_manager or not deck_manager.card_factory:
		push_warning("ShopManager: deck_manager 或 card_factory 未设置! 商店将显示为空。请确保已调用 set_deck_manager() 或 CardManager 已在场景中。")
		_is_generating = false
		return
	
	# ★ 调试输出：检查布局参数
	print("🔍 商店生成参数 - shop_slots_count: %d, shop_columns: %d, 实际列数: %d" % [
		shop_slots_count, shop_columns, shop_grid.columns if shop_grid else -1
	])
	
	# ★ 核心步骤1: 获取当前时代值
	var current_era = _get_shop_era()
	print("商店生成 - 基础时代: %d (全局时代: %d + 本地偏移: %d)" % [
		current_era, _get_global_era(), local_era_offset
	])
	
	# 创建临时幽灵牌堆 (完全隐形)
	var temp_pile = _create_temp_pile()
	if temp_pile == null:
		_is_generating = false
		return
	temp_pile.visible = false
	
	# 生成所有商品位
	print("📊 开始生成商品位，总数: %d，当前循环索引: 0 到 %d" % [shop_slots_count, shop_slots_count - 1])
	for i in range(shop_slots_count):
		print("  🔸 生成商品位 %d/%d" % [i + 1, shop_slots_count])
		# ★ 核心步骤2: 根据时代权重随机选择卡牌ID
		var card_id = _select_card_by_era_weight(current_era)
		if card_id == "":
			print("警告: 无法为时代 %d 选择卡牌，使用默认卡牌" % current_era)
			card_id = "default_card"  # 回退默认值
		
		# 创建商店专用轻量级 DraftCard
		var shop_card = draft_card_scene.instantiate()
		shop_card.card_id = card_id
		
		# ★ 应用自定义尺寸设置
		shop_card.custom_set_size = card_display_size
		
		# ★ 核心步骤3: 数据窃取 - 从真实卡牌提取属性
		await _steal_card_data(card_id, shop_card, temp_pile)
		
		# 创建商品位容器 (VBox: 上方卡牌，下方价格标签)
		var slot_container = VBoxContainer.new()
		slot_container.alignment = BoxContainer.ALIGNMENT_CENTER
		
		# 添加卡牌到容器
		slot_container.add_child(shop_card)
		
		# 创建价格标签
		var price_label = Label.new()
		var price = _calculate_card_price(i)
		price_label.text = "%d 时间币" % price
		price_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		price_label.add_theme_font_size_override("font_size", 14)
		price_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.2, 1.0))  # 金色
		
		slot_container.add_child(price_label)
		
		# 添加到商店网格
		shop_grid.add_child(slot_container)
		
		# 记录映射关系
		shop_cards.append(shop_card)
		card_price_map[shop_card] = {"label": price_label, "price": price, "container": slot_container}
		
		# 绑定卡牌点击事件 (购买)
		shop_card.card_clicked.connect(_on_shop_card_clicked)
	
	# ★ 核心步骤4: 资源回收 - 销毁幽灵牌堆
	temp_pile.queue_free()
	
	# ★ 调试输出：生成完成后的状态
	print("✅ 商店商品生成完成 - 生成数量: %d, 实际shop_grid子节点数: %d, shop_cards记录数: %d" % [
		shop_slots_count, shop_grid.get_child_count(), shop_cards.size()
	])
	
	_is_generating = false # 释放锁

## 数据窃取核心函数 (模仿 reward_manager 中的提取逻辑)
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
	
	# 1. 提取卡牌描述文本
	draft_card.raw_description = await _extract_card_description(real_card)
	
	# 2. 提取关键词词条
	if _object_has_property(real_card, &"active_keywords") and real_card.get("active_keywords") is Array:
		draft_card.active_keywords = real_card.get("active_keywords").duplicate()
	
	# 3. 偷取真牌贴图 (从 FrontFace/TextureRect)
	var front_texture = await _extract_front_texture(real_card, card_id)
	if front_texture != null:
		draft_card.texture = front_texture
	
	# 4. 从牌堆移除临时卡牌 (防止堆积)
	temp_pile.remove_card(real_card)
	real_card.queue_free()

## 基于时代的权重选择卡牌ID
func _select_card_by_era_weight(base_era: int) -> String:
	var selected_era = _get_era_weight_selector().select_era(
		base_era,
		weight_previous_era,
		weight_current_era,
		weight_next_era,
		weight_next_next_era
	)

	if selected_era < 1:
		return ""

	print("权重选择 - 基础时代: %d, 选中时代: %d" % [base_era, selected_era])
	
	# ★ 这里需要对接你的卡牌数据系统
	# 假设有一个函数可以根据时代返回卡牌ID列表
	var era_card_pool = _get_cards_by_era(selected_era)
	if era_card_pool.size() == 0:
		return ""
	
	# 随机选择卡牌
	return era_card_pool[randi() % era_card_pool.size()]

## 清空商店商品
func _clear_shop_items():
	# 彻底清理 shop_grid 
	for child in shop_grid.get_children():
		if is_instance_valid(child):
			# ★ 核心修复：先从容器移除，让 GridContainer 立即重新计算排版 
			shop_grid.remove_child(child)
			# 再销毁，释放内存 
			child.queue_free()
	
	# 清理记录的数据 
	shop_cards.clear()
	card_price_map.clear()
	print("🧹 已执行物理清理，容器当前子节点数: %d" % shop_grid.get_child_count())

## ==========================================
## ★ 购买逻辑 - 核心"飞入"架构
## ==========================================

## 商店卡牌点击事件 (购买)
func _on_shop_card_clicked(clicked_card: Control):
	if not clicked_card in shop_cards:
		return
	
	var price_data = card_price_map.get(clicked_card)
	if not price_data:
		return
	
	var price = price_data.price
	
	# ★ 核心步骤1: 价格验证 - 调用 global_timecoin 消费时间币
	if not _consume_timecoins(price):
		print("购买失败: 时间币不足! 需要: %d, 当前余额不足" % price)
		# 可以添加视觉反馈，如卡牌抖动或红色闪烁
		return
	
	print("成功购买卡牌 %s, 价格: %d 时间币" % [clicked_card.card_id, price])
	
	# 移除价格标签
	price_data.label.queue_free()
	
	# ★ 核心步骤2: 剥离卡牌，准备飞入动画
	var slot_container = price_data.container
	var global_pos = clicked_card.global_position
	slot_container.remove_child(clicked_card)
	self.add_child(clicked_card)
	clicked_card.global_position = global_pos
	
	# ★ 核心步骤2.5: 移除空容器（避免刷新时布局混乱）
	if is_instance_valid(slot_container) and slot_container.get_parent():
		slot_container.get_parent().remove_child(slot_container)
	slot_container.queue_free()
	
	# ★ 核心步骤3: 红色拖影特效 (完全模仿 reward_manager)
	_fly_to_deck_pile(clicked_card)

## 飞入牌库动画 (模仿 _on_confirm_pressed)
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
	
	# 2. 飞行 Tween 动画 (飞向左下角抽牌堆)
	var deck_target_pos = Vector2(100, get_viewport().get_visible_rect().size.y + 100)
	var tw = create_tween().set_parallel(true).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
	
	tw.tween_property(card, "global_position", deck_target_pos, fly_duration)
	tw.tween_property(card, "scale", Vector2.ZERO, fly_duration)
	tw.tween_property(card, "rotation", PI * 2, fly_duration)  # 炫酷旋转
	
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
		
		# ★ 核心数据打通：先写入全局牌组，再同步刷新当前局内抽牌堆。
		if deck_manager != null and deck_manager.has_method("add_card_to_deck"):
			deck_manager.add_card_to_deck(card.card_id)

		var main = get_tree().get_first_node_in_group("MainBoard")
		if (
			deck_manager != null
			and deck_manager.has_method("sync_runtime_deck_from_global")
			and main
			and main.deck_pile
		):
			deck_manager.sync_runtime_deck_from_global(main.deck_pile)
			if main.has_method("update_counts_and_ui"):
				main.update_counts_and_ui()
			print("✅ 已成功将 %s 加入全局牌组并同步抽牌堆！" % card.card_id)
		
		# 从商店列表中移除
		var idx = shop_cards.find(card)
		if idx != -1:
			shop_cards.remove_at(idx)
			card_price_map.erase(card)
	)

## ==========================================
## ★ 商店管理操作
## ==========================================

## 刷新商店
func _on_refresh_pressed():
	var refresh_cost = refresh_base_cost + (refresh_count * price_increment)
	
	# 验证时间币
	if not _consume_timecoins(refresh_cost):
		print("刷新失败: 时间币不足! 需要: %d" % refresh_cost)
		return
	
	refresh_count += 1
	print("商店刷新成功! 消耗: %d 时间币, 刷新次数: %d" % [refresh_cost, refresh_count])
	
	# 重新生成商品
	_generate_shop_items()
	_update_price_display()

## 升级时代
func _on_upgrade_pressed():
	var upgrade_cost = upgrade_base_cost + (upgrade_count * price_increment)
	
	# 验证时间币
	if not _consume_timecoins(upgrade_cost):
		print("升级失败: 时间币不足! 需要: %d" % upgrade_cost)
		return
	
	upgrade_count += 1
	local_era_offset += 1
	
	print("时代升级成功! 消耗: %d 时间币, 升级次数: %d, 本地时代偏移: %d" % [
		upgrade_cost, upgrade_count, local_era_offset
	])
	
	# 免费刷新商店
	_generate_shop_items()
	_update_price_display()

## 离开商店
func _on_exit_pressed():
	print("离开商店场景...")
	
	# ★ 重要: 重置商店状态 (确保下次进入时数据清零)
	refresh_count = 0
	upgrade_count = 0
	# 注意: local_era_offset 是否重置取决于设计需求
	# 这里不重置 local_era_offset，保持升级效果
	
	# 隐藏商店界面
	self.hide()
	
	reward_scene_close_requested.emit(self)

## ==========================================
## ★ 工具函数
## ==========================================

## 获取商店基础时代: x = global_clock.era + local_era_offset
func _get_shop_era() -> int:
	return _get_global_era() + local_era_offset

## 获取全局时代值 (通过 global_clock 单例)
func _get_global_era() -> int:
	# 尝试查找 global_clock 单例
	var clock = _find_global_clock()
	if clock:
		return clock.era
	
	# 回退值 (如果找不到 global_clock)
	print("警告: 无法找到 global_clock 单例，使用默认时代值 1")
	return 1

## 消费时间币 (通过 global_timecoin 单例)
func _consume_timecoins(amount: int) -> bool:
	# 尝试查找 global_timecoin 单例
	var timecoin = _find_global_timecoin()
	if timecoin:
		return timecoin.consume_timecoins(amount)
	
	# 回退逻辑 (如果找不到单例，总是返回成功用于测试)
	print("警告: 无法找到 global_timecoin 单例，跳过消费验证")
	return true

## 根据卡牌位置计算价格
func _calculate_card_price(slot_index: int) -> int:
	return _get_pricing_presenter().calculate_card_price(slot_index, base_price)

## 更新侧边栏价格显示
func _update_price_display():
	var refresh_cost = _get_pricing_presenter().calculate_refresh_cost(refresh_base_cost, refresh_count, price_increment)
	var upgrade_cost = _get_pricing_presenter().calculate_upgrade_cost(upgrade_base_cost, upgrade_count, price_increment)
	
	# 调试输出：显示详细价格信息
	print("💰 价格更新 - 刷新: %d (基数: %d + 计数: %d × 增量: %d), 升级: %d (基数: %d + 计数: %d × 增量: %d)" % [
		refresh_cost, refresh_base_cost, refresh_count, price_increment,
		upgrade_cost, upgrade_base_cost, upgrade_count, price_increment
	])
	
	# 更新 UI 标签显示当前价格
	_get_pricing_presenter().update_price_labels(label_refresh_cost, label_upgrade_cost, refresh_cost, upgrade_cost)
	
	# ✅ 标签位置已修正：label_refresh_cost 在 RefreshGroup 中，label_upgrade_cost 在 UpgradeGroup 中

## 根据时代获取卡牌ID列表 (对接 CardDataPool 系统)
func _get_cards_by_era(era: int) -> Array[String]:
	# ★ 重要: 使用 CardDataPool 单例获取卡牌ID列表
	# CardDataPool 会自动跳过缺少时代和id的卡牌
	var card_data_pool = CardDataPool.get_instance()
	if card_data_pool:
		var cards = card_data_pool.get_cards_by_era(era)
		print("🃏 ShopManager: 从 CardDataPool 获取时代 %d 的卡牌，共 %d 张" % [era, cards.size()])
		return cards
	else:
		push_error("❌ ShopManager: 无法获取 CardDataPool 单例")
		# 备用方案: 返回硬编码的卡牌列表
		var era_pools = {
			1: ["1"],
			2: ["2"],
			3: ["3"],
			4: [""]
		}
		return era_pools.get(era, [])

## ==========================================
## ★ 单例查找函数 (模仿 TimelineManager 中的查找逻辑)
## ==========================================

## 查找 global_clock 单例
func _find_global_clock() -> Node:
	return _get_global_node_finder().find_global_clock(self)

## 查找 global_timecoin 单例
func _find_global_timecoin() -> Node:
	return _get_global_node_finder().find_global_timecoin(self)

## 递归查找包含指定脚本的节点 (模仿 TimelineManager)
func _find_node_with_script_recursive(root: Node, script_name: String) -> Node:
	return _get_global_node_finder().find_node_with_script_recursive(root, script_name)

## ==========================================
## ★ 公共接口方法
## ==========================================

## 打开商店界面 (模仿 reward_manager 的 open_reward_screen)
func open_shop():
	self.show()
	
	# 重置UI状态
	_generate_shop_items()
	_update_price_display()
	
	print("商店已打开 - 全局时代: %d, 本地偏移: %d" % [_get_global_era(), local_era_offset])

## 尝试自动查找 CardManager 节点
func _try_find_card_manager():
	# 方案1: 通过元数据查找 (CardManager 在 _ready() 中将自己注册到场景根)
	var tree_root = get_tree().root
	print("🔍 ShopManager: 开始查找 CardManager，树根: %s" % tree_root.name if tree_root else "null")
	
	if tree_root and tree_root.has_meta("card_manager"):
		var card_manager = tree_root.get_meta("card_manager")
		print("🔍 ShopManager: 找到元数据，值类型: %s" % str(card_manager.get_class()) if card_manager else "null")
		if card_manager is CardManager:
			deck_manager = card_manager
			print("✅ ShopManager: 通过场景根元数据找到 CardManager: %s" % card_manager.name)
			# 检查 card_factory
			if deck_manager.card_factory:
				print("✅ ShopManager: CardManager 的 card_factory 已准备")
			else:
				print("⚠️ ShopManager: CardManager 的 card_factory 未初始化")
			return
		else:
			print("❌ ShopManager: 元数据中的对象不是 CardManager 类型: %s" % str(card_manager.get_class()) if card_manager else "null")
	else:
		print("🔍 ShopManager: 树根元数据中没有 card_manager")
	
	# 方案2: 通过当前场景元数据查找
	var scene_root = get_tree().current_scene
	print("🔍 ShopManager: 当前场景: %s" % scene_root.name if scene_root else "null")
	if scene_root and scene_root.has_meta("card_manager"):
		var card_manager = scene_root.get_meta("card_manager")
		if card_manager is CardManager:
			deck_manager = card_manager
			print("✅ ShopManager: 通过当前场景元数据找到 CardManager: %s" % card_manager.name)
			return
	
	# 方案3: 通过父节点链查找 (如果 ShopManager 是 CardManager 的子节点)
	var parent = get_parent()
	var parent_chain = []
	while parent:
		parent_chain.append(parent.name)
		if parent is CardManager:
			deck_manager = parent
			print("✅ ShopManager: 通过父节点链找到 CardManager: %s" % parent.name)
			return
		parent = parent.get_parent()
	print("🔍 ShopManager: 父节点链: %s" % str(parent_chain))
	
	# 方案4: 通过节点名查找 (回退方案)
	var scene_root_node = get_tree().root
	var card_manager_node = scene_root_node.find_child("CardManager", true, false)
	if card_manager_node and card_manager_node is CardManager:
		deck_manager = card_manager_node
		print("✅ ShopManager: 通过节点名找到 CardManager: %s" % card_manager_node.name)
		return
	else:
		print("🔍 ShopManager: 通过节点名未找到 CardManager")
	
	# 方案5: 遍历所有节点查找
	print("🔍 ShopManager: 开始遍历所有节点查找 CardManager...")
	var all_nodes = scene_root_node.get_children()
	for node in all_nodes:
		if node is CardManager:
			deck_manager = node
			print("✅ ShopManager: 通过遍历找到 CardManager: %s" % node.name)
			return
	
	print("⚠️ ShopManager: 未能自动找到 CardManager，需要手动调用 set_deck_manager()")

## 设置 deck_manager 引用 (必须由主场景调用)
func set_deck_manager(manager):
	deck_manager = manager
	print("ShopManager: deck_manager 已设置")


func _create_temp_pile() -> Pile:
	return _get_temp_pile_factory().create_temp_pile(deck_manager, "ShopManager")


func _extract_front_texture(real_card: Node, card_id: String) -> Texture2D:
	return await _get_card_texture_extractor().extract_front_texture(real_card, card_id, deck_manager, self)


func _extract_card_description(real_card: Node) -> String:
	return await _get_card_description_extractor().extract_description(real_card, self)

## 初始化共享 Tooltip presenter。
## 商店场景不展示关键词列，因此第三个参数固定为 false。
func _setup_tooltip_presenter() -> void:
	tooltip_presenter = _get_tooltip_adapter().ensure_presenter(tooltip_presenter, self, tooltip_config)

# 显示tooltip (简化版，只显示效果文本)
func show_tooltip(card: Control):
	tooltip_presenter = _get_tooltip_adapter().show_card_tooltip(tooltip_presenter, self, tooltip_config, card)

# 隐藏tooltip
func hide_tooltip(card: Control = null):
	_get_tooltip_adapter().hide_tooltip(tooltip_presenter)
