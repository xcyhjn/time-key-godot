extends Control

# 预加载资源
var hand_scene = load("res://addons/card-framework/hand.tscn")
var pile_scene = load("res://addons/card-framework/pile.tscn")
# 请确保这里路径正确，必须指向你真实的卡牌场景
var card_scene_ref = load("res://scene/card/custom_card.tscn")
var pile_viewer_scene = preload("res://scene/pile/pile_viewer.tscn")

const SETTLEMENT_REWARD_SCENE_PATHS := {
	"shop": "res://scene/in_scene/rewards/shop.tscn",
	"acquire": "res://scene/in_scene/rewards/acquire_reward.tscn",
	"remove": "res://scene/in_scene/rewards/remove_reward.tscn",
	"craft": "res://scene/in_scene/rewards/craft_reward.tscn",
}

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

enum BattleFlowState {
	COMBAT,
	SETTLEMENT
}

var current_battle_state: BattleFlowState = BattleFlowState.COMBAT
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
@export var hand_y_offset: float = 150.0  # 手牌区域下沉量 (默认 0.7 比例基础上的像素下移)
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
@onready var win_debug_button: Button = $"../WinButton"
@onready var combat_victory_debug_button: Button = $"../CombatVictoryDebugButton"
@onready var game_over_ui = $"../GameOver"
# ★ 新增：地图和时间币显示引用
@onready var hex_map = $"../../map/HexMap"
@onready var timecoin_container = get_node_or_null("/root/in_scene/TimecoinView/TimecoinCanvasLayer/TimecoinContainer")

# ★ 新增：回合与局内功能按钮引用
@onready var end_turn_button = $"../EndTurnButton"
@onready var end_combat_button = $"../EndCombatButton"  # 根据你的实际路径修改
@onready var height_view_toggle_button: Button = $"../HeightViewToggleButton"
@onready var cursor_tooltip = $"../CursorTooltip"  # 指向刚才创建的 Label
var cursor_tooltip_panel: PanelContainer  # 增强后的PanelContainer包装
@onready var timeline_ui = $"../TimelineUI"  # 根据你的实际路径修改
@onready var timeline_manager = $"../TimelineSystem/TimelineManager"
@onready var dim = $"../DimMenu"
@onready var win = $"../GameWinScreen"
@onready var total_enemy_health_bar = $"../TotalEnemyHealthBar"
@onready var combat_victory_banner = $"../../combat_victory_banner"
@onready var combat_cartoon_ui = $"../../CartoonUI"

# ================================
# ★ 导出调整项：Tooltip 资源
# ================================
@export_group("Tooltip资源配置")
@export var tooltip_config: TooltipConfig = preload("res://scene/shared/tooltip/battle_card_tooltip_config.tres")

## 主战斗场景使用完整版卡牌 Tooltip：
## - 主效果框
## - 关键词多列解释
## - 左右翻转与异步防幽灵
## 这些运行时细节统一交给共享 presenter。
var card_tooltip_presenter: CardTooltipPresenter = null

@export_group("Scene Transition")
## 单局结算结束后返回的局外场景路径。
## 单独导出成可调字段，方便你未来切换为别的世界地图场景而不改代码。
@export_file("*.tscn") var out_scene_path: String = "res://scene/out_scene/Out_Scene.tscn"

@export_group("局外收获配置")
## 是否继续显示左侧四个旧奖励按钮。
## 默认关闭，让玩家从建筑 tooltip 进入奖励；需要调试奖励页时可以在检查器里打开。
@export var show_settlement_debug_buttons: bool = false

@export_group("时间货币调试")
## 开启后，局内按键盘 9 可直接获得调试时间货币。
@export var enable_keyboard_timecoin_debug: bool = true
## 每次按下 9 获得的时间货币数量。
@export_range(1, 999999, 1, "or_greater") var keyboard_timecoin_debug_amount: int = 1000

@export_group("抽牌堆交互")
## 是否允许左键点击抽牌堆直接抽牌。
## 默认关闭：左键与右键都打开当前抽牌堆查看器，避免误抽牌。
@export var enable_left_click_draw_from_deck: bool = false
@export_range(1, 10, 1, "or_greater") var left_click_deck_draw_count: int = 3

@export_group("光标提示框")
## 光标 tooltip 会按文本内容自动收缩/扩展，但不会小于这个宽度；160px 约等于 10 个汉字。
@export var cursor_tooltip_min_width: float = 160.0
## 光标 tooltip 的最大文本宽度；超过后自动换行，避免长句飞出屏幕。
@export var cursor_tooltip_max_width: float = 260.0
@export var cursor_tooltip_min_height: float = 1.0

## 记录进入局内时携带的外部数据。
## 目前主要用于保留 battle_normal / battle_elite / boss_stage 这类来源标签，
## 让回到局外时仍然能带回基础上下文。
var incoming_external_payload: Variant = null
var incoming_battle_tag: String = ""
var incoming_map_seed: String = ""
var active_settlement_reward_context: Dictionary = {}

var _card_system_ready: bool = false
var _resolution_hides_debug_buttons: bool = false

## 首回合自动启动状态。
## 进入局内后会等待地图入场与时间轴入场都完成，再触发一次真正的回合开始。
var _first_turn_started: bool = false
var _first_turn_starting: bool = false


func _ready() -> void:
	_connect_global_clock_progress_signal()
	if SceneLog:
		SceneLog.scene_event("InSceneMain", "ready", {"incoming_payload": incoming_external_payload})

	# 初始化游戏状态
	_pull_era_from_global()
	
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
	
	_hide_settlement_buttons()

	## 将卡组管理器引用传给奖励界面，方便后续删牌/加牌操作
	#if is_instance_valid(reward_manager) and is_instance_valid(manager_instance):
		#reward_manager.deck_manager = manager_instance
	#else:
		#push_warning("project.gd: reward_manager 或 manager_instance 无效，无法设置 deck_manager")

	if is_instance_valid(timeline_manager):
		if timeline_manager.has_signal("action_hovered_changed"):
			timeline_manager.action_hovered_changed.connect(_on_timeline_action_hovered)
			if timeline_manager.has_signal("action_executed") and not timeline_manager.action_executed.is_connected(_on_timeline_action_executed):
				timeline_manager.action_executed.connect(_on_timeline_action_executed)
		else:
			push_warning("project.gd: timeline_manager 没有 action_hovered_changed 信号！")
	else:
		push_warning("project.gd: timeline_manager 无效！")
		# ★ 新增：将结算信号直接转交给 EffectProcessor
		var effect_processor = $TimelineSystem/EffectProcessor
		if is_instance_valid(effect_processor):
			if not timeline_manager.action_executed.is_connected(effect_processor.execute_action):
				timeline_manager.action_executed.connect(effect_processor.execute_action)
	
	# 局内开场不再提前生成一次敌方意图。
	# 等地图与时间轴完成入场后，统一进入第一回合，再抽牌并生成敌方意图。
	_schedule_auto_first_turn()
	
	# ★ 增强光标提示框样式，模仿卡牌文本框
	call_deferred("_enhance_cursor_tooltip")
	
	# 初始化时间轴UI引用
	if timeline_ui:
		pass

	# CartoonUI 是 in_scene 根节点的另一个子节点。
	# ui/Main 的 _ready 可能早于同级 CartoonUI 的 _ready，
	# 因此延后一帧再读它的 @onready 引用，避免只生成闹钟、不生成面板。
	call_deferred("_setup_combat_cartoon_ui")
	
	# 2. 连接失败按钮点击信号
	if is_instance_valid(lose_button):
		lose_button.pressed.connect(_on_lose_button_pressed)
	
	# 3. 监听全局失败信号
	if Signal_Bus:
		Signal_Bus.defeat_triggered.connect(_on_defeat_triggered)
		if not Signal_Bus.combat_victory_triggered.is_connected(_on_combat_victory_triggered):
			Signal_Bus.combat_victory_triggered.connect(_on_combat_victory_triggered)

	if is_instance_valid(hex_map) and hex_map.has_signal("settlement_reward_requested"):
		if not hex_map.settlement_reward_requested.is_connected(_on_settlement_reward_requested):
			hex_map.settlement_reward_requested.connect(_on_settlement_reward_requested)

	# 局外传入的房间类型 / seed 必须继续转交给 HexMap。
	# 否则 HexMap 会一直用默认空 payload 建图，后续关卡更容易出现初始化错位。
	call_deferred("_apply_incoming_payload_to_hex_map")
	call_deferred("_ensure_entry_dim_hidden")
	

func _connect_global_clock_progress_signal() -> void:
	if GlobalClock and GlobalClock.has_signal("progress_changed"):
		if not GlobalClock.progress_changed.is_connected(_on_global_clock_progress_changed):
			GlobalClock.progress_changed.connect(_on_global_clock_progress_changed)


func _on_global_clock_progress_changed(era_value: int, _phase_value: int) -> void:
	current_era_value = max(era_value, 1)
	_refresh_combat_cartoon_ui_progress()


## 从 GlobalClock 拉取当前时代值。
## UI 显示和回合推进都以 GlobalClock 为唯一来源。
func _pull_era_from_global() -> void:
	if GlobalClock:
		if GlobalClock.has_method("get_current_era"):
			current_era_value = int(GlobalClock.get_current_era())
		elif _object_has_property(GlobalClock, &"era"):
			current_era_value = int(GlobalClock.get("era"))
	else:
		current_era_value = 1

	current_era_value = max(current_era_value, 1)
	_push_era_to_global()


## 把局内当前时代值回写到 GlobalClock。
## 这是“局内战斗进度”与“局外全局进度”之间的同步桥。
func _push_era_to_global() -> void:
	current_era_value = max(current_era_value, 1)

	if GlobalClock:
		if GlobalClock.has_method("set_current_era"):
			GlobalClock.set_current_era(current_era_value)
		elif _object_has_property(GlobalClock, &"era"):
			GlobalClock.set("era", current_era_value)

	if MapState and MapState.has_method("set_saved_era_progress"):
		var phase_value := 1
		if GlobalClock and GlobalClock.has_method("get_current_phase"):
			phase_value = int(GlobalClock.get_current_phase())
		elif GlobalClock and _object_has_property(GlobalClock, &"phase"):
			phase_value = int(GlobalClock.get("phase"))
		MapState.set_saved_era_progress(current_era_value, phase_value)


## 初始化局内顶部 CartoonUI。
## 目标：
## - 局内不播放时钟入场动画
## - 直接把时钟停靠到右上角
## - 刷新顶部时代/阶段文本
## - 把时间轴整体向下让位，避免与顶部 UI 重叠
func _setup_combat_cartoon_ui() -> void:
	if not is_instance_valid(combat_cartoon_ui):
		return

	_refresh_combat_cartoon_ui_progress()

	if combat_cartoon_ui.has_method("set_character_index") and MapState:
		combat_cartoon_ui.set_character_index(int(MapState.chosen_char_index))

	if combat_cartoon_ui.has_method("apply_combat_layout"):
		combat_cartoon_ui.apply_combat_layout()

	if is_instance_valid(timeline_ui) and timeline_ui.has_method("set_top_reserved_space") and combat_cartoon_ui.has_method("get_reserved_height"):
		timeline_ui.set_top_reserved_space(float(combat_cartoon_ui.get_reserved_height()))


## 刷新局内顶部 CartoonUI 的时代/阶段文字。
func _refresh_combat_cartoon_ui_progress() -> void:
	if not is_instance_valid(combat_cartoon_ui):
		return
	if not combat_cartoon_ui.has_method("set_progress_labels"):
		return

	var era_value := current_era_value
	if GlobalClock and GlobalClock.has_method("get_current_era"):
		era_value = int(GlobalClock.get_current_era())
	elif GlobalClock and _object_has_property(GlobalClock, &"era"):
		era_value = int(GlobalClock.get("era"))
	current_era_value = max(era_value, 1)

	var phase_value := 1
	if GlobalClock and GlobalClock.has_method("get_current_phase"):
		phase_value = int(GlobalClock.get_current_phase())
	elif GlobalClock and _object_has_property(GlobalClock, &"phase"):
		phase_value = int(GlobalClock.get("phase"))

	combat_cartoon_ui.set_progress_labels(current_era_value, phase_value)


## 安全判断对象是否声明了当前场景会读取的全局进度属性。
## 注意：这里不要再调用 get_property_list()。
## GlobalClock 是一个带 UI/Shader 子节点的自动加载场景，Godot 在枚举完整属性列表时可能会顺带触碰
## ShaderMaterial 的内部版本数据，进而触发 “Parameter version is null” 的渲染层报错。
## 局内时代同步只需要 era / phase 两个字段，所以改成白名单判断，既更轻，也不会扫描无关资源。
func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	if property_name != &"era" and property_name != &"phase":
		return false
	if target == GlobalClock or target == Global:
		return true

	var target_script = target.get_script()
	if target_script == null:
		return false
	var script_path = target_script.resource_path
	return script_path == "res://scene/global/global_clock.gd" or script_path == "res://scene/global/global.gd"


## 安排局内自动进入第一回合。
## 这取代旧的“开局先生成一次敌方意图”流程，避免第一回合开始时重复生成意图。
func _schedule_auto_first_turn() -> void:
	if (
		is_instance_valid(hex_map)
		and hex_map.has_signal("map_intro_reveal_finished")
		and hex_map.has_method("is_map_intro_reveal_active")
		and hex_map.is_map_intro_reveal_active()
	):
		if not hex_map.map_intro_reveal_finished.is_connected(_start_first_turn_after_scene_ready):
			hex_map.map_intro_reveal_finished.connect(_start_first_turn_after_scene_ready)
		return

	call_deferred("_start_first_turn_after_scene_ready")


## 普通局内首回合入口：时间轴隐藏时通常代表教程流程正在接管，先交给教程导演。
func _start_first_turn_after_scene_ready() -> void:
	if incoming_battle_tag == "boss_stage":
		SoundManager.play_bgm("battle_boss")
	else:
		SoundManager.play_bgm_in_game()
	await _start_first_turn_after_timeline_ready(false)


## 教程 Dialogic 入口：显示时间轴后，同样走第一回合启动，保证规则和普通战斗一致。
func play_timeline_intro_and_generate_enemy_intents() -> void:
	if is_instance_valid(timeline_ui):
		timeline_ui.show()
	await _start_first_turn_after_timeline_ready(true)


func _start_first_turn_after_timeline_ready(force_timeline_visible: bool = false) -> void:
	if _first_turn_started or _first_turn_starting:
		return
	if not is_instance_valid(timeline_manager):
		return

	# 教程等特殊流程可能会在开局隐藏时间轴。
	# 此时先不自动开局，等导演节点调用 play_timeline_intro_and_generate_enemy_intents() 再继续。
	if is_instance_valid(timeline_ui) and not timeline_ui.visible and not force_timeline_visible:
		return

	_first_turn_starting = true
	disable_player_inputs()
	await _wait_for_card_system_ready()
	await _play_timeline_intro_if_visible()
	await _start_turn(false)
	_first_turn_started = true
	_first_turn_starting = false


func _wait_for_card_system_ready() -> void:
	while not _card_system_ready and is_inside_tree():
		await get_tree().process_frame


func _play_timeline_intro_if_visible() -> void:
	var intro_started: bool = false

	if is_instance_valid(combat_cartoon_ui) and combat_cartoon_ui.has_method("play_intro"):
		combat_cartoon_ui.play_intro()
		intro_started = true

	if is_instance_valid(total_enemy_health_bar) and total_enemy_health_bar.has_method("play_intro"):
		total_enemy_health_bar.play_intro()
		intro_started = true

	if is_instance_valid(timeline_ui) and timeline_ui.visible and timeline_ui.has_method("play_intro"):
		timeline_ui.play_intro()
		intro_started = true

	if intro_started:
		await _wait_for_battle_intro_ui()


func _wait_for_battle_intro_ui() -> void:
	while _is_any_battle_intro_ui_running():
		await get_tree().process_frame


func _is_any_battle_intro_ui_running() -> bool:
	if is_instance_valid(combat_cartoon_ui) and combat_cartoon_ui.has_method("is_intro_in_progress"):
		if bool(combat_cartoon_ui.call("is_intro_in_progress")):
			return true

	if is_instance_valid(total_enemy_health_bar) and total_enemy_health_bar.has_method("is_intro_in_progress"):
		if bool(total_enemy_health_bar.call("is_intro_in_progress")):
			return true

	if is_instance_valid(timeline_ui) and timeline_ui.visible and timeline_ui.has_method("is_intro_in_progress"):
		if bool(timeline_ui.call("is_intro_in_progress")):
			return true

	return false


func _refresh_enemy_intents_on_timeline() -> void:
	if not is_instance_valid(timeline_manager):
		return

	# 清除任何可能的残留占用
	if timeline_manager.has_method("clear_grid"):
		timeline_manager.clear_grid()
	
	var all_enemies = get_tree().get_nodes_in_group("Enemies")
	
	if all_enemies.is_empty():
		return
	
	if timeline_manager.has_method("generate_enemy_intents"):
		timeline_manager.generate_enemy_intents(all_enemies)
		# 调试：打印网格状态
		if timeline_manager.has_method("debug_print_grid"):
			timeline_manager.debug_print_grid()
	else:
		pass


func _on_timeline_action_hovered(action: TimelineAction, is_hovering: bool):
	# 敌方意图 hover 现在由 EnemyIntentPresentationController 直接监听 TimelineManager 信号处理。
	# 这里对 ENEMY 类型直接返回，避免旧逻辑与新系统重复触发。
	if action and action.type == TimelineAction.Type.ENEMY:
		return

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

	_card_system_ready = true


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


# --- 新增：抽牌堆的输入处理 (左键抽牌，右键查看) ---
func _on_deck_button_gui_input(event: InputEvent):
	# ★ 第一道防线：如果正在洗牌或抽牌，屏蔽此按钮的所有点击
	if is_processing_deck:
		return
		
	# 检查是否是鼠标按键事件
	if event is InputEventMouseButton and event.pressed:
		# --- 左键点击：抽牌 ---
		if event.button_index == MOUSE_BUTTON_LEFT:
			if enable_left_click_draw_from_deck:
				attempt_draw_cards(left_click_deck_draw_count)
			else:
				_open_deck_pile_viewer()

		# --- 右键点击：查看抽牌堆 ---
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			_open_deck_pile_viewer()

func _on_discard_button_pressed():
	# 点击弃牌堆按钮 -> 查看弃牌堆
	if discard_pile._held_cards.size() > 0:
		pile_viewer.open_pile_view(discard_pile, manager_instance)
	else:
		pass


func _open_deck_pile_viewer() -> void:
	if is_instance_valid(deck_pile) and deck_pile._held_cards.size() > 0:
		pile_viewer.open_pile_view(deck_pile, manager_instance)

# ==========================================
# ★ 回合流程控制区
# ==========================================

func _on_end_turn_pressed():
	if current_battle_state != BattleFlowState.COMBAT:
		return

	# 禁用 UI，防止结算期间玩家乱点
	disable_player_inputs()

	# 1. 弃置所有手牌
	discard_all_hand_cards()

	# 2. ★ 核心改动：加上 await！等待时间轴的每一列特效和扣血逐步播放完毕
	await timeline_manager.resolve_timeline()
	if current_battle_state != BattleFlowState.COMBAT:
		return

	# 3. 触发建筑行为（建筑扩张等）
	Signal_Bus.step_next.emit(current_era_value, 0)

	# 4. 结算完毕后，UI 彻底清空
	timeline_ui.clear_ui()

	# 5. 开启新回合！
	start_new_turn()

func start_new_turn():
	_start_turn(true)


func _start_turn(advance_phase: bool = true) -> void:
	if current_battle_state != BattleFlowState.COMBAT:
		return

	if Signal_Bus:
		Signal_Bus.new_turn_starting.emit()

	# 回合开始先结算建筑状态。
	# HexMap 会先创建状态快照，再逐个处理，避免中毒扩散在同一回合无限连锁。
	if is_instance_valid(hex_map) and hex_map.has_method("process_turn_start_statuses"):
		hex_map.process_turn_start_statuses()

	# 首回合进入局内时不额外推进阶段；后续回合开始才推进全局阶段。
	if advance_phase:
		_advance_global_phase()
	_refresh_combat_cartoon_ui_progress()

	# 玩家先抽 5 张牌，抽牌/洗牌动画完成后再生成敌方意图。
	await attempt_draw_cards(5)

	_refresh_enemy_intents_on_timeline()

	if Signal_Bus and Signal_Bus.has_method("emit_turn_started"):
		Signal_Bus.emit_turn_started(current_era_value)

	# 恢复 UI
	enable_player_inputs()


func _advance_global_phase() -> void:
	if GlobalClock:
		if GlobalClock.has_method("advance_phase"):
			GlobalClock.advance_phase()
		else:
			var next_phase := 1
			if GlobalClock.has_method("get_current_phase"):
				next_phase = int(GlobalClock.get_current_phase()) + 1
			elif _object_has_property(GlobalClock, &"phase"):
				next_phase = int(GlobalClock.get("phase")) + 1

			if GlobalClock.has_method("set_current_phase"):
				GlobalClock.set_current_phase(next_phase)
			elif _object_has_property(GlobalClock, &"phase"):
				GlobalClock.set("phase", next_phase)

			if GlobalClock.has_method("time_detect"):
				GlobalClock.time_detect()

		if GlobalClock.has_method("get_current_era"):
			current_era_value = int(GlobalClock.get_current_era())
		elif _object_has_property(GlobalClock, &"era"):
			current_era_value = int(GlobalClock.get("era"))
	else:
		current_era_value = max(current_era_value, 1)

	if MapState and MapState.has_method("set_saved_era_progress"):
		var phase_value := 1
		if GlobalClock and GlobalClock.has_method("get_current_phase"):
			phase_value = int(GlobalClock.get_current_phase())
		elif GlobalClock and _object_has_property(GlobalClock, &"phase"):
			phase_value = int(GlobalClock.get("phase"))
		MapState.set_saved_era_progress(current_era_value, phase_value)


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
		return


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
		return
		
	if player_hand._held_cards.size() >= 7:
		return

	# ★ 上锁
	is_processing_deck = true
	# ★ 禁用所有卡牌相关交互，防止抽牌途中玩家强行拖走刚抽一半的卡
	disable_player_inputs()

	for i in range(count):
		# --- 步骤 1: 检查牌堆是否为空，如果是则尝试洗牌 ---
		if deck_pile._held_cards.size() == 0:
			if discard_pile._held_cards.size() > 0:
				await shuffle_card()  # 等待洗牌动画和逻辑完成
			else:
				break  # 真的没牌了，只能停止

		# --- 步骤 2: 再次检查牌堆 ---
		if deck_pile._held_cards.size() > 0:
			var top_card = deck_pile._held_cards.back()
			player_hand.move_cards([top_card])
			Signal_Bus.emit_card_drawn(top_card, 1)

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
	if (
		enable_keyboard_timecoin_debug
		and event is InputEventKey
		and event.pressed
		and not event.echo
		and event.keycode == KEY_9
	):
		if GlobalTimecoin and GlobalTimecoin.has_method("add_timecoins"):
			GlobalTimecoin.add_timecoins(keyboard_timecoin_debug_amount)
			get_viewport().set_input_as_handled()
			return

	# 在“已选中卡牌但尚未进入时间占位拖拽”阶段，右键任意位置都可以取消选牌。
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_RIGHT:
		var drag_controller = get_tree().get_first_node_in_group("DragShapeController")
		var is_dragging_timeline_shape = false
		if drag_controller and drag_controller.get("is_dragging") != null:
			is_dragging_timeline_shape = drag_controller.is_dragging
		
		if not is_dragging_timeline_shape:
			var cm = manager_instance
			var selected_card = cm.get("current_selected_card") if cm else null
			if is_instance_valid(selected_card) and selected_card.has_method("force_deselect"):
				selected_card.force_deselect()
				hide_tooltip()
				get_viewport().set_input_as_handled()
				return

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
		return


	# 1. 批量移动卡牌：从 discard_pile 移到 deck_pile
	# 注意：move_cards 会自动处理节点父子关系的切换
	var cards_to_move = discard_pile._held_cards.duplicate()
	deck_pile.move_cards(cards_to_move)

	# 2. 逻辑洗牌：打乱数组顺序
	# _held_cards 是 CardContainer 定义的存储卡牌引用的数组
	deck_pile._held_cards.shuffle()
	Signal_Bus.emit_deck_shuffled()

	# 3. 视觉更新：确保所有牌盖着，并根据 Pile 逻辑重新堆叠
	# 强制抽牌堆的所有牌背面向上
	deck_pile.card_face_up = false
	await get_tree().create_timer(0.6).timeout
	deck_pile.update_card_ui()

	update_counts_and_ui()


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
	pass


# --- 共享 Tooltip 模块入口 ---
func _setup_card_tooltip_presenter() -> void:
	if card_tooltip_presenter != null:
		return
	card_tooltip_presenter = CardTooltipPresenter.new(self, tooltip_config, true)


## 保留旧接口名，避免其它脚本调用链重写。
## 现在它只负责初始化 presenter，而不再在本文件里手工拼 UI。
func setup_tooltip_ui():
	_setup_card_tooltip_presenter()
	card_tooltip_presenter.ensure_ui_created()


# 显示多重词条与效果
func show_tooltip(card: Control):
	# 优先从树根查找 card_manager 元数据，兼容场景切换
	var cm = null
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		cm = tree_root.get_meta("card_manager")
	else:
		pass
		# 回退到当前场景（向后兼容）
		var current_scene = get_tree().current_scene
		if current_scene and current_scene.has_meta("card_manager"):
			cm = current_scene.get_meta("card_manager")
	
	if not cm: return

	var selected_card = cm.get("current_selected_card")
	if selected_card != null and selected_card != card:
		return

	_setup_card_tooltip_presenter()
	card_tooltip_presenter.show_card_tooltip(card, {
		"show_keywords": true,
		"fallback_text": "",
	})


func hide_tooltip(card: Control = null):
	# 优先从树根查找 card_manager 元数据，兼容场景切换
	var cm = null
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		cm = tree_root.get_meta("card_manager")
	else:
		pass
		# 回退到当前场景（向后兼容）
		var current_scene = get_tree().current_scene
		if current_scene and current_scene.has_meta("card_manager"):
			cm = current_scene.get_meta("card_manager")
	
	# 如果有卡牌被选中，不隐藏工具提示
	if cm and cm.get("current_selected_card") != null:
		return

	if card_tooltip_presenter != null:
		card_tooltip_presenter.hide_tooltip()


# ==========================================
# ★ 新增修复：目标有效性检测与选中 Shader 控制
# ==========================================
# 校验玩家悬浮的地块是否满足当前举起卡牌的释放条件
func is_valid_target(hovered_stack: Area2D, active_card: Control) -> bool:
	if not is_instance_valid(hovered_stack) or not is_instance_valid(active_card):
		return false
	if _card_has_effect_type(active_card, "built") or _card_has_effect_type(active_card, "build"):
		return _is_empty_build_target(hovered_stack)
	return true


## built 类卡牌只允许选择空地块。
## 放牌阶段先拦截非法目标，结算阶段 BuiltCommand 仍会再校验一次，避免时间轴期间地块状态变化。
func _is_empty_build_target(stack: Area2D) -> bool:
	if not is_instance_valid(stack):
		return false

	if stack.has_meta("occupant"):
		var occupant: Variant = stack.get_meta("occupant")
		if is_instance_valid(occupant):
			return false

	if not is_instance_valid(hex_map):
		return false

	var coord: Variant = hex_map.stack_nodes.find_key(stack)
	if coord == null or not hex_map.map_data.has(coord):
		return false

	var tile_data: Variant = hex_map.map_data[coord]
	if typeof(tile_data) != TYPE_DICTIONARY:
		return false
	if tile_data.has("landform") and is_instance_valid(tile_data["landform"]):
		return false
	if tile_data.has("landform_in") and is_instance_valid(tile_data["landform_in"]):
		return false

	return true


func _card_has_effect_type(card: Control, effect_type: String) -> bool:
	var card_info: Variant = card.get("card_info")
	if typeof(card_info) != TYPE_DICTIONARY:
		return false

	var effects: Variant = card_info.get("effects", [])
	if typeof(effects) != TYPE_ARRAY:
		return false

	for effect in effects:
		if typeof(effect) == TYPE_DICTIONARY and str(effect.get("type", "")) == effect_type:
			return true
	return false


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
		# ★ 核心修复：坚决不在这里修改 Shader！全权交由 HexMap 的状态机处理
		var card_info = active_card.get("card_info")
		var dmg = card_info.get("ATK", 0) if typeof(card_info) == TYPE_DICTIONARY else 0
		cursor_tooltip.text = "-" + str(dmg)
		cursor_tooltip.add_theme_color_override("font_color", Color.RED)
		cursor_tooltip.show()
	else:
		pass
		# ★ 核心修复：坚决不在这里修改 Shader！
		cursor_tooltip.text = "无效果"
		cursor_tooltip.add_theme_color_override("font_color", Color.GRAY)
		cursor_tooltip.show()
		
func _on_end_combat_pressed():
	if current_battle_state != BattleFlowState.SETTLEMENT:
		return

	end_combat_button.disabled = true
	if Signal_Bus and Signal_Bus.has_method("emit_combat_ended"):
		Signal_Bus.emit_combat_ended()

	await _return_to_out_scene()


## 返回局外地图的统一入口。
## 这里做四件事：
## 1. 把局内维护的时代值同步回全局单例
## 2. 组装一份战斗返回 payload，供局外场景未来消费
## 3. 调用 DimMenu 做黑幕过渡
## 4. 以“实例化并切 current_scene”的方式回到 OutScene
func _return_to_out_scene() -> void:
	_push_era_to_global()

	var return_payload := _build_combat_return_payload()
	if SceneLog:
		SceneLog.scene_event("InSceneMain", "return to out scene", return_payload)
	if MapState and MapState.has_method("set_pending_room_resolution"):
		MapState.set_pending_room_resolution(return_payload)

	disable_player_inputs()
	hide_ui_for_external_scene()
	SoundManager.stop_looping_sfx()
	SoundManager.stop_bgm()
	SoundManager.play_bgm_main_menu()

	if is_instance_valid(dim):
		await dim.use(0, 0)

	_switch_scene_with_data(out_scene_path, return_payload)


## 组装“局内 -> 局外”的战斗返回数据。
## 当前先把最关键的全局进度数据带上：
## - 时代
## - 时间币
## - 当前牌组快照
## - 来源房间上下文
##
## 这样以后你要在局外做：
## - 清空当前战斗节点
## - 标记房间已完成
## - 发放奖励
## - 写入路线记录
## 都有足够的上下文字段可用。
func _build_combat_return_payload() -> Dictionary:
	var room_context: Dictionary = {}
	if MapState and MapState.has_method("get_active_room_context"):
		room_context = MapState.get_active_room_context()

	return {
		"transition_type": "return_from_combat",
		"combat_result": "completed",
		"battle_state": "settlement",
		"battle_tag": incoming_battle_tag,
		"map_seed": incoming_map_seed,
		"era": current_era_value,
		"timecoins": GlobalTimecoin.get_timecoins() if GlobalTimecoin and GlobalTimecoin.has_method("get_timecoins") else 0,
		"deck_snapshot": GlobalDB.player_deck.duplicate() if GlobalDB else [],
		"deck_size": GlobalDB.player_deck.size() if GlobalDB else 0,
		"room_context": room_context,
		"clear_active_room_context": true,
	}


## 通用场景切换函数。
## 注意：
## - 当前脚本挂在 ui/Main，而不是整张 in_scene 的根节点上
## - 因此不能简单地 queue_free(self)，而要释放 old_scene 根节点
func _switch_scene_with_data(path: String, payload: Variant = null) -> void:
	var packed_scene := _load_packed_scene_for_switch(path, "返回局外失败")
	if packed_scene == null:
		_recover_dim_after_failed_switch()
		return

	if SceneLog:
		SceneLog.scene_event("InSceneMain", "switch scene start", {"path": path, "payload": payload})
	var next_scene = packed_scene.instantiate()
	_apply_payload_to_new_scene_before_tree(next_scene, payload)

	var old_scene := get_tree().current_scene
	get_tree().root.add_child(next_scene)
	get_tree().current_scene = next_scene
	if SceneLog:
		SceneLog.scene_event("InSceneMain", "switch scene success", {"path": path})

	if is_instance_valid(old_scene):
		old_scene.queue_free()


## 用 ResourceLoader 加载 PackedScene，避免导出版 res:// 场景被 remap 后 FileAccess.file_exists() 误判。
func _load_packed_scene_for_switch(path: String, fail_message: String) -> PackedScene:
	if path.strip_edges() == "":
		_log_scene_switch_error(fail_message + "：场景路径为空", {"path": path})
		return null

	var packed_scene := ResourceLoader.load(path, "PackedScene") as PackedScene
	if packed_scene == null:
		_log_scene_switch_error(fail_message + "：无法加载 PackedScene", {
			"path": path,
			"resource_exists": ResourceLoader.exists(path, "PackedScene"),
		})
		return null

	return packed_scene


## 切场失败时把当前黑幕退回去，避免玩家停在全黑过渡层。
func _recover_dim_after_failed_switch() -> void:
	if is_instance_valid(dim) and dim.has_method("use"):
		dim.use(1, 1)


## 统一记录切场错误，导出版可在 user://logs/scene_flow.log 里定位。
func _log_scene_switch_error(message: String, extra: Dictionary = {}) -> void:
	if SceneLog:
		SceneLog.error_event("InSceneMain", message, extra)
	push_error("[InSceneMain] %s %s" % [message, str(extra)])


## 在新场景进入树之前写入返回 payload。
## OutScene 会把 payload 暂存在 pending_external_event，等 _ready() 完成后再消费。
func _apply_payload_to_new_scene_before_tree(next_scene: Node, payload: Variant) -> void:
	if payload == null:
		return
	if next_scene.has_method("apply_external_event"):
		next_scene.apply_external_event(payload)

# 商店按钮回调
func _on_shop_button_pressed():
	_open_settlement_reward_scene("shop", {})

# 获取卡牌奖励按钮回调
func _on_acquire_reward_button_pressed():
	_open_settlement_reward_scene("acquire", {})

# 删除卡牌奖励按钮回调
func _on_remove_reward_button_pressed():
	_open_settlement_reward_scene("remove", {})

# 合成卡牌奖励按钮回调
func _on_craft_reward_button_pressed():
	_open_settlement_reward_scene("craft", {})


## HexMap 点击某个收获建筑后会进入这里。
## Main 只关心奖励类型与上下文，不再关心建筑具体是哪一个脚本类。
func _on_settlement_reward_requested(reward_info: Dictionary) -> void:
	if current_battle_state != BattleFlowState.SETTLEMENT:
		return

	var reward_type := str(reward_info.get("reward_type", ""))
	_open_settlement_reward_scene(reward_type, reward_info)


## 打开局外收获奖励页的统一入口。
## 说明:
## - reward_context 为空时，表示旧调试按钮打开，不会消耗任何建筑。
## - reward_context 不为空时，会写到奖励场景 meta，退出时由 _on_external_scene_exit_pressed 统一判断是否消耗建筑。
func _open_settlement_reward_scene(reward_type: String, reward_context: Dictionary) -> void:
	if not SETTLEMENT_REWARD_SCENE_PATHS.has(reward_type):
		push_warning("未知的局外收获类型：%s" % reward_type)
		return

	var scene_path := str(SETTLEMENT_REWARD_SCENE_PATHS[reward_type])
	var packed_scene = load(scene_path)
	if packed_scene == null:
		push_error("无法加载局外收获场景：%s" % scene_path)
		return

	hide_ui_for_external_scene()
	active_settlement_reward_context = reward_context.duplicate()

	var reward_instance = packed_scene.instantiate()
	add_child(reward_instance)
	reward_instance.show()

	if not reward_context.is_empty():
		reward_instance.set_meta("settlement_reward_context", reward_context)
		# 商店只有退出按钮，按下退出即视为已经使用该建筑。
		if reward_type == "shop":
			reward_instance.set_meta("settlement_reward_consume_on_exit", true)
		else:
			reward_instance.set_meta("settlement_reward_committed", false)

	if reward_instance.has_method("set_deck_manager") and is_instance_valid(manager_instance):
		reward_instance.set_deck_manager(manager_instance)

	if reward_instance.has_method("open_shop"):
		reward_instance.open_shop()
	elif reward_instance.has_method("open"):
		reward_instance.open()
	else:
		reward_instance.show()

	_connect_exit_signal_for_external_scene(reward_instance)


# 当局外界面点击离开/下一关时调用这个函数
func proceed_to_next_stage():
	current_battle_state = BattleFlowState.COMBAT

	# 把刚才隐藏的按钮全部恢复显示
	if is_instance_valid(deck_button): deck_button.show()
	if is_instance_valid(discard_button): discard_button.show()
	if is_instance_valid(end_turn_button): end_turn_button.show()
	if is_instance_valid(timeline_ui): timeline_ui.show()
	if is_instance_valid(player_hand): player_hand.show()
	if is_instance_valid(hex_map) and hex_map.has_method("set_tiles_interactive"):
		hex_map.set_tiles_interactive(true)
	if is_instance_valid(hex_map) and hex_map.has_method("set_visuals_locked"):
		hex_map.set_visuals_locked(false)
	if is_instance_valid(hex_map) and hex_map.has_method("exit_settlement_reward_mode"):
		hex_map.exit_settlement_reward_mode()
	_hide_settlement_buttons()
	if is_instance_valid(total_enemy_health_bar): total_enemy_health_bar.show()
	_set_single_health_bars_visible(true)
	enable_player_inputs()

	# 初始化新回合（比如自动抽5张牌）
	# attempt_draw_cards(5)


# 接收来自关卡选择场景的数据
func apply_external_event(payload: String) -> void:
	print("[Project] 接收到外部事件数据: ", payload)
	if SceneLog:
		SceneLog.scene_event("InSceneMain", "apply_external_event", {"payload": payload})
	incoming_external_payload = payload
	incoming_battle_tag = ""
	incoming_map_seed = ""

	# 兼容当前局外场景传递格式：
	# "battle_normal <map_seed>"
	# "battle_elite <map_seed>"
	# "boss_stage <map_seed>"
	if payload == null:
		return

	var payload_text := str(payload).strip_edges()
	if payload_text == "":
		return

	var first_space_index := payload_text.find(" ")
	if first_space_index == -1:
		incoming_battle_tag = payload_text
	else:
		incoming_battle_tag = payload_text.substr(0, first_space_index)
		incoming_map_seed = payload_text.substr(first_space_index + 1).strip_edges()

	# 如果 Main 已经进入节点树，则立刻同步一次；
	# 如果切场时在 add_child() 前预注入，_ready 里的 deferred 会在 HexMap ready 后再补一次。
	if is_inside_tree():
		_apply_incoming_payload_to_hex_map()


func _apply_incoming_payload_to_hex_map() -> void:
	if not is_instance_valid(hex_map):
		if SceneLog:
			SceneLog.error_event("InSceneMain", "hex_map missing while applying payload")
		return
	if incoming_external_payload == null:
		return

	var payload_text := str(incoming_external_payload).strip_edges()
	if payload_text == "":
		return
	if not hex_map.has_method("apply_external_event"):
		if SceneLog:
			SceneLog.error_event("InSceneMain", "hex_map has no apply_external_event")
		return

	if SceneLog:
		SceneLog.scene_event("InSceneMain", "forward payload to hex_map", {"payload": payload_text})
	hex_map.apply_external_event(payload_text)


func _ensure_entry_dim_hidden() -> void:
	if not is_instance_valid(dim):
		return

	var dim_rect := dim.get_node_or_null("ColorRect") as ColorRect
	if is_instance_valid(dim_rect) and dim_rect.color.a <= 0.001:
		dim.hide()

## 增强光标提示框，模仿卡牌文本框的样式
func _enhance_cursor_tooltip() -> void:
	if not is_instance_valid(cursor_tooltip):
		return
	cursor_tooltip.hide()
	
	# 检查是否已经增强过（通过检查父节点是否为PanelContainer）
	if cursor_tooltip.get_parent() is PanelContainer:
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
	
	# 应用样式（复用 TooltipConfig，确保光标提示和卡牌提示保持同一套外框语言）
	var style = StyleBoxFlat.new()
	style.bg_color = tooltip_config.tooltip_bg_color
	style.border_width_left = tooltip_config.tooltip_border_width
	style.border_width_top = tooltip_config.tooltip_border_width
	style.border_width_right = tooltip_config.tooltip_border_width
	style.border_width_bottom = tooltip_config.tooltip_border_width
	style.border_color = tooltip_config.tooltip_border_color
	style.set_corner_radius_all(tooltip_config.tooltip_corner_radius)
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
	panel.custom_minimum_size = Vector2.ZERO
	
	# 隐藏原始标签的边框和背景（如果有）
	original_label.add_theme_stylebox_override("normal", StyleBoxEmpty.new())
	original_label.fit_content = true
	original_label.scroll_active = false
	original_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	original_label.custom_minimum_size = Vector2.ZERO
	
	# 保存Panel引用
	cursor_tooltip_panel = panel
	
	# 连接可见性变化信号，确保Panel与Label同步
	original_label.visibility_changed.connect(_on_cursor_tooltip_visibility_changed)
	

## 当cursor_tooltip可见性变化时，同步Panel的可见性
func _on_cursor_tooltip_visibility_changed() -> void:
	if is_instance_valid(cursor_tooltip_panel) and is_instance_valid(cursor_tooltip):
		_refresh_cursor_tooltip_size()
		cursor_tooltip_panel.visible = cursor_tooltip.visible

## 设置光标提示框的位置（增强后使用Panel的位置）
func set_cursor_tooltip_position(position: Vector2) -> void:
	_refresh_cursor_tooltip_size()
	if is_instance_valid(cursor_tooltip_panel):
		cursor_tooltip_panel.global_position = position
	elif is_instance_valid(cursor_tooltip):
		cursor_tooltip.global_position = position


## 刷新光标提示框尺寸。
## 核心逻辑：先关闭自动换行测自然宽度，再按最小/最大宽度重排，避免中文被压成一字一行。
func _refresh_cursor_tooltip_size() -> void:
	if not is_instance_valid(cursor_tooltip):
		return

	var min_width: float = maxf(cursor_tooltip_min_width, 1.0)
	var max_width: float = maxf(cursor_tooltip_max_width, min_width)
	var original_autowrap: int = cursor_tooltip.autowrap_mode

	cursor_tooltip.size = Vector2.ZERO
	cursor_tooltip.custom_minimum_size = Vector2.ZERO
	cursor_tooltip.autowrap_mode = TextServer.AUTOWRAP_OFF
	var content_width: float = float(cursor_tooltip.get_content_width())
	cursor_tooltip.autowrap_mode = original_autowrap

	var target_width: float = clampf(content_width, min_width, max_width)
	cursor_tooltip.size = Vector2(target_width, 0.0)
	var target_height: float = maxf(float(cursor_tooltip.get_content_height()), cursor_tooltip_min_height)
	cursor_tooltip.custom_minimum_size = Vector2(target_width, target_height)

	if is_instance_valid(cursor_tooltip_panel):
		cursor_tooltip_panel.size = Vector2.ZERO
		cursor_tooltip_panel.custom_minimum_size = Vector2.ZERO


## ==========================================
## ★ 局外场景UI管理函数
## ==========================================

## 隐藏除hexmap和时间币外的所有UI，为局外场景做准备
func hide_ui_for_external_scene():
	
	# 记录需要隐藏的UI元素
	# 注意：hex_map和timecoin_container会保持显示
	
	# 1. 隐藏卡牌系统UI
	if is_instance_valid(player_hand): player_hand.hide()
	if is_instance_valid(deck_pile): deck_pile.hide()
	if is_instance_valid(discard_pile): discard_pile.hide()
	
	# 2. 隐藏按钮UI
	if is_instance_valid(deck_button): deck_button.hide()
	if is_instance_valid(discard_button): discard_button.hide()
	if is_instance_valid(end_turn_button): end_turn_button.hide()
	if is_instance_valid(end_combat_button): end_combat_button.hide()
	if is_instance_valid(height_view_toggle_button):
		height_view_toggle_button.hide()
		height_view_toggle_button.disabled = true
	
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
	if is_instance_valid(total_enemy_health_bar): total_enemy_health_bar.hide()
	_set_single_health_bars_visible(false)
	
	# 8. 确保hexmap和时间币显示
	if is_instance_valid(hex_map): 
		hex_map.show()
		# 确保hexmap在正确层级
		hex_map.z_index = 0
	
	if is_instance_valid(timecoin_container):
		timecoin_container.show()
		# 确保时间币UI在最上层
		timecoin_container.z_index = 100


## 单体血条由 HexMap/BarManager 动态生成。
## 局外收获阶段要求不显示血条，所以这里统一转发给 BarManager。
func _set_single_health_bars_visible(is_visible: bool) -> void:
	if not is_instance_valid(hex_map):
		return
	var bar_manager = hex_map.get_node_or_null("BarManager")
	if bar_manager and bar_manager.has_method("set_all_health_bars_visible"):
		bar_manager.set_all_health_bars_visible(is_visible)


## 结算期统一隐藏调试入口。
## 覆盖失败、胜利、旧奖励调试等按钮，防止结算界面露出开发控件。
func _hide_debug_buttons_for_resolution() -> void:
	_resolution_hides_debug_buttons = true

	var debug_buttons: Array[Control] = [
		win_debug_button,
		combat_victory_debug_button,
		lose_button,
		shop_button,
		acquire_reward_button,
		remove_reward_button,
		craft_reward_button,
	]

	for button in debug_buttons:
		if not is_instance_valid(button):
			continue
		button.hide()
		if button is BaseButton:
			(button as BaseButton).disabled = true
	

## 退出局外场景后，恢复四个局外按钮
func restore_ui_after_external_scene():
	if _resolution_hides_debug_buttons:
		if current_battle_state == BattleFlowState.SETTLEMENT and is_instance_valid(end_combat_button):
			end_combat_button.show()
			end_combat_button.disabled = false
		if current_battle_state == BattleFlowState.SETTLEMENT:
			if is_instance_valid(total_enemy_health_bar):
				total_enemy_health_bar.hide()
			_set_single_health_bars_visible(false)
		return

	
	# 只恢复四个局外按钮，其他UI保持隐藏状态
	var buttons_restored = 0
	if show_settlement_debug_buttons and is_instance_valid(shop_button):
		shop_button.show()
		buttons_restored += 1
	elif is_instance_valid(shop_button):
		shop_button.hide()
	
	if show_settlement_debug_buttons and is_instance_valid(acquire_reward_button):
		acquire_reward_button.show()
		buttons_restored += 1
	elif is_instance_valid(acquire_reward_button):
		acquire_reward_button.hide()
	
	if show_settlement_debug_buttons and is_instance_valid(remove_reward_button):
		remove_reward_button.show()
		buttons_restored += 1
	elif is_instance_valid(remove_reward_button):
		remove_reward_button.hide()
	
	if show_settlement_debug_buttons and is_instance_valid(craft_reward_button):
		craft_reward_button.show()
		buttons_restored += 1
	elif is_instance_valid(craft_reward_button):
		craft_reward_button.hide()
	
	if current_battle_state == BattleFlowState.SETTLEMENT and is_instance_valid(end_combat_button):
		end_combat_button.show()
		end_combat_button.disabled = false
	
	if current_battle_state == BattleFlowState.SETTLEMENT:
		if is_instance_valid(total_enemy_health_bar):
			total_enemy_health_bar.hide()
		_set_single_health_bars_visible(false)
	

## 完全恢复所有UI（用于返回游戏主界面）
func restore_all_ui():
	
	# 恢复所有之前隐藏的UI元素
	if is_instance_valid(player_hand): player_hand.show()
	if is_instance_valid(deck_pile): deck_pile.show()
	if is_instance_valid(discard_pile): discard_pile.show()
	
	if is_instance_valid(deck_button): deck_button.show()
	if is_instance_valid(discard_button): discard_button.show()
	if is_instance_valid(end_turn_button): end_turn_button.show()
	if is_instance_valid(end_combat_button): end_combat_button.show()
	if is_instance_valid(height_view_toggle_button):
		height_view_toggle_button.show()
		height_view_toggle_button.disabled = false
	
	if is_instance_valid(timeline_ui): timeline_ui.show()
	
	# 四个局外按钮也显示
	if current_battle_state == BattleFlowState.SETTLEMENT:
		_show_settlement_buttons()
	else:
		_hide_settlement_buttons()
	if current_battle_state == BattleFlowState.SETTLEMENT:
		if is_instance_valid(total_enemy_health_bar): total_enemy_health_bar.hide()
		_set_single_health_bars_visible(false)
	else:
		if is_instance_valid(total_enemy_health_bar): total_enemy_health_bar.show()
		_set_single_health_bars_visible(true)
	

## 连接外部场景的退出信号
func _connect_exit_signal_for_external_scene(scene_instance: Node):
	# 奖励/商店场景本身已经在 _ready 里把按钮接到自己的 close / exit 流程。
	# 这些流程会回调 _on_external_scene_exit_pressed；这里如果再次直连按钮，
	# 会绕过奖励页自己的“退出禁用 / 未完成确认”等状态机。
	if scene_instance.has_method("_on_back_pressed") or scene_instance.has_method("_on_exit_pressed"):
		return

	# 尝试连接常见的退出按钮信号
	# 1. 检查 btn_exit (商店使用)
	if scene_instance.has_node("btn_exit"):
		var btn_exit = scene_instance.get_node("btn_exit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
	
	# 2. 检查 btn_back (奖励场景使用)
	if scene_instance.has_node("btn_back"):
		var btn_back = scene_instance.get_node("btn_back")
		if btn_back is Button and btn_back.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_back.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_back is Button:
			btn_back.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
	
	# 3. 检查 BtnExit (带大写)
	if scene_instance.has_node("BtnExit"):
		var btn_exit = scene_instance.get_node("BtnExit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
	
	# 4. 检查 BtnBack (带大写)
	if scene_instance.has_node("BtnBack"):
		var btn_back = scene_instance.get_node("BtnBack")
		if btn_back is Button and btn_back.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_back.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_back is Button:
			btn_back.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
	
	# 5. 检查 Sidebar/BtnExit (商店侧边栏退出按钮)
	if scene_instance.has_node("Sidebar/BtnExit"):
		var btn_exit = scene_instance.get_node("Sidebar/BtnExit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))
	
	# 6. 检查 Sidebar/btn_exit (小写版本)
	if scene_instance.has_node("Sidebar/btn_exit"):
		var btn_exit = scene_instance.get_node("Sidebar/btn_exit")
		if btn_exit is Button and btn_exit.pressed.is_connected(_on_external_scene_exit_pressed):
			btn_exit.pressed.disconnect(_on_external_scene_exit_pressed)
		if btn_exit is Button:
			btn_exit.pressed.connect(_on_external_scene_exit_pressed.bind(scene_instance))

## 外部场景退出按钮回调
func _on_external_scene_exit_pressed(scene_instance: Node):
	var should_consume_settlement_reward := false
	var reward_context: Dictionary = {}
	if is_instance_valid(scene_instance) and scene_instance.has_meta("settlement_reward_context"):
		var context_variant = scene_instance.get_meta("settlement_reward_context")
		if typeof(context_variant) == TYPE_DICTIONARY:
			reward_context = context_variant

		var committed = bool(scene_instance.get_meta("settlement_reward_committed", false))
		var consume_on_exit = bool(scene_instance.get_meta("settlement_reward_consume_on_exit", false))
		should_consume_settlement_reward = committed or consume_on_exit
	
	# 0. 先隐藏场景实例，防止覆盖按钮
	if is_instance_valid(scene_instance):
		scene_instance.hide()

	# 1. 如果奖励页确认完成，则通知 HexMap 更新建筑状态。
	if should_consume_settlement_reward:
		_consume_settlement_reward_context(reward_context)

	# 1. 恢复四个局外按钮
	restore_ui_after_external_scene()

	# 2. 移除场景实例
	if is_instance_valid(scene_instance):
		scene_instance.queue_free()

	active_settlement_reward_context.clear()
	
	# 3. 可选：如果需要完全恢复所有UI，可以调用 restore_all_ui()
	# 但根据需求，只恢复四个局外按钮，其他UI保持隐藏


## 消耗一个建筑绑定的收获奖励。
## Main 只负责把“这个奖励已经被确认使用”的事实传回 HexMap；
## 具体高亮、tooltip 文案、碰撞关闭都由 HexMap 统一处理。
func _consume_settlement_reward_context(reward_context: Dictionary) -> void:
	if reward_context.is_empty():
		return
	if not is_instance_valid(hex_map) or not hex_map.has_method("mark_settlement_reward_used"):
		return

	var reward_stack = reward_context.get("stack")
	if is_instance_valid(reward_stack):
		hex_map.mark_settlement_reward_used(reward_stack)

	active_settlement_reward_context.clear()

# 信号响应：执行实际的动画转换逻辑
func _on_defeat_triggered():
	SoundManager.stop_bgm()
	
	_hide_debug_buttons_for_resolution()

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

	Saver.Delete_save(0)

# 按钮点击：只负责发出全局信号
func _on_lose_button_pressed():
	Signal_Bus.emit_defeat_triggered()
	
func _on_combat_victory_debug_button_down() -> void:
	if Signal_Bus and Signal_Bus.has_method("emit_combat_victory_triggered"):
		Signal_Bus.emit_combat_victory_triggered()

func _on_win_button_button_down() -> void:
	SoundManager.stop_bgm()
	SoundManager.play_looping_sfx("game_win")
	_hide_debug_buttons_for_resolution()
	if is_instance_valid(win) and win.has_method("_on_victory_triggered"):
		win._on_victory_triggered()


func _hide_settlement_buttons() -> void:
	if is_instance_valid(shop_button): shop_button.hide()
	if is_instance_valid(acquire_reward_button): acquire_reward_button.hide()
	if is_instance_valid(remove_reward_button): remove_reward_button.hide()
	if is_instance_valid(craft_reward_button): craft_reward_button.hide()
	if is_instance_valid(end_combat_button):
		end_combat_button.hide()
		end_combat_button.disabled = false


func _show_settlement_buttons() -> void:
	if _resolution_hides_debug_buttons:
		if is_instance_valid(end_combat_button):
			end_combat_button.show()
			end_combat_button.disabled = false
		return

	if show_settlement_debug_buttons:
		if is_instance_valid(shop_button): shop_button.show()
		if is_instance_valid(acquire_reward_button): acquire_reward_button.show()
		if is_instance_valid(remove_reward_button): remove_reward_button.show()
		if is_instance_valid(craft_reward_button): craft_reward_button.show()
	else:
		if is_instance_valid(shop_button): shop_button.hide()
		if is_instance_valid(acquire_reward_button): acquire_reward_button.hide()
		if is_instance_valid(remove_reward_button): remove_reward_button.hide()
		if is_instance_valid(craft_reward_button): craft_reward_button.hide()
	if is_instance_valid(end_combat_button):
		end_combat_button.show()
		end_combat_button.disabled = false


func _hide_combat_phase_ui_for_settlement() -> void:
	if is_instance_valid(player_hand): player_hand.hide()
	if is_instance_valid(deck_pile): deck_pile.hide()
	if is_instance_valid(discard_pile): discard_pile.hide()
	if is_instance_valid(deck_button): deck_button.hide()
	if is_instance_valid(discard_button): discard_button.hide()
	if is_instance_valid(end_turn_button): end_turn_button.hide()
	if is_instance_valid(timeline_ui):
		if timeline_ui.has_method("clear_grid_preview"):
			timeline_ui.clear_grid_preview()
		timeline_ui.hide()
	if is_instance_valid(cursor_tooltip): cursor_tooltip.hide()
	if is_instance_valid(cursor_tooltip_panel): cursor_tooltip_panel.hide()


func _on_combat_victory_triggered() -> void:
	if current_battle_state != BattleFlowState.COMBAT:
		return

	current_battle_state = BattleFlowState.SETTLEMENT
	SoundManager.stop_bgm()
	_hide_debug_buttons_for_resolution()

	disable_player_inputs()
	_hide_combat_phase_ui_for_settlement()

	if is_instance_valid(timeline_manager) and timeline_manager.has_method("clear_grid"):
		timeline_manager.clear_grid()

	if is_instance_valid(hex_map):
		if hex_map.has_method("set_tiles_interactive"):
			hex_map.set_tiles_interactive(false)
		if hex_map.has_method("set_visuals_locked"):
			hex_map.set_visuals_locked(true)
		if hex_map.has_method("update_all_stack_conditional_effects"):
			hex_map.update_all_stack_conditional_effects()
		if hex_map.has_method("enter_settlement_reward_mode"):
			hex_map.enter_settlement_reward_mode(self)

	if is_instance_valid(total_enemy_health_bar):
		total_enemy_health_bar.hide()
	_set_single_health_bars_visible(false)

	if is_instance_valid(combat_victory_banner) and combat_victory_banner.has_method("play_banner"):
		await combat_victory_banner.play_banner("战斗胜利")

	_show_settlement_buttons()

func _on_timeline_action_executed(_action: TimelineAction) -> void:
	SoundManager.play_sfx("tile_damage")
