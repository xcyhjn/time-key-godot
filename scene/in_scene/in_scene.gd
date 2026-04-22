extends Control

# 预加载资源
var hand_scene = load("res://addons/card-framework/hand.tscn")
var pile_scene = load("res://addons/card-framework/pile.tscn")
# 请确保这里路径正确，必须指向你真实的卡牌场景
var card_scene_ref = load("res://scene/card/custom_card.tscn")
var pile_viewer_scene = preload("res://scene/pile/pile_viewer.tscn")

# 弃牌堆显示
var player_hand: Hand
var deck_pile: Pile
var discard_pile: Pile
var manager_instance: CardManager
var pile_viewer

# 卡组数量
var current_deck_count: int = 0
var current_discard_count: int = 0
# ★ 新增修复：抽牌与洗牌的并发锁，防止多重点击打断 await 逻辑
var is_processing_deck: bool = false
# ==========================================
# ★ 新增修复：补充缺失的全局状态变量
# ==========================================
# 用于记录当前游戏所处的时代阶段，回合结束时递增
var current_era_value: int = 1

# 游戏配置
var drop_area = 4.0 / 7.0
@export var pulse_shader: Shader  # 在检查器中把你的呼吸 Shader 拖进来

@export_group("主桌面排版 (Board Layout)")
@export var card_size: Vector2 = Vector2(125.0, 175.0)  # 卡牌尺寸
@export var hand_y_offset: float = 120.0  # 手牌区域下沉量 (默认 0.7 比例基础上的像素下移)
@export var hand_x_position_ratio: float = 0.5  # 手牌水平位置比例 (0.0=左, 0.5=中, 1.0=右)
@export var hand_y_position_ratio: float = 0.7  # 手牌垂直位置比例 (0.0=上, 0.7=下)
@export var hand_spacing: float = 20.0  # 手牌间距

# UI 节点引用
@onready var deck_button = $"../DeckButton"
@onready var discard_button = $"../DiscardButton"
@onready var deck_count_label = $"../DeckButton/Label"
@onready var discard_count_label = $"../DiscardButton/Label"

# ★ 新增：局外收获按钮引用
@onready var shop_button = $"../ShopButton"
@onready var acquire_reward_button = $"../AcquireRewardButton"
@onready var remove_reward_button = $"../RemoveRewardButton"
@onready var craft_reward_button = $"../CraftRewardButton"
@onready var lose_button = $"../LoseButton"
@onready var game_over_ui = $"../GameOver"
# ★ 新增：地图和时间币显示引用
@onready var hex_map = $"../../map/HexMap"
@onready var timecoin_container = get_node_or_null("/root/in_scene/TimecoinView/TimecoinCanvasLayer/TimecoinContainer")

# ★ 新增：回合按钮引用
@onready var start_turn_button = $"../StartTurnButton"
@onready var end_turn_button = $"../EndTurnButton"
@onready var end_combat_button = $"../EndCombatButton"  # 根据你的实际路径修改
@onready var cursor_tooltip = $"../CursorTooltip"  # 指向刚才创建的 Label
var cursor_tooltip_panel: PanelContainer  # 增强后的PanelContainer包装
@onready var timeline_ui = $"../TimelineUI"  # 根据你的实际路径修改
@onready var timeline_manager = $"../TimelineSystem/TimelineManager"
@onready var dim = $"../DimMenu"
@onready var win = $"../GameWinScreen"

# ================================
# ★ 导出调整项：词条 UI 外观
# ================================
@export_group("词条UI调整栏 (Tooltip)")
@export var tooltip_width: int = 220  # 词条框宽度
@export var tooltip_spacing: int = 8  # 多个框之间的上下间距
@export var tooltip_bottom_margin: int = 2  # 距离屏幕底部的安全距离
@export var tooltip_border_width: int = 2  # 边框粗细
@export var tooltip_border_color: Color = Color(0.8, 0.6, 0.2, 1.0)
@export var tooltip_bg_color: Color = Color(0.12, 0.12, 0.12, 0.95)
@export var tooltip_title_size: int = 18
@export var tooltip_desc_size: int = 14
@export var tooltip_desc_color: Color = Color(0.9, 0.9, 0.9, 1.0)
@export var tooltip_effect_font_size: int = 16  # 卡牌效果文本字体大小
@export var tooltip_effect_font_color: Color = Color(0.95, 0.95, 0.95, 1.0)  # 卡牌效果文本颜色
@export var tooltip_effect_font: Font  # 卡牌效果文本字体（可选）

# ================================
# ★ 导出调整项：悬浮 UI 排版与分列机制
# ================================
@export_group("悬浮信息枢纽 (Tooltip Layout)")
@export var tooltip_offset_x: int = 15  # 效果框与卡牌的 X 轴间距
@export var tooltip_offset_y: int = 0  # 效果框与卡牌顶部的 Y 轴偏移 (0 代表完美齐平)
@export var tooltip_gap_x: int = 15  # 效果框与右侧词条列、列与列之间的横向间距
@export var effect_panel_width: int = 240  # 主效果框的宽度
@export var max_keywords_per_column: int = 3  # 单列最多显示的词条数

# ★ 拆分为两个完全独立的顶级容器
var effect_tooltip_panel: PanelContainer
var effect_label: RichTextLabel
var keywords_tooltip_hbox: HBoxContainer
var current_hovered_card: Control = null
# ★ 新版：多重堆叠词条 UI 容器(增加卡牌文本版本)
var tooltip_vbox: VBoxContainer  # 改为纵向容器
# ★ 新增：用于记录当前正在显示词条的卡牌，防止异步显示“幽灵词条”
var current_tooltip_card: Control = null
var tooltip_ui_initialized = false


func _ready() -> void:
	# 初始化游戏状态
	current_era_value = 1
	
	add_to_group("MainBoard")  # 注册进群组，让卡牌能够呼叫此脚本

	# 输入验证：检查必要的导出变量
	# pulse_shader是可选的，如果未配置则跳过着色器效果
	#if not is_instance_valid(pulse_shader):
	#	push_warning("project.gd: pulse_shader 未在检查器中配置！")

	# 1. 先实例化查看器，避免后面调用报错
	if is_instance_valid(pile_viewer_scene):
		pile_viewer = pile_viewer_scene.instantiate()
		if is_instance_valid(pile_viewer):
			add_child(pile_viewer)
			pile_viewer.hide()
		else:
			push_error("project.gd: 无法实例化 pile_viewer_scene！")
	else:
		push_error("project.gd: pile_viewer_scene 加载失败！")

	# 2. ★ 新增：构建并初始化词条 UI (杀戮尖塔风格)
	setup_tooltip_ui()

	# 3. 初始化卡牌系统
	setup_card_system()
	if is_instance_valid(end_combat_button):
		end_combat_button.pressed.connect(_on_end_combat_pressed)

	## 将卡组管理器引用传给奖励界面，方便后续删牌/加牌操作
	#if is_instance_valid(reward_manager) and is_instance_valid(manager_instance):
		#reward_manager.deck_manager = manager_instance
	#else:
		#push_warning("project.gd: reward_manager 或 manager_instance 无效，无法设置 deck_manager")

	if is_instance_valid(timeline_manager):
		if timeline_manager.has_signal("action_hovered_changed"):
			timeline_manager.action_hovered_changed.connect(_on_timeline_action_hovered)
		else:
			push_warning("project.gd: timeline_manager 没有 action_hovered_changed 信号！")
	else:
		push_warning("project.gd: timeline_manager 无效！")
		# ★ 新增：将结算信号直接转交给 EffectProcessor
		var effect_processor = $TimelineSystem/EffectProcessor
		if is_instance_valid(effect_processor):
			if not timeline_manager.action_executed.is_connected(effect_processor.execute_action):
				timeline_manager.action_executed.connect(effect_processor.execute_action)
	
	# ★ 游戏开始时生成敌人意图（第一次生成敌人时）
	# 延迟一帧调用，确保所有敌人都已初始化并添加到Enemies组
	call_deferred("_generate_initial_enemy_intents")
	
	# ★ 增强光标提示框样式，模仿卡牌文本框
	call_deferred("_enhance_cursor_tooltip")
	
	# 初始化时间轴UI引用
	if timeline_ui:
		GameLogger.info("TimelineUI已找到: " + timeline_ui.name, "project")
	
	# 2. 连接失败按钮点击信号
	if is_instance_valid(lose_button):
		lose_button.pressed.connect(_on_lose_button_pressed)
	
	# 3. 监听全局失败信号
	if Signal_Bus:
		Signal_Bus.defeat_triggered.connect(_on_defeat_triggered)
	

## 游戏开始时生成初始敌人意图
## 确保在第一次生成敌人时也在时间轴上部署意图
func _generate_initial_enemy_intents() -> void:
	if not is_instance_valid(timeline_manager):
		GameLogger.warning("timeline_manager 无效，无法生成初始敌人意图", "Project")
		return
	
	# 清除任何可能的残留占用
	if timeline_manager.has_method("clear_grid"):
		timeline_manager.clear_grid()
	
	var all_enemies = get_tree().get_nodes_in_group("Enemies")
	GameLogger.debug("游戏初始化：找到 %d 个敌人，生成初始意图" % all_enemies.size(), "Project")
	
	if all_enemies.is_empty():
		GameLogger.debug("游戏开始时没有敌人，跳过初始意图生成", "Project")
		return
	
	if timeline_manager.has_method("generate_enemy_intents"):
		timeline_manager.generate_enemy_intents(all_enemies)
		GameLogger.info("✅ 游戏开始时已生成敌人意图", "Project")
		# 调试：打印网格状态
		if timeline_manager.has_method("debug_print_grid"):
			timeline_manager.debug_print_grid()
	else:
		GameLogger.warning("timeline_manager 没有 generate_enemy_intents 方法", "Project")


func _on_timeline_action_hovered(action: TimelineAction, is_hovering: bool):
	if is_hovering:
		# ★ 修复：读取你 JSON 里设定的 "效果" 字段
		var effect_text = action.action_data.get("效果", "发动未知技能！")

		# 鼠标进入方格
		if action.type == TimelineAction.Type.ENEMY:
			# 敌人行动：红色高亮敌人本身和目标地块
			_apply_pulse_shader(action.source_node, Color(1.0, 0.0, 0.0, 1.0))
			_apply_pulse_shader(action.target_tile, Color(1.0, 0.0, 0.0, 1.0))

			cursor_tooltip.text = "敌方意图: " + effect_text
			cursor_tooltip.add_theme_color_override("font_color", Color.RED)

		elif action.type == TimelineAction.Type.PLAYER:
			# 玩家卡牌：金色高亮目标地块
			_apply_pulse_shader(action.target_tile, Color(1.0, 0.8, 0.0, 1.0))

			# ★ 新增修复：完成 TODO，显示玩家卡牌即将造成的效果
			cursor_tooltip.text = "卡牌效果: " + effect_text
			cursor_tooltip.add_theme_color_override("font_color", Color.GOLD)

		# ★ 修复：设置提示框位置为鼠标位置 + 偏移
		set_cursor_tooltip_position(get_global_mouse_position() + Vector2(20, -30))
		cursor_tooltip.show()
	else:
		# 鼠标离开方格，清除高亮并隐藏浮窗
		_clear_pulse_shader(action.source_node)
		_clear_pulse_shader(action.target_tile)
		cursor_tooltip.hide()


# ==========================================
# 辅助函数：安全地挂载和卸载材质
# ==========================================
func _apply_pulse_shader(target_node: Node, color: Color):
	if not is_instance_valid(target_node): return
	if not is_instance_valid(pulse_shader): return

	# 假设你的地块/敌人有一个名叫 Sprite2D 的主要视觉节点
	var sprite = target_node.get_node_or_null("Sprite2D")
	if sprite:
		# 使用 Duplicate 防止一亮全亮
		sprite.material = pulse_shader.duplicate()
		# 传入颜色参数给 Shader
		sprite.material.set_shader_parameter("pulse_color", color)


func _clear_pulse_shader(target_node: Node):
	if not is_instance_valid(target_node): return

	var sprite = target_node.get_node_or_null("Sprite2D")
	if sprite:
		# 恢复为普通状态（如果是地块，记得恢复成原有的悬浮 Shader，而不是直接清空为 null）
		# sprite.material = original_hex_material
		sprite.material = null


func setup_card_system():
	var screen_size = get_viewport_rect().size

	# 验证场景是否加载成功
	var manager_scene = load("res://addons/card-framework/card_manager.tscn")
	if not is_instance_valid(manager_scene):
		push_error("project.gd: 无法加载 card_manager.tscn！")
		return

	manager_instance = manager_scene.instantiate() as CardManager
	if not is_instance_valid(manager_instance):
		push_error("project.gd: 无法实例化 CardManager！")
		return

	var card_factory_scene = load("res://addons/card-framework/card_factory.tscn")
	if is_instance_valid(card_factory_scene):
		manager_instance.card_factory_scene = card_factory_scene
	else:
		push_warning("project.gd: card_factory.tscn 加载失败！")

	manager_instance.debug_mode = true
	add_child(manager_instance)

	# --- 【修复重点】必须告诉工厂卡牌长什么样 ---
	# 如果不加这几行，工厂生产出来的卡是空的，或者导致空指针报错
	await get_tree().process_frame  # 等待工厂节点就绪
	if is_instance_valid(manager_instance.card_factory) and is_instance_valid(card_scene_ref):
		manager_instance.card_factory.default_card_scene = card_scene_ref
	else:
		push_error("project.gd: card_factory 或 card_scene_ref 无效！")

	# --- 1. 实例化手牌区 ---
	if is_instance_valid(hand_scene):
		player_hand = hand_scene.instantiate() as Hand
		if is_instance_valid(player_hand):
			player_hand.name = "PlayerHand"  # ★ 强制命名
			add_child(player_hand)
			player_hand.position = Vector2(screen_size.x * hand_x_position_ratio - card_size.x / 2, screen_size.y * hand_y_position_ratio + hand_y_offset)
		else:
			push_error("project.gd: 无法实例化 Hand！")
	else:
		push_error("project.gd: hand_scene 无效！")

	# --- 2. 实例化抽牌区 (逻辑实体-屏幕外) ---
	if is_instance_valid(pile_scene):
		deck_pile = pile_scene.instantiate() as Pile
		if is_instance_valid(deck_pile):
			deck_pile.name = "DeckPile"  # ★ 强制命名
			add_child(deck_pile)
			deck_pile.position = Vector2(0, 400)
			deck_pile.visible = false
		else:
			push_error("project.gd: 无法实例化 deck_pile！")
	else:
		push_error("project.gd: pile_scene 无效！")

	# --- 3. 实例化弃牌区 (逻辑实体-屏幕外) ---
	if is_instance_valid(pile_scene):
		discard_pile = pile_scene.instantiate() as Pile
		if is_instance_valid(discard_pile):
			discard_pile.name = "DiscardPile"  # ★ 强制命名
			add_child(discard_pile)
			discard_pile.add_to_group("DiscardPile")  # 添加到组，便于全局查找
			discard_pile.position = Vector2(2000, 400)
			discard_pile.visible = false
		else:
			push_error("project.gd: 无法实例化 discard_pile！")

	# --- 4. 生成初始卡牌 (从全局动态牌组生成) ---
	if is_instance_valid(manager_instance.card_factory) and is_instance_valid(deck_pile):
		# ★ 替换原本的硬编码 for i in range(10)
		for card_id in GlobalDB.player_deck:
			manager_instance.card_factory.create_card(card_id, deck_pile)

		if deck_pile._held_cards.size() > 0:
			deck_pile._held_cards.shuffle()
	else:
		push_error("project.gd: 无法生成初始卡牌，card_factory 或 deck_pile 无效！")

	update_counts_and_ui()

	# --- 5. 连接 UI 按钮点击事件 ---
	# 先断开可能的旧连接，防止重复触发
	if is_instance_valid(discard_button):
		if discard_button.pressed.is_connected(_on_discard_button_pressed):
			discard_button.pressed.disconnect(_on_discard_button_pressed)
		discard_button.pressed.connect(_on_discard_button_pressed)

	if is_instance_valid(deck_button):
		if deck_button.gui_input.is_connected(_on_deck_button_gui_input):
			deck_button.gui_input.disconnect(_on_deck_button_gui_input)
		deck_button.gui_input.connect(_on_deck_button_gui_input)

	# ★ 新增：绑定回合控制按钮
	if is_instance_valid(start_turn_button):
		start_turn_button.pressed.connect(_on_start_turn_pressed)
	if is_instance_valid(end_turn_button):
		end_turn_button.pressed.connect(_on_end_turn_pressed)
	
	# ★ 新增：绑定局外收获按钮
	if is_instance_valid(shop_button):
		shop_button.pressed.connect(_on_shop_button_pressed)
	if is_instance_valid(acquire_reward_button):
		acquire_reward_button.pressed.connect(_on_acquire_reward_button_pressed)
	if is_instance_valid(remove_reward_button):
		remove_reward_button.pressed.connect(_on_remove_reward_button_pressed)
	if is_instance_valid(craft_reward_button):
		craft_reward_button.pressed.connect(_on_craft_reward_button_pressed)


# --- 按钮逻辑修正 ---
func update_counts_and_ui():
	# 1. 从逻辑实体获取最新数据
	if is_instance_valid(deck_pile) and deck_pile._held_cards != null:
		current_deck_count = deck_pile._held_cards.size()
	else:
		current_deck_count = 0

	if is_instance_valid(discard_pile) and discard_pile._held_cards != null:
		current_discard_count = discard_pile._held_cards.size()
	else:
		current_discard_count = 0

	# 2. 更新 UI 标签
	if is_instance_valid(deck_count_label):
		deck_count_label.text = str(current_deck_count)
	if is_instance_valid(discard_count_label):
		discard_count_label.text = str(current_discard_count)
	GameLogger.info("UI 更新完毕 | 抽牌堆: %d | 弃牌堆: %d" % [current_deck_count, current_discard_count], "Project")


# --- 新增：抽牌堆的输入处理 (左键抽牌，右键查看) ---
func _on_deck_button_gui_input(event: InputEvent):
	# ★ 第一道防线：如果正在洗牌或抽牌，屏蔽此按钮的所有点击
	if is_processing_deck:
		return
		
	# 检查是否是鼠标按键事件
	if event is InputEventMouseButton and event.pressed:
		# --- 左键点击：抽牌 ---
		if event.button_index == MOUSE_BUTTON_LEFT:
			GameLogger.debug("左键点击抽牌堆：尝试抽牌...", "Project")
			attempt_draw_cards(3)  # 这里填你想要的抽牌数量

		# --- 右键点击：查看抽牌堆 ---
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			GameLogger.debug("右键点击抽牌堆：查看剩余卡牌", "Project")
			if deck_pile._held_cards.size() > 0:
				pile_viewer.open_pile_view(deck_pile, manager_instance)
			else:
				GameLogger.warning("抽牌堆是空的，没东西看！", "Project")

func _on_discard_button_pressed():
	# 点击弃牌堆按钮 -> 查看弃牌堆
	GameLogger.debug("查看弃牌堆", "Project")
	if discard_pile._held_cards.size() > 0:
		pile_viewer.open_pile_view(discard_pile, manager_instance)
	else:
		GameLogger.warning("弃牌堆是空的", "Project")

# ==========================================
# ★ 回合流程控制区
# ==========================================


func _on_start_turn_pressed():
	GameLogger.info("⚔️ 回合开始！", "Project")

	# 1. 切换按钮状态（防止连按）
	start_turn_button.disabled = true
	end_turn_button.disabled = false

	# 2. 预设效果：回合开始抽 5 张牌
	# 这里调用你之前写好的强力抽牌函数，它会自动处理洗牌！
	attempt_draw_cards(5)

	# 3. 在第一回合也生成敌人意图
	var all_enemies = get_tree().get_nodes_in_group("Enemies")
	if is_instance_valid(timeline_manager) and timeline_manager.has_method("generate_enemy_intents"):
		timeline_manager.generate_enemy_intents(all_enemies)

	# 你还可以在这里触发：地块上敌人的中毒掉血、技能冷却减少等逻辑

# 在 in_scene.gd 中，找到 _on_end_turn_pressed() 并替换：

func _on_end_turn_pressed():
	GameLogger.info("⏳ 玩家点击回合结束，开始时间轴结算...", "Project")

	# 禁用 UI，防止结算期间玩家乱点
	disable_player_inputs()

	# 1. 弃置所有手牌
	discard_all_hand_cards()

	# 2. ★ 核心改动：加上 await！等待时间轴的每一列特效和扣血逐步播放完毕
	await timeline_manager.resolve_timeline()

	# 3. 触发建筑行为（建筑扩张等）
	Signal_Bus.step_next.emit(current_era_value, 0)

	# 4. 结算完毕后，UI 彻底清空
	timeline_ui.clear_ui()

	# 5. 开启新回合！
	start_new_turn()

func start_new_turn():
	GameLogger.info("☀️ 新回合开始！", "Project")
	# 1. 时代值 +1 等系统级结算
	current_era_value += 1

	# 2. 玩家抽牌
	# ★ 新增修复：调用已存在的 attempt_draw_cards，取代之前错误的 draw_cards 函数名
	attempt_draw_cards(5)

	# 3. 敌人生成新的意图并塞满时间轴
	var all_enemies = get_tree().get_nodes_in_group("Enemies")
	if is_instance_valid(timeline_manager) and timeline_manager.has_method("generate_enemy_intents"):
		timeline_manager.generate_enemy_intents(all_enemies)

	# 恢复 UI
	enable_player_inputs()


# ==========================================
# ★ 新增修复：补齐缺失的玩家输入锁定/解锁方法
# ==========================================
# 这些函数在结算回合时极其重要，它们能切断玩家的任何鼠标操作（拖拽、抽牌），防止逻辑穿插崩溃
func disable_player_inputs():
	if is_instance_valid(player_hand): player_hand.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if is_instance_valid(deck_button): deck_button.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if is_instance_valid(discard_button): discard_button.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if is_instance_valid(end_turn_button): end_turn_button.disabled = true


func enable_player_inputs():
	if is_instance_valid(player_hand): player_hand.mouse_filter = Control.MOUSE_FILTER_PASS
	if is_instance_valid(deck_button): deck_button.mouse_filter = Control.MOUSE_FILTER_PASS
	if is_instance_valid(discard_button): discard_button.mouse_filter = Control.MOUSE_FILTER_PASS
	if is_instance_valid(end_turn_button): end_turn_button.disabled = false


func discard_all_hand_cards():
	if player_hand._held_cards.is_empty():
		GameLogger.debug("手牌已空，无需弃牌。", "Project")
		return

	GameLogger.info("正在弃掉所有手牌...", "Project")

	# ★ 核心安全机制：必须 duplicate() 复制一份数组！
	# 因为 move_cards 会在底层动态修改 player_hand._held_cards，
	# 边遍历边修改数组会导致严重的空指针报错。
	var cards_to_discard = player_hand._held_cards.duplicate()

	# 批量执行强行复位，防止有牌被玩家举在半空中强制结束回合，产生幽灵残影
	for card in cards_to_discard:
		if card.has_method("force_deselect"):
			card.force_deselect()  # 放下卡牌
		if card.has_method("change_state"):
			card.change_state(0)  # 强制变为闲置状态

	# 隐藏可能因为强制切走卡牌而残留在屏幕上的 Tooltip
	hide_tooltip()

	# 调用插件底层逻辑，将数组内所有卡牌一次性全移入弃牌堆！
	discard_pile.move_cards(cards_to_discard)

	# 挂起一帧，等待物理移动和底层字典结算完毕后更新数字 UI
	await get_tree().process_frame
	update_counts_and_ui()


# --- 抽牌逻辑 ---
func attempt_draw_cards(count: int):
	# ★ 拦截：如果锁正在开启，说明前一次抽牌/洗牌 await 还没结束，直接无视请求
	if is_processing_deck:
		GameLogger.warning("正在处理牌堆动画，忽略重复的抽牌请求", "Project")
		return
		
	if player_hand._held_cards.size() >= 7:
		GameLogger.warning("手牌已满，无法摸牌！", "Project")
		return

	# ★ 上锁
	is_processing_deck = true
	# ★ 禁用所有卡牌相关交互，防止抽牌途中玩家强行拖走刚抽一半的卡
	disable_player_inputs()

	GameLogger.info("开始摸牌，数量：%d" % count, "Project")
	for i in range(count):
		# --- 步骤 1: 检查牌堆是否为空，如果是则尝试洗牌 ---
		if deck_pile._held_cards.size() == 0:
			if discard_pile._held_cards.size() > 0:
				GameLogger.info("抽牌中途牌堆耗尽，触发洗牌...", "Project")
				await shuffle_card()  # 等待洗牌动画和逻辑完成
			else:
				GameLogger.warning("抽牌堆和弃牌堆都空了，无法继续抽牌！", "Project")
				break  # 真的没牌了，只能停止

		# --- 步骤 2: 再次检查牌堆 ---
		if deck_pile._held_cards.size() > 0:
			var top_card = deck_pile._held_cards.back()
			player_hand.move_cards([top_card])

			# 等待一帧，确保物理移动和数据更新
			await get_tree().process_frame
			update_counts_and_ui()
			# 抽卡间隔动画
			await get_tree().create_timer(0.1).timeout

	# ★ 解锁并恢复输入
	is_processing_deck = false
	enable_player_inputs()

# --- 弃牌判定逻辑 (拖拽松手) ---
func _input(event):
	# 【修复重点】删除了之前这里检测 deck_pile 距离的代码，因为现在用按钮了

	# 截获“空地释放”实现弃牌
	if event is InputEventMouseButton and not event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		for card in player_hand._held_cards:
			# 检查卡牌是否仍然有效（没有被释放）
			if not is_instance_valid(card):
				continue

			# 检查是否有卡牌处于被按住状态
			if card.is_pressed:
				var mouse_pos = get_global_mouse_position()
				var screen_size = get_viewport_rect().size
				var discard_threshold = screen_size.y * drop_area
				# 如果在上方区域松手
				if mouse_pos.y < discard_threshold:
					GameLogger.info("触发弃牌：%s" % card.card_info.get("name"), "Project")
					# 移动到弃牌堆
					# ================= ★ 新增修正 =================
					# 强制打断悬停或拖拽的表现，将其重置为闲置
					# 避免被丢入弃牌堆的卡残留放大和发光状态
					if card.has_method("change_state"):
						card.change_state(0)  # 0 对应 State.IDLE
					hide_tooltip()
					# ===============================================
					discard_pile.call_deferred("move_cards", [card])
					_handle_discard_effects(card)
					await get_tree().process_frame
					update_counts_and_ui()
					break


# 洗牌逻辑
func shuffle_card():
	if discard_pile._held_cards.is_empty():
		GameLogger.warning("弃牌堆也是空的，无牌可洗！", "Project")
		return

	GameLogger.info("正在将弃牌堆洗入抽牌堆...", "Project")

	# 1. 批量移动卡牌：从 discard_pile 移到 deck_pile
	# 注意：move_cards 会自动处理节点父子关系的切换
	var cards_to_move = discard_pile._held_cards.duplicate()
	deck_pile.move_cards(cards_to_move)

	# 2. 逻辑洗牌：打乱数组顺序
	# _held_cards 是 CardContainer 定义的存储卡牌引用的数组
	deck_pile._held_cards.shuffle()

	# 3. 视觉更新：确保所有牌盖着，并根据 Pile 逻辑重新堆叠
	# 强制抽牌堆的所有牌背面向上
	deck_pile.card_face_up = false
	await get_tree().create_timer(0.6).timeout
	deck_pile.update_card_ui()

	update_counts_and_ui()
	GameLogger.info("洗牌完成！当前抽牌堆数量：%d" % deck_pile._held_cards.size(), "Project")


# --- 辅助逻辑 ---
func _handle_discard_effects(card):
	var card_name = card.card_info.get("name", "未知卡牌")
	if card_name == "1":
		trigger_1_effect()
	if card_name == "2":
		trigger_1_effect()
	if card_name == "3":
		trigger_1_effect()


func trigger_1_effect():
	GameLogger.debug("draw 2", "Project")


# --- 纯代码构建词条 UI（完全分离版） ---
func setup_tooltip_ui():
	# ==========================================
	# 1. 创建唯一且层级最高的画布 (CanvasLayer)
	# ==========================================
	var tooltip_canvas = CanvasLayer.new()
	tooltip_canvas.layer = 2000  # 霸凌一切 UI 的究极层级，保证绝不被遮挡
	add_child(tooltip_canvas)

	# ==========================================
	# 2. 独立的左侧：核心效果文本框
	# ==========================================
	effect_tooltip_panel = PanelContainer.new()
	effect_tooltip_panel.z_index = 1000
	effect_tooltip_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	effect_tooltip_panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	effect_tooltip_panel.hide()

	# ★ 核心修复：只加给 tooltip_canvas，坚决不要再写 add_child(effect_tooltip_panel)
	tooltip_canvas.add_child(effect_tooltip_panel)

	# 样式设置
	var style = StyleBoxFlat.new()
	style.bg_color = tooltip_bg_color
	style.border_width_left = tooltip_border_width
	style.border_width_top = tooltip_border_width
	style.border_width_right = tooltip_border_width
	style.border_width_bottom = tooltip_border_width
	style.border_color = tooltip_border_color
	style.set_corner_radius_all(6)
	effect_tooltip_panel.add_theme_stylebox_override("panel", style)

	# 边距与文本节点
	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 15)
	margin.add_theme_constant_override("margin_right", 15)
	margin.add_theme_constant_override("margin_top", 15)
	margin.add_theme_constant_override("margin_bottom", 15)
	effect_tooltip_panel.add_child(margin)

	effect_label = RichTextLabel.new()
	effect_label.bbcode_enabled = true
	effect_label.fit_content = true
	effect_label.custom_minimum_size = Vector2(effect_panel_width, 0)
	# 应用效果文本字体设置
	effect_label.add_theme_font_size_override("normal_font_size", tooltip_effect_font_size)
	effect_label.add_theme_color_override("default_color", tooltip_effect_font_color)
	# 如果提供了自定义字体，应用它
	if tooltip_effect_font != null:
		effect_label.add_theme_font_override("normal_font", tooltip_effect_font)
	margin.add_child(effect_label)

	# ==========================================
	# 3. 独立的右侧：词条解释的“多列”横向容器
	# ==========================================
	keywords_tooltip_hbox = HBoxContainer.new()
	keywords_tooltip_hbox.z_index = 1000
	keywords_tooltip_hbox.mouse_filter = Control.MOUSE_FILTER_IGNORE
	keywords_tooltip_hbox.add_theme_constant_override("separation", tooltip_gap_x)
	keywords_tooltip_hbox.hide()

	# ★ 核心修复：同样只加给 tooltip_canvas，把多余的 add_child 全部删掉！
	tooltip_canvas.add_child(keywords_tooltip_hbox)

	tooltip_ui_initialized = true

# 动态生成单个带金边的小面板
func _create_keyword_panel(title_text: String, desc_text: String, title_color: String) -> PanelContainer:
	var panel = PanelContainer.new()
	var style = StyleBoxFlat.new()
	# ★ 应用导出的边框与颜色设置
	style.bg_color = tooltip_bg_color
	style.border_width_left = tooltip_border_width
	style.border_width_top = tooltip_border_width
	style.border_width_right = tooltip_border_width
	style.border_width_bottom = tooltip_border_width
	style.border_color = tooltip_border_color
	style.set_corner_radius_all(6)
	panel.add_theme_stylebox_override("panel", style)

	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 12)
	margin.add_theme_constant_override("margin_right", 12)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	var vbox = VBoxContainer.new()
	margin.add_child(vbox)

	var title = Label.new()
	title.text = title_text
	title.add_theme_color_override("font_color", Color(title_color))
	# ★ 应用导出的标题文字大小
	title.add_theme_font_size_override("font_size", tooltip_title_size)
	vbox.add_child(title)

	var desc = RichTextLabel.new()
	desc.bbcode_enabled = true
	desc.fit_content = true
	desc.autowrap_mode = TextServer.AUTOWRAP_ARBITRARY
	desc.custom_minimum_size = Vector2(tooltip_width, 0)
	desc.add_theme_font_size_override("normal_font_size", tooltip_desc_size)
	desc.add_theme_color_override("default_color", tooltip_desc_color)
	desc.text = desc_text
	vbox.add_child(desc)

	return panel


# 显示多重词条与效果 (分离式排版算法)
func show_tooltip(card: Control):
	# 优先从树根查找 card_manager 元数据，兼容场景切换
	var cm = null
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		cm = tree_root.get_meta("card_manager")
	else:
		# 回退到当前场景（向后兼容）
		var current_scene = get_tree().current_scene
		if current_scene and current_scene.has_meta("card_manager"):
			cm = current_scene.get_meta("card_manager")
	
	if not cm: return

	var selected_card = cm.get("current_selected_card")
	if selected_card != null and selected_card != card:
		return

	# 确保 Tooltip UI 已初始化
	if effect_tooltip_panel == null:
		setup_tooltip_ui()
	
	current_hovered_card = card

	# 确保 effect_label 已初始化
	if effect_label == null:
		push_error("effect_label 未初始化，无法显示 Tooltip")
		return

	# 1. 注入主效果文本
	var description_text = ""
	if card.has_method("get_parsed_description"):
		description_text = card.get_parsed_description()
	else:
		description_text = card.get("raw_description")
	
	# 确保文本不是 null
	if description_text == null:
		description_text = ""
	
	effect_label.text = description_text

	# 2. 清空并生成词条列
	if keywords_tooltip_hbox != null:
		for child in keywords_tooltip_hbox.get_children():
			keywords_tooltip_hbox.remove_child(child)
			child.queue_free()
	else:
		push_warning("keywords_tooltip_hbox 未初始化，跳过关键词显示")

	var keywords = card.get("active_keywords")
	# 确保 keywords 是一个数组，而不是 null
	if keywords == null:
		keywords = []
	
	# 只在 keywords_tooltip_hbox 有效时生成关键词面板
	if keywords_tooltip_hbox != null:
		var current_vbox: VBoxContainer = null
		for i in range(keywords.size()):
			var kw = keywords[i]
			if GlobalDB.KEYWORDS.has(kw):
				if i % max_keywords_per_column == 0:
					current_vbox = VBoxContainer.new()
					current_vbox.add_theme_constant_override("separation", tooltip_spacing)
					keywords_tooltip_hbox.add_child(current_vbox)

				var kw_data = GlobalDB.KEYWORDS[kw]
				var panel = _create_keyword_panel(kw, kw_data["desc"], kw_data["color"])
				current_vbox.add_child(panel)

	# ★ 强制重置两个独立框的尺寸，防幽灵撑大
	if effect_tooltip_panel != null:
		effect_tooltip_panel.size = Vector2.ZERO
	if keywords_tooltip_hbox != null:
		keywords_tooltip_hbox.size = Vector2.ZERO

	# 显现并测算
	if effect_tooltip_panel != null:
		effect_tooltip_panel.modulate = Color(1, 1, 1, 0)
		effect_tooltip_panel.show()

	var has_keywords = keywords.size() > 0
	if has_keywords and keywords_tooltip_hbox != null:
		keywords_tooltip_hbox.modulate = Color(1, 1, 1, 0)
		keywords_tooltip_hbox.show()

	await get_tree().process_frame
	if current_hovered_card != card: return

	var screen_size = get_viewport_rect().size
	var actual_card_width = card.size.x * card.scale.x
	var is_effect_placed_on_left = false

	# ==========================================
	# 独立计算 1：先摆放【主效果框】
	# ==========================================
	var eff_w = effect_tooltip_panel.size.x
	var eff_h = effect_tooltip_panel.size.y

	var eff_x = card.global_position.x + actual_card_width + tooltip_offset_x
	var eff_y = card.global_position.y + tooltip_offset_y

	# 如果主效果框在右侧出界，翻转到左边
	if eff_x + eff_w > screen_size.x:
		eff_x = card.global_position.x - eff_w - tooltip_offset_x
		is_effect_placed_on_left = true

	if eff_y + eff_h > screen_size.y - tooltip_bottom_margin:
		eff_y = screen_size.y - eff_h - tooltip_bottom_margin

	effect_tooltip_panel.global_position = Vector2(eff_x, eff_y)
	effect_tooltip_panel.modulate = Color(1, 1, 1, 1)

	# ==========================================
	# 独立计算 2：接着摆放【注释框】
	# ==========================================
	if has_keywords:
		var kw_w = keywords_tooltip_hbox.size.x
		var kw_h = keywords_tooltip_hbox.size.y

		var kw_x = 0.0
		var kw_y = eff_y  # 高度始终跟随效果框

		if not is_effect_placed_on_left:
			# 正常情况：注释框接在效果框右边
			kw_x = eff_x + eff_w + tooltip_gap_x
			# 如果注释框挤爆了屏幕右侧 -> 把它单独丢到卡牌的左侧！
			if kw_x + kw_w > screen_size.x:
				kw_x = card.global_position.x - kw_w - tooltip_offset_x
		else:
			# 翻转情况：效果框在左侧，注释框接在效果框左边
			kw_x = eff_x - kw_w - tooltip_gap_x
			# 如果注释框挤爆了屏幕左侧 -> 把它单独丢到卡牌的右侧！
			if kw_x < 0:
				kw_x = card.global_position.x + actual_card_width + tooltip_offset_x

		if kw_y + kw_h > screen_size.y - tooltip_bottom_margin:
			kw_y = screen_size.y - kw_h - tooltip_bottom_margin

		keywords_tooltip_hbox.global_position = Vector2(kw_x, kw_y)
		keywords_tooltip_hbox.modulate = Color(1, 1, 1, 1)


func hide_tooltip(card: Control = null):
	# 优先从树根查找 card_manager 元数据，兼容场景切换
	var cm = null
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		cm = tree_root.get_meta("card_manager")
	else:
		# 回退到当前场景（向后兼容）
		var current_scene = get_tree().current_scene
		if current_scene and current_scene.has_meta("card_manager"):
			cm = current_scene.get_meta("card_manager")
	
	# 如果有卡牌被选中，不隐藏工具提示
	if cm and cm.get("current_selected_card") != null:
		return

	current_hovered_card = null
	if is_instance_valid(effect_tooltip_panel): effect_tooltip_panel.hide()
	if is_instance_valid(keywords_tooltip_hbox): keywords_tooltip_hbox.hide()


# ==========================================
# ★ 新增修复：目标有效性检测与选中 Shader 控制
# ==========================================
# 校验玩家悬浮的地块是否满足当前举起卡牌的释放条件
func is_valid_target(hovered_stack: Area2D, active_card: Control) -> bool:
	if not is_instance_valid(hovered_stack) or not is_instance_valid(active_card):
		return false
	# TODO: 这里填入你的真实有效性检测。比如检测该地块上是否有可以攻击的敌人，距离是否足够等。
	# 暂时默认只要是存在的地块就是有效目标。
	return true


# 控制单个地块叠加选中特效的开关
func _set_stack_selected_shader(stack: Area2D, is_selected: bool):
	if not is_instance_valid(stack): return

	# 从地块身上提取刚才在 map.gd 中绑定的所有方格精灵
	var sprites = stack.get_meta("sprites") as Array
	if sprites == null or sprites.is_empty(): return

	var blend_val = 1.0 if is_selected else 0.0
	for s in sprites:
		if is_instance_valid(s) and s.material:
			# 向专属地块悬浮/选中 Shader 内传入选中混合度，1.0 代表变白蒙版激活
			s.set_instance_shader_parameter("is_selected_blend", blend_val)


func update_target_selection_hover(hovered_stack: Area2D, active_card: Control):
	var mouse_pos = get_global_mouse_position()
	# 这里使用 call_deferred 防止 Godot 的坐标尚未更新导致漂移
	set_cursor_tooltip_position(mouse_pos + Vector2(20, -30))

	if is_valid_target(hovered_stack, active_card):
		# ★ 合法目标：显示地块白色 Shader，计算并显示伤害
		_set_stack_selected_shader(hovered_stack, true)

		# ★ 修复：改为读取 card_info，并且获取你的 "ATK" 属性
		var dmg = active_card.card_info.get("ATK", 0) if "card_info" in active_card else 0
		cursor_tooltip.text = "-" + str(dmg)
		cursor_tooltip.add_theme_color_override("font_color", Color.RED)
		cursor_tooltip.show()

		# （这里还可以获取敌人的节点，让敌人变红闪烁）

	else:
		# ★ 非法目标：不播放变白 Shader，只显示灰色字
		_set_stack_selected_shader(hovered_stack, false)
		cursor_tooltip.text = "无效果"
		cursor_tooltip.add_theme_color_override("font_color", Color.GRAY)
		cursor_tooltip.show()


func _on_end_combat_pressed():
	GameLogger.info("🏆 战斗结束！进入局外卡牌操作阶段！", "Project")

	# 1. 隐藏局内所有战斗 UI (抽牌堆、弃牌堆、手牌)
	if is_instance_valid(player_hand): player_hand.hide()
	if is_instance_valid(deck_pile): deck_pile.hide()
	if is_instance_valid(discard_pile): discard_pile.hide()

	if is_instance_valid(deck_button): deck_button.hide()
	if is_instance_valid(discard_button): discard_button.hide()

	# 隐藏回合控制按钮
	if is_instance_valid(start_turn_button): start_turn_button.hide()
	if is_instance_valid(end_turn_button): end_turn_button.hide()
	if is_instance_valid(timeline_ui): timeline_ui.hide()

	# 这里不需要立刻弹出版面，而是等待玩家去点击地块
	# 玩家点击敌方地块的具体逻辑，可以直接调用：
	#这段是打开版面
	get_tree().get_first_node_in_group("MainBoard").reward_manager.open_reward_screen()

# 商店按钮回调
func _on_shop_button_pressed():
	GameLogger.info("🏪 玩家点击商店按钮，进入商店场景", "Project")
	# 隐藏除hexmap和时间币外的所有UI
	hide_ui_for_external_scene()
	
	# 加载并显示商店场景
	var shop_scene = preload("res://scene/in_scene/rewards/shop.tscn")
	if is_instance_valid(shop_scene):
		var shop_instance = shop_scene.instantiate()
		add_child(shop_instance)
		# 确保商店场景显示在最上层
		shop_instance.show()
		# 设置 CardManager 引用
		if shop_instance.has_method("set_deck_manager") and is_instance_valid(manager_instance):
			shop_instance.set_deck_manager(manager_instance)
		
		# 调用open_shop方法初始化商店
		if shop_instance.has_method("open_shop"):
			shop_instance.open_shop()
		elif shop_instance.has_method("open"):
			shop_instance.open()
		else:
			shop_instance.show()
		
		# 连接退出信号（如果场景有退出按钮）
		_connect_exit_signal_for_external_scene(shop_instance)
	else:
		GameLogger.error("无法加载商店场景", "Project")

# 获取卡牌奖励按钮回调
func _on_acquire_reward_button_pressed():
	GameLogger.info("🎁 玩家点击获取卡牌奖励按钮", "Project")
	# 隐藏除hexmap和时间币外的所有UI
	hide_ui_for_external_scene()
	
	var acquire_scene = preload("res://scene/in_scene/rewards/acquire_reward.tscn")
	if is_instance_valid(acquire_scene):
		var acquire_instance = acquire_scene.instantiate()
		add_child(acquire_instance)
		acquire_instance.show()
		if acquire_instance.has_method("set_deck_manager") and is_instance_valid(manager_instance):
			acquire_instance.set_deck_manager(manager_instance)
		
		# 调用open方法初始化场景
		if acquire_instance.has_method("open"):
			acquire_instance.open()
		else:
			acquire_instance.show()
		
		# 连接退出信号（如果场景有退出按钮）
		_connect_exit_signal_for_external_scene(acquire_instance)
	else:
		GameLogger.error("无法加载获取卡牌奖励场景", "Project")

# 删除卡牌奖励按钮回调
func _on_remove_reward_button_pressed():
	GameLogger.info("🗑️ 玩家点击删除卡牌奖励按钮", "Project")
	# 隐藏除hexmap和时间币外的所有UI
	hide_ui_for_external_scene()
	
	var remove_scene = preload("res://scene/in_scene/rewards/remove_reward.tscn")
	if is_instance_valid(remove_scene):
		var remove_instance = remove_scene.instantiate()
		add_child(remove_instance)
		remove_instance.show()
		if remove_instance.has_method("set_deck_manager") and is_instance_valid(manager_instance):
			remove_instance.set_deck_manager(manager_instance)
		
		# 调用open方法初始化场景
		if remove_instance.has_method("open"):
			remove_instance.open()
		else:
			remove_instance.show()
		
		# 连接退出信号（如果场景有退出按钮）
		_connect_exit_signal_for_external_scene(remove_instance)
	else:
		GameLogger.error("无法加载删除卡牌奖励场景", "Project")

# 合成卡牌奖励按钮回调
func _on_craft_reward_button_pressed():
	GameLogger.info("🔧 玩家点击合成卡牌奖励按钮", "Project")
	# 隐藏除hexmap和时间币外的所有UI
	hide_ui_for_external_scene()
	
	var craft_scene = preload("res://scene/in_scene/rewards/craft_reward.tscn")
	if is_instance_valid(craft_scene):
		var craft_instance = craft_scene.instantiate()
		add_child(craft_instance)
		craft_instance.show()
		if craft_instance.has_method("set_deck_manager") and is_instance_valid(manager_instance):
			craft_instance.set_deck_manager(manager_instance)
		
		# 调用open方法初始化场景
		if craft_instance.has_method("open"):
			craft_instance.open()
		else:
			craft_instance.show()
		
		# 连接退出信号（如果场景有退出按钮）
		_connect_exit_signal_for_external_scene(craft_instance)
	else:
		GameLogger.error("无法加载合成卡牌奖励场景", "Project")


# 当局外界面点击离开/下一关时调用这个函数
func proceed_to_next_stage():
	GameLogger.info("进入下一关，恢复局内 UI！", "Project")

	# 把刚才隐藏的按钮全部恢复显示
	if is_instance_valid(deck_button): deck_button.show()
	if is_instance_valid(discard_button): discard_button.show()
	if is_instance_valid(start_turn_button): start_turn_button.show()
	if is_instance_valid(end_turn_button): end_turn_button.show()
	if is_instance_valid(timeline_ui): timeline_ui.show()
	if is_instance_valid(player_hand): player_hand.show()

	# 初始化新回合（比如自动抽5张牌）
	# attempt_draw_cards(5)


# 接收来自关卡选择场景的数据
func apply_external_event(payload: String) -> void:
	print("[Project] 接收到外部事件数据: ", payload)
	# TODO: 解析 payload 并设置游戏参数
	# 格式示例: "battle_normal" 或 "battle_normal|enhance,combine"
	# 可以解析为关卡类型和额外功能
	# 暂时只记录，后续实现具体逻辑

## 增强光标提示框，模仿卡牌文本框的样式
func _enhance_cursor_tooltip() -> void:
	if not is_instance_valid(cursor_tooltip):
		GameLogger.warning("cursor_tooltip 无效，无法增强样式", "Project")
		return
	
	# 检查是否已经增强过（通过检查父节点是否为PanelContainer）
	if cursor_tooltip.get_parent() is PanelContainer:
		GameLogger.debug("cursor_tooltip 已经增强过样式", "Project")
		return
	
	# 保存原始标签的引用和属性
	var original_label = cursor_tooltip
	var original_position = original_label.global_position
	var original_visible = original_label.visible
	var original_text = original_label.text
	var original_parent = original_label.get_parent()
	var original_z_index = original_label.z_index
	
	# 创建PanelContainer作为新容器
	var panel = PanelContainer.new()
	panel.z_index = original_z_index + 1
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	panel.visible = original_visible
	
	# 应用样式（使用与卡牌tooltip相同的样式变量）
	var style = StyleBoxFlat.new()
	style.bg_color = tooltip_bg_color
	style.border_width_left = tooltip_border_width
	style.border_width_top = tooltip_border_width
	style.border_width_right = tooltip_border_width
	style.border_width_bottom = tooltip_border_width
	style.border_color = tooltip_border_color
	style.set_corner_radius_all(6)
	panel.add_theme_stylebox_override("panel", style)
	
	# 创建MarginContainer用于内边距
	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 8)
	margin.add_theme_constant_override("margin_bottom", 8)
	panel.add_child(margin)
	
	# 将原始标签重新父级到MarginContainer
	original_label.get_parent().remove_child(original_label)
	margin.add_child(original_label)
	
	# 将Panel添加到原始父节点
	original_parent.add_child(panel)
	panel.global_position = original_position
	
	# 更新Panel大小以适应内容
	panel.custom_minimum_size = Vector2(150, 40)
	
	# 隐藏原始标签的边框和背景（如果有）
	original_label.add_theme_stylebox_override("normal", StyleBoxEmpty.new())
	
	# 保存Panel引用
	cursor_tooltip_panel = panel
	
	# 连接可见性变化信号，确保Panel与Label同步
	original_label.visibility_changed.connect(_on_cursor_tooltip_visibility_changed)
	
	GameLogger.debug("cursor_tooltip 已增强为PanelContainer样式", "Project")

## 当cursor_tooltip可见性变化时，同步Panel的可见性
func _on_cursor_tooltip_visibility_changed() -> void:
	if is_instance_valid(cursor_tooltip_panel) and is_instance_valid(cursor_tooltip):
		cursor_tooltip_panel.visible = cursor_tooltip.visible

## 设置光标提示框的位置（增强后使用Panel的位置）
func set_cursor_tooltip_position(position: Vector2) -> void:
	if is_instance_valid(cursor_tooltip_panel):
		cursor_tooltip_panel.global_position = position
	elif is_instance_valid(cursor_tooltip):
		cursor_tooltip.global_position = position


## ==========================================
## ★ 局外场景UI管理函数
## ==========================================

## 隐藏除hexmap和时间币外的所有UI，为局外场景做准备
func hide_ui_for_external_scene():
	GameLogger.info("🔄 隐藏UI，为局外场景做准备", "Project")
	
	# 记录需要隐藏的UI元素
	# 注意：hex_map和timecoin_container会保持显示
	
	# 1. 隐藏卡牌系统UI
	if is_instance_valid(player_hand): player_hand.hide()
	if is_instance_valid(deck_pile): deck_pile.hide()
	if is_instance_valid(discard_pile): discard_pile.hide()
	
	# 2. 隐藏按钮UI
	if is_instance_valid(deck_button): deck_button.hide()
	if is_instance_valid(discard_button): discard_button.hide()
	if is_instance_valid(start_turn_button): start_turn_button.hide()
	if is_instance_valid(end_turn_button): end_turn_button.hide()
	if is_instance_valid(end_combat_button): end_combat_button.hide()
	
	# 3. 隐藏时间轴UI
	if is_instance_valid(timeline_ui): timeline_ui.hide()
	
	# 4. 隐藏光标提示
	if is_instance_valid(cursor_tooltip): cursor_tooltip.hide()
	if is_instance_valid(cursor_tooltip_panel): cursor_tooltip_panel.hide()
	
	# 5. 隐藏tooltip系统
	hide_tooltip()
	
	# 6. 隐藏四个局外按钮（它们会在场景退出时单独恢复）
	if is_instance_valid(shop_button): shop_button.hide()
	if is_instance_valid(acquire_reward_button): acquire_reward_button.hide()
	if is_instance_valid(remove_reward_button): remove_reward_button.hide()
	if is_instance_valid(craft_reward_button): craft_reward_button.hide()
	
	# 8. 确保hexmap和时间币显示
	if is_instance_valid(hex_map): 
		hex_map.show()
		# 确保hexmap在正确层级
		hex_map.z_index = 0
	
	if is_instance_valid(timecoin_container):
		timecoin_container.show()
		# 确保时间币UI在最上层
		timecoin_container.z_index = 100
	
	GameLogger.info("✅ UI隐藏完成，hexmap和时间币保持显示", "Project")

## 退出局外场景后，恢复四个局外按钮
func restore_ui_after_external_scene():
	GameLogger.info("🔄 恢复四个局外按钮", "Project")
	
	# 只恢复四个局外按钮，其他UI保持隐藏状态
	var buttons_restored = 0
	if is_instance_valid(shop_button):
		shop_button.show()
		GameLogger.debug("商店按钮显示: visible=%s, disabled=%s" % [shop_button.visible, shop_button.disabled], "Project")
		buttons_restored += 1
	else:
		GameLogger.warning("商店按钮引用无效", "Project")
	
	if is_instance_valid(acquire_reward_button):
		acquire_reward_button.show()
		GameLogger.debug("获取按钮显示: visible=%s, disabled=%s" % [acquire_reward_button.visible, acquire_reward_button.disabled], "Project")
		buttons_restored += 1
	else:
		GameLogger.warning("获取按钮引用无效", "Project")
	
	if is_instance_valid(remove_reward_button):
		remove_reward_button.show()
		GameLogger.debug("删除按钮显示: visible=%s, disabled=%s" % [remove_reward_button.visible, remove_reward_button.disabled], "Project")
		buttons_restored += 1
	else:
		GameLogger.warning("删除按钮引用无效", "Project")
	
	if is_instance_valid(craft_reward_button):
		craft_reward_button.show()
		GameLogger.debug("合成按钮显示: visible=%s, disabled=%s" % [craft_reward_button.visible, craft_reward_button.disabled], "Project")
		buttons_restored += 1
	else:
		GameLogger.warning("合成按钮引用无效", "Project")
	
	GameLogger.info("✅ 局外按钮已恢复 (恢复数量: %d/4)" % buttons_restored, "Project")

## 完全恢复所有UI（用于返回游戏主界面）
func restore_all_ui():
	GameLogger.info("🔄 恢复所有UI", "Project")
	
	# 恢复所有之前隐藏的UI元素
	if is_instance_valid(player_hand): player_hand.show()
	if is_instance_valid(deck_pile): deck_pile.show()
	if is_instance_valid(discard_pile): discard_pile.show()
	
	if is_instance_valid(deck_button): deck_button.show()
	if is_instance_valid(discard_button): discard_button.show()
	if is_instance_valid(start_turn_button): start_turn_button.show()
	if is_instance_valid(end_turn_button): end_turn_button.show()
	if is_instance_valid(end_combat_button): end_combat_button.show()
	
	if is_instance_valid(timeline_ui): timeline_ui.show()
	
	# 四个局外按钮也显示
	if is_instance_valid(shop_button): shop_button.show()
	if is_instance_valid(acquire_reward_button): acquire_reward_button.show()
	if is_instance_valid(remove_reward_button): remove_reward_button.show()
	if is_instance_valid(craft_reward_button): craft_reward_button.show()
	
	GameLogger.info("✅ 所有UI已恢复", "Project")

## 连接外部场景的退出信号
func _connect_exit_signal_for_external_scene(scene_instance: Node):
	# 尝试连接常见的退出按钮信号
	# 1. 检查 btn_exit (商店使用)
	if scene_instance.has_node("btn_exit"):
		var btn_exit = scene_instance.get_node("btn_exit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
			GameLogger.debug("已连接商店退出按钮", "Project")
	
	# 2. 检查 btn_back (奖励场景使用)
	if scene_instance.has_node("btn_back"):
		var btn_back = scene_instance.get_node("btn_back")
		if btn_back is Button and btn_back.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_back.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_back is Button:
			btn_back.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
			GameLogger.debug("已连接奖励场景返回按钮", "Project")
	
	# 3. 检查 BtnExit (带大写)
	if scene_instance.has_node("BtnExit"):
		var btn_exit = scene_instance.get_node("BtnExit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
			GameLogger.debug("已连接大写退出按钮", "Project")
	
	# 4. 检查 BtnBack (带大写)
	if scene_instance.has_node("BtnBack"):
		var btn_back = scene_instance.get_node("BtnBack")
		if btn_back is Button and btn_back.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_back.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_back is Button:
			btn_back.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
			GameLogger.debug("已连接大写返回按钮", "Project")
	
	# 5. 检查 Sidebar/BtnExit (商店侧边栏退出按钮)
	if scene_instance.has_node("Sidebar/BtnExit"):
		var btn_exit = scene_instance.get_node("Sidebar/BtnExit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
			GameLogger.debug("已连接侧边栏退出按钮", "Project")
	
	# 6. 检查 Sidebar/btn_exit (小写版本)
	if scene_instance.has_node("Sidebar/btn_exit"):
		var btn_exit = scene_instance.get_node("Sidebar/btn_exit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
			GameLogger.debug("已连接侧边栏小写退出按钮", "Project")

## 外部场景退出按钮回调
func _on_external_scene_exit_pressed(scene_instance: Node):
	GameLogger.info("🚪 退出局外场景，恢复UI", "Project")
	
	# 0. 先隐藏场景实例，防止覆盖按钮
	if is_instance_valid(scene_instance):
		scene_instance.hide()
		GameLogger.debug("已隐藏场景实例", "Project")
	
	# 1. 恢复四个局外按钮
	restore_ui_after_external_scene()
	
	# 2. 移除场景实例
	if is_instance_valid(scene_instance):
		scene_instance.queue_free()
		GameLogger.debug("已移除场景实例", "Project")
	
	# 3. 可选：如果需要完全恢复所有UI，可以调用 restore_all_ui()
	# 但根据需求，只恢复四个局外按钮，其他UI保持隐藏

# 信号响应：执行实际的动画转换逻辑
func _on_defeat_triggered():
	GameLogger.info("💀 收到失败信号，开始失败动画序列", "Project")
	
	# 隐藏所有战斗 UI
	hide_ui_for_external_scene()
	
	# 准备结算数据
	var stats = {
		"所处时代": str(current_era_value),
		"因果状态": "彻底断裂",
		"时间资产": str(GlobalTimecoin.get_timecoins() if GlobalTimecoin else 0),
		"同步率": "0%"
	}
	
	# 启动动画序列
	if is_instance_valid(game_over_ui):
		game_over_ui.start_sequence(stats)

# 按钮点击：只负责发出全局信号
func _on_lose_button_pressed():
	GameLogger.info("🔧 玩家点击失败调试按钮", "Project")
	Signal_Bus.emit_defeat_triggered()
	
func _on_win_button_button_down() -> void:
	win._on_victory_triggered()
