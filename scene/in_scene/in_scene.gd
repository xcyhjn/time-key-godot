extends Control

const HAND_SCENE: PackedScene = preload("res://addons/card-framework/hand.tscn")
const PILE_SCENE: PackedScene = preload("res://addons/card-framework/pile.tscn")
const CARD_SCENE_REF: PackedScene = preload("res://scene/card/custom_card.tscn")
const PILE_VIEWER_SCENE: PackedScene = preload("res://scene/pile/pile_viewer.tscn")
const CARD_MANAGER_SCENE: PackedScene = preload("res://addons/card-framework/card_manager.tscn")
const CARD_FACTORY_SCENE: PackedScene = preload("res://addons/card-framework/card_factory.tscn")
const HEX_TARGET_RULES := preload("res://scene/in_scene/hex_map_modules/rules/HexTargetRules.gd")
const IN_SCENE_NODE_BRIDGE := preload("res://scene/in_scene/in_scene_modules/bridges/InSceneNodeBridge.gd")
const IN_SCENE_GLOBAL_CLOCK_BRIDGE := preload("res://scene/in_scene/in_scene_modules/bridges/InSceneGlobalClockBridge.gd")
const CARD_PILE_UI_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/cards/CardPileUiController.gd")
const CARD_SYSTEM_BOOTSTRAP := preload("res://scene/in_scene/in_scene_modules/cards/CardSystemBootstrap.gd")
const CARD_DRAW_FLOW_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/cards/CardDrawFlowController.gd")
const HAND_DISCARD_FLOW_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/cards/HandDiscardFlowController.gd")
const IN_SCENE_INPUT_LOCK_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/ui/InSceneInputLockController.gd")
const FIRST_TURN_INTRO_RUNNER := preload("res://scene/in_scene/in_scene_modules/turn/FirstTurnIntroRunner.gd")
const ENEMY_INTENT_TIMELINE_REFRESHER := preload("res://scene/in_scene/in_scene_modules/turn/EnemyIntentTimelineRefresher.gd")
const CARD_TOOLTIP_UI_ADAPTER := preload("res://scene/in_scene/in_scene_modules/ui/CardTooltipUiAdapter.gd")
const COMBAT_CARTOON_UI_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/ui/CombatCartoonUiController.gd")
const CURSOR_TOOLTIP_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/ui/CursorTooltipController.gd")
const IN_SCENE_INPUT_EVENT_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/ui/InSceneInputEventController.gd")
const IN_SCENE_UI_VISIBILITY_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/ui/InSceneUiVisibilityController.gd")
const TARGET_SELECTION_HOVER_UI_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/ui/TargetSelectionHoverUiController.gd")
const TIMELINE_ACTION_HOVER_UI_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/ui/TimelineActionHoverUiController.gd")
const SETTLEMENT_DECK_SNAPSHOT_SERVICE := preload("res://scene/in_scene/in_scene_modules/settlement/SettlementDeckSnapshotService.gd")
const SETTLEMENT_DECK_RECLAIM_SERVICE := preload("res://scene/in_scene/in_scene_modules/settlement/SettlementDeckReclaimService.gd")
const SETTLEMENT_REWARD_SCENE_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/settlement/SettlementRewardSceneController.gd")
const SETTLEMENT_REWARD_EXIT_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/settlement/SettlementRewardExitController.gd")
const SETTLEMENT_REWARD_CONSUMER := preload("res://scene/in_scene/in_scene_modules/settlement/SettlementRewardConsumer.gd")
const COMBAT_VICTORY_SETTLEMENT_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/settlement/CombatVictorySettlementController.gd")
const COMBAT_DEFEAT_FLOW_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/settlement/CombatDefeatFlowController.gd")
const GAME_WIN_FLOW_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/settlement/GameWinFlowController.gd")
const IN_SCENE_RETURN_PAYLOAD_BUILDER := preload("res://scene/in_scene/in_scene_modules/scene_flow/InSceneReturnPayloadBuilder.gd")
const IN_SCENE_EXTERNAL_PAYLOAD_PARSER := preload("res://scene/in_scene/in_scene_modules/scene_flow/InSceneExternalPayloadParser.gd")
const IN_SCENE_PAYLOAD_BRIDGE := preload("res://scene/in_scene/in_scene_modules/scene_flow/InScenePayloadBridge.gd")
const IN_SCENE_SCENE_SWITCH_LOADER := preload("res://scene/in_scene/in_scene_modules/scene_flow/InSceneSceneSwitchLoader.gd")
const IN_SCENE_SCENE_SWITCH_EXECUTOR := preload("res://scene/in_scene/in_scene_modules/scene_flow/InSceneSceneSwitchExecutor.gd")
const IN_SCENE_RETURN_FLOW_CONTROLLER := preload("res://scene/in_scene/in_scene_modules/scene_flow/InSceneReturnFlowController.gd")

# 预加载资源
var hand_scene: PackedScene = HAND_SCENE
var pile_scene: PackedScene = PILE_SCENE
# 请确保这里路径正确，必须指向你真实的卡牌场景
var card_scene_ref: PackedScene = CARD_SCENE_REF
var pile_viewer_scene: PackedScene = PILE_VIEWER_SCENE

const SETTLEMENT_REWARD_SCENE_PATHS := {
	"shop": "res://scene/in_scene/rewards/shop.tscn",
	"acquire": "res://scene/in_scene/rewards/acquire_reward.tscn",
	"remove": "res://scene/in_scene/rewards/remove_reward.tscn",
	"craft": "res://scene/in_scene/rewards/craft_reward.tscn",
}
var _settlement_reward_scene_cache: Dictionary = {}

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
var deck_button
var discard_button
var deck_count_label
var discard_count_label

# ★ 新增：局外收获按钮引用
var shop_button
var acquire_reward_button
var remove_reward_button
var craft_reward_button
var lose_button
var win_debug_button: Button
var combat_victory_debug_button: Button
var game_over_ui
# ★ 新增：地图和时间币显示引用
var hex_map
var timecoin_container

# ★ 新增：回合与局内功能按钮引用
var end_turn_button
var end_combat_button
var height_view_toggle_button: Button
var cursor_tooltip
var cursor_tooltip_panel: PanelContainer  # 增强后的PanelContainer包装
var timeline_ui
var timeline_manager
var dim
var win
var total_enemy_health_bar
var combat_victory_banner
var combat_cartoon_ui

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

var _node_bridge: RefCounted = IN_SCENE_NODE_BRIDGE.new()
var _global_clock_bridge: RefCounted = IN_SCENE_GLOBAL_CLOCK_BRIDGE.new()
var _card_pile_ui_controller: RefCounted = CARD_PILE_UI_CONTROLLER.new()
var _card_system_bootstrap: RefCounted = CARD_SYSTEM_BOOTSTRAP.new()
var _card_draw_flow_controller: RefCounted = CARD_DRAW_FLOW_CONTROLLER.new()
var _hand_discard_flow_controller: RefCounted = HAND_DISCARD_FLOW_CONTROLLER.new()
var _input_lock_controller: RefCounted = IN_SCENE_INPUT_LOCK_CONTROLLER.new()
var _first_turn_intro_runner: RefCounted = FIRST_TURN_INTRO_RUNNER.new()
var _enemy_intent_timeline_refresher: RefCounted = ENEMY_INTENT_TIMELINE_REFRESHER.new()
var _card_tooltip_ui_adapter: RefCounted = CARD_TOOLTIP_UI_ADAPTER.new()
var _combat_cartoon_ui_controller: RefCounted = COMBAT_CARTOON_UI_CONTROLLER.new()
var _cursor_tooltip_controller: RefCounted = CURSOR_TOOLTIP_CONTROLLER.new()
var _input_event_controller: RefCounted = IN_SCENE_INPUT_EVENT_CONTROLLER.new()
var _ui_visibility_controller: RefCounted = IN_SCENE_UI_VISIBILITY_CONTROLLER.new()
var _target_selection_hover_ui_controller: RefCounted = TARGET_SELECTION_HOVER_UI_CONTROLLER.new()
var _timeline_action_hover_ui_controller: RefCounted = TIMELINE_ACTION_HOVER_UI_CONTROLLER.new()
var _settlement_deck_snapshot_service: RefCounted = SETTLEMENT_DECK_SNAPSHOT_SERVICE.new()
var _settlement_deck_reclaim_service: RefCounted = SETTLEMENT_DECK_RECLAIM_SERVICE.new()
var _settlement_reward_scene_controller: RefCounted = SETTLEMENT_REWARD_SCENE_CONTROLLER.new()
var _settlement_reward_exit_controller: RefCounted = SETTLEMENT_REWARD_EXIT_CONTROLLER.new()
var _settlement_reward_consumer: RefCounted = SETTLEMENT_REWARD_CONSUMER.new()
var _combat_victory_settlement_controller: RefCounted = COMBAT_VICTORY_SETTLEMENT_CONTROLLER.new()
var _combat_defeat_flow_controller: RefCounted = COMBAT_DEFEAT_FLOW_CONTROLLER.new()
var _game_win_flow_controller: RefCounted = GAME_WIN_FLOW_CONTROLLER.new()
var _return_payload_builder: RefCounted = IN_SCENE_RETURN_PAYLOAD_BUILDER.new()
var _external_payload_parser: RefCounted = IN_SCENE_EXTERNAL_PAYLOAD_PARSER.new()
var _payload_bridge: RefCounted = IN_SCENE_PAYLOAD_BRIDGE.new()
var _scene_switch_loader: RefCounted = IN_SCENE_SCENE_SWITCH_LOADER.new()
var _scene_switch_executor: RefCounted = IN_SCENE_SCENE_SWITCH_EXECUTOR.new()
var _return_flow_controller: RefCounted = IN_SCENE_RETURN_FLOW_CONTROLLER.new()


func _ready() -> void:
	_resolve_scene_nodes()
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
		_connect_signal_once(end_combat_button.pressed, _on_end_combat_pressed)
	
	_hide_settlement_buttons()

	## 将卡组管理器引用传给奖励界面，方便后续删牌/加牌操作
	#if is_instance_valid(reward_manager) and is_instance_valid(manager_instance):
		#reward_manager.deck_manager = manager_instance
	#else:
		#push_warning("project.gd: reward_manager 或 manager_instance 无效，无法设置 deck_manager")

	if is_instance_valid(timeline_manager):
		_connect_signal_once(timeline_manager.action_hovered_changed, _on_timeline_action_hovered)
		_connect_signal_once(timeline_manager.action_executed, _on_timeline_action_executed)
	
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
		_connect_signal_once(lose_button.pressed, _on_lose_button_pressed)
	
	# 3. 监听全局失败信号
	if Signal_Bus:
		_connect_signal_once(Signal_Bus.defeat_triggered, _on_defeat_triggered)
		_connect_signal_once(Signal_Bus.combat_victory_triggered, _on_combat_victory_triggered)

	if is_instance_valid(hex_map):
		_connect_signal_once(hex_map.settlement_reward_requested, _on_settlement_reward_requested)

	# 局外传入的房间类型 / seed 必须继续转交给 HexMap。
	# 否则 HexMap 会一直用默认空 payload 建图，后续关卡更容易出现初始化错位。
	call_deferred("_apply_incoming_payload_to_hex_map")
	call_deferred("_ensure_entry_dim_hidden")
	

func _resolve_scene_nodes() -> void:
	var nodes: Dictionary = _node_bridge.resolve(self)
	deck_button = nodes.get("deck_button")
	discard_button = nodes.get("discard_button")
	deck_count_label = nodes.get("deck_count_label")
	discard_count_label = nodes.get("discard_count_label")
	shop_button = nodes.get("shop_button")
	acquire_reward_button = nodes.get("acquire_reward_button")
	remove_reward_button = nodes.get("remove_reward_button")
	craft_reward_button = nodes.get("craft_reward_button")
	lose_button = nodes.get("lose_button")
	win_debug_button = nodes.get("win_debug_button") as Button
	combat_victory_debug_button = nodes.get("combat_victory_debug_button") as Button
	game_over_ui = nodes.get("game_over_ui")
	hex_map = nodes.get("hex_map")
	timecoin_container = nodes.get("timecoin_container")
	end_turn_button = nodes.get("end_turn_button")
	end_combat_button = nodes.get("end_combat_button")
	height_view_toggle_button = nodes.get("height_view_toggle_button") as Button
	cursor_tooltip = nodes.get("cursor_tooltip")
	timeline_ui = nodes.get("timeline_ui")
	timeline_manager = nodes.get("timeline_manager")
	dim = nodes.get("dim")
	win = nodes.get("win")
	total_enemy_health_bar = nodes.get("total_enemy_health_bar")
	combat_victory_banner = nodes.get("combat_victory_banner")
	combat_cartoon_ui = nodes.get("combat_cartoon_ui")


func _connect_global_clock_progress_signal() -> void:
	_global_clock_bridge.connect_progress_changed(_connect_signal_once, _on_global_clock_progress_changed)


func _connect_signal_once(source_signal: Signal, callback: Callable) -> void:
	if not source_signal.is_connected(callback):
		source_signal.connect(callback)


func _on_global_clock_progress_changed(era_value: int, _phase_value: int) -> void:
	current_era_value = max(era_value, 1)
	_refresh_combat_cartoon_ui_progress()


## 从 GlobalClock 拉取当前时代值。
## UI 显示和回合推进都以 GlobalClock 为唯一来源。
func _pull_era_from_global() -> void:
	current_era_value = _global_clock_bridge.pull_era()
	_push_era_to_global()


## 把局内当前时代值回写到 GlobalClock。
## 这是“局内战斗进度”与“局外全局进度”之间的同步桥。
func _push_era_to_global() -> void:
	current_era_value = _global_clock_bridge.push_era(current_era_value)


## 初始化局内顶部 CartoonUI。
## 目标：
## - 局内不播放时钟入场动画
## - 直接把时钟停靠到右上角
## - 刷新顶部时代/阶段文本
## - 把时间轴整体向下让位，避免与顶部 UI 重叠
func _setup_combat_cartoon_ui() -> void:
	var result: Dictionary = _combat_cartoon_ui_controller.setup({
		"combat_cartoon_ui": combat_cartoon_ui,
		"timeline_ui": timeline_ui,
		"map_state": MapState,
		"global_clock_bridge": _global_clock_bridge,
	})
	current_era_value = int(result.get("current_era_value", current_era_value))


## 刷新局内顶部 CartoonUI 的时代/阶段文字。
func _refresh_combat_cartoon_ui_progress() -> void:
	var result: Dictionary = _combat_cartoon_ui_controller.refresh_progress({
		"combat_cartoon_ui": combat_cartoon_ui,
		"global_clock_bridge": _global_clock_bridge,
	})
	current_era_value = int(result.get("current_era_value", current_era_value))


## 安排局内自动进入第一回合。
## 这取代旧的“开局先生成一次敌方意图”流程，避免第一回合开始时重复生成意图。
func _schedule_auto_first_turn() -> void:
	if _first_turn_intro_runner.should_wait_for_map_intro(hex_map):
		_first_turn_intro_runner.connect_map_intro_finished(hex_map, _start_first_turn_after_scene_ready)
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
	if not _first_turn_intro_runner.can_start_first_turn({
		"first_turn_started": _first_turn_started,
		"first_turn_starting": _first_turn_starting,
		"timeline_manager": timeline_manager,
		"timeline_ui": timeline_ui,
		"force_timeline_visible": force_timeline_visible,
	}):
		return

	_first_turn_starting = true
	disable_player_inputs()
	await _wait_for_card_system_ready()
	await _play_timeline_intro_if_visible()
	await _start_turn(false)
	_first_turn_started = true
	_first_turn_starting = false


func _wait_for_card_system_ready() -> void:
	await _first_turn_intro_runner.wait_for_card_system_ready(self, func() -> bool:
		return _card_system_ready
	)


func _play_timeline_intro_if_visible() -> void:
	await _first_turn_intro_runner.play_intro_if_visible(self, _build_first_turn_intro_config())


func _wait_for_battle_intro_ui() -> void:
	await _first_turn_intro_runner.wait_for_battle_intro_ui(self, _build_first_turn_intro_config())


func _is_any_battle_intro_ui_running() -> bool:
	return _first_turn_intro_runner.is_any_battle_intro_ui_running(_build_first_turn_intro_config())


func _build_first_turn_intro_config() -> Dictionary:
	return {
		"combat_cartoon_ui": combat_cartoon_ui,
		"total_enemy_health_bar": total_enemy_health_bar,
		"timeline_ui": timeline_ui,
	}


func _refresh_enemy_intents_on_timeline() -> void:
	_enemy_intent_timeline_refresher.refresh({
		"timeline_manager": timeline_manager,
		"tree": get_tree(),
	})


func _on_timeline_action_hovered(action: TimelineAction, is_hovering: bool):
	# 敌方意图 hover 现在由 EnemyIntentPresentationController 直接监听 TimelineManager 信号处理。
	# 这里对 ENEMY 类型直接返回，避免旧逻辑与新系统重复触发。
	if action and action.type == TimelineAction.Type.ENEMY:
		return

	_timeline_action_hover_ui_controller.handle_hover({
		"action": action,
		"is_hovering": is_hovering,
		"pulse_shader": pulse_shader,
		"cursor_tooltip": cursor_tooltip,
		"mouse_position": get_global_mouse_position(),
		"set_cursor_tooltip_position": Callable(self, "set_cursor_tooltip_position"),
	})


func setup_card_system():
	var result: Dictionary = await _card_system_bootstrap.setup(_build_card_system_bootstrap_config())
	manager_instance = result.get("manager_instance") as CardManager
	player_hand = result.get("player_hand") as Hand
	deck_pile = result.get("deck_pile") as Pile
	discard_pile = result.get("discard_pile") as Pile

	update_counts_and_ui()
	_card_system_ready = bool(result.get("ready", false))


func _build_card_system_bootstrap_config() -> Dictionary:
	return {
		"owner": self,
		"manager_scene": CARD_MANAGER_SCENE,
		"card_factory_scene": CARD_FACTORY_SCENE,
		"hand_scene": hand_scene,
		"pile_scene": pile_scene,
		"card_scene_ref": card_scene_ref,
		"card_size": card_size,
		"hand_x_position_ratio": hand_x_position_ratio,
		"hand_y_position_ratio": hand_y_position_ratio,
		"hand_y_offset": hand_y_offset,
		"deck_button": deck_button,
		"deck_gui_input": _on_deck_button_gui_input,
		"discard_button": discard_button,
		"discard_pressed": _on_discard_button_pressed,
		"end_turn_button": end_turn_button,
		"end_turn_pressed": _on_end_turn_pressed,
		"shop_button": shop_button,
		"shop_pressed": _on_shop_button_pressed,
		"acquire_reward_button": acquire_reward_button,
		"acquire_reward_pressed": _on_acquire_reward_button_pressed,
		"remove_reward_button": remove_reward_button,
		"remove_reward_pressed": _on_remove_reward_button_pressed,
		"craft_reward_button": craft_reward_button,
		"craft_reward_pressed": _on_craft_reward_button_pressed,
		"connect_once": _connect_signal_once,
	}


# --- 按钮逻辑修正 ---
func update_counts_and_ui():
	var counts: Dictionary = _card_pile_ui_controller.update_counts(deck_pile, discard_pile, deck_count_label, discard_count_label)
	current_deck_count = int(counts.get("deck_count", 0))
	current_discard_count = int(counts.get("discard_count", 0))


# --- 新增：抽牌堆的输入处理 (左键抽牌，右键查看) ---
func _on_deck_button_gui_input(event: InputEvent):
	var result: Dictionary = _card_pile_ui_controller.resolve_deck_button_action(event, {
		"is_processing_deck": is_processing_deck,
		"is_settlement": current_battle_state == BattleFlowState.SETTLEMENT,
		"enable_left_click_draw_from_deck": enable_left_click_draw_from_deck,
		"left_click_deck_draw_count": left_click_deck_draw_count,
	})

	var action: String = String(result.get("action", CARD_PILE_UI_CONTROLLER.ACTION_NONE))
	if action == CARD_PILE_UI_CONTROLLER.ACTION_OPEN_DECK:
		_open_deck_pile_viewer()
	elif action == CARD_PILE_UI_CONTROLLER.ACTION_DRAW_FROM_DECK:
		attempt_draw_cards(int(result.get("draw_count", left_click_deck_draw_count)))

	if bool(result.get("handled", false)):
		get_viewport().set_input_as_handled()

func _on_discard_button_pressed():
	_card_pile_ui_controller.open_discard_viewer(discard_pile, manager_instance, pile_viewer)


func _open_deck_pile_viewer() -> void:
	var is_settlement := current_battle_state == BattleFlowState.SETTLEMENT
	_card_pile_ui_controller.open_deck_viewer(deck_pile, manager_instance, pile_viewer, is_settlement)

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
	if is_instance_valid(hex_map):
		hex_map.process_turn_start_statuses()

	# 首回合进入局内时不额外推进阶段；后续回合开始才推进全局阶段。
	if advance_phase:
		_advance_global_phase()
	_refresh_combat_cartoon_ui_progress()

	# 玩家先抽 5 张牌，抽牌/洗牌动画完成后再生成敌方意图。
	await attempt_draw_cards(5)

	_refresh_enemy_intents_on_timeline()

	if Signal_Bus:
		Signal_Bus.emit_turn_started(current_era_value)

	# 恢复 UI
	enable_player_inputs()


func _advance_global_phase() -> void:
	var progress: Dictionary = _global_clock_bridge.advance_phase()
	current_era_value = int(progress.get("era", 1))


# ==========================================
# ★ 新增修复：补齐缺失的玩家输入锁定/解锁方法
# ==========================================
# 这些函数在结算回合时极其重要，它们能切断玩家的任何鼠标操作（拖拽、抽牌），防止逻辑穿插崩溃
func disable_player_inputs():
	_input_lock_controller.disable(_build_input_lock_config())


func enable_player_inputs():
	_input_lock_controller.enable(_build_input_lock_config())


func _build_input_lock_config() -> Dictionary:
	return {
		"player_hand": player_hand,
		"deck_button": deck_button,
		"discard_button": discard_button,
		"end_turn_button": end_turn_button,
	}


func discard_all_hand_cards():
	await _hand_discard_flow_controller.discard_all({
		"player_hand": player_hand,
		"discard_pile": discard_pile,
		"hide_tooltip": Callable(self, "hide_tooltip"),
		"tree": get_tree(),
	})
	update_counts_and_ui()


# --- 抽牌逻辑 ---
func attempt_draw_cards(count: int):
	# ★ 拦截：如果锁正在开启，说明前一次抽牌/洗牌 await 还没结束，直接无视请求
	if is_processing_deck:
		return

	# ★ 上锁
	is_processing_deck = true
	await _card_draw_flow_controller.draw_cards(_build_card_draw_flow_config(count))
	is_processing_deck = false

# --- 弃牌判定逻辑 (拖拽松手) ---
func _input(event):
	var input_result: Dictionary = _input_event_controller.handle_input(event, _build_input_event_config())
	var action: String = str(input_result.get("action", "none"))
	if action == "handled":
		get_viewport().set_input_as_handled()
		return
	if action == "discard_card":
		var card: Variant = input_result.get("card")
		if is_instance_valid(card):
			if card.has_method("change_state"):
				card.change_state(0)  # 0 对应 State.IDLE
			hide_tooltip()
			discard_pile.call_deferred("move_cards", [card])
			_handle_discard_effects(card)
			await get_tree().process_frame
			update_counts_and_ui()


func _build_input_event_config() -> Dictionary:
	return {
		"enable_keyboard_timecoin_debug": enable_keyboard_timecoin_debug,
		"keyboard_timecoin_debug_amount": keyboard_timecoin_debug_amount,
		"global_timecoin": GlobalTimecoin,
		"drag_controller": get_tree().get_first_node_in_group("DragShapeController"),
		"manager_instance": manager_instance,
		"hide_tooltip": Callable(self, "hide_tooltip"),
		"player_hand": player_hand,
		"mouse_position": get_global_mouse_position(),
		"screen_size": get_viewport_rect().size,
		"drop_area": drop_area,
	}


# 洗牌逻辑
func shuffle_card():
	await _card_draw_flow_controller.shuffle_cards(_build_card_draw_flow_config(0))


func _build_card_draw_flow_config(count: int) -> Dictionary:
	return {
		"count": count,
		"hand_limit": 7,
		"player_hand": player_hand,
		"deck_pile": deck_pile,
		"discard_pile": discard_pile,
		"tree": get_tree(),
		"signal_bus": Signal_Bus,
		"disable_player_inputs": Callable(self, "disable_player_inputs"),
		"enable_player_inputs": Callable(self, "enable_player_inputs"),
		"update_counts_and_ui": Callable(self, "update_counts_and_ui"),
		"draw_interval": 0.1,
		"shuffle_delay": 0.6,
	}


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
	card_tooltip_presenter = _card_tooltip_ui_adapter.ensure_presenter(self, card_tooltip_presenter, tooltip_config)


## 保留旧接口名，避免其它脚本调用链重写。
## 现在它只负责初始化 presenter，而不再在本文件里手工拼 UI。
func setup_tooltip_ui():
	card_tooltip_presenter = _card_tooltip_ui_adapter.setup_ui(self, card_tooltip_presenter, tooltip_config)


# 显示多重词条与效果
func show_tooltip(card: Control):
	card_tooltip_presenter = _card_tooltip_ui_adapter.show_tooltip(self, card_tooltip_presenter, tooltip_config, card)


func hide_tooltip(card: Control = null):
	card_tooltip_presenter = _card_tooltip_ui_adapter.hide_tooltip(self, card_tooltip_presenter, tooltip_config, card)


# ==========================================
# ★ 新增修复：目标有效性检测与选中 Shader 控制
# ==========================================
# 校验玩家悬浮的地块是否满足当前举起卡牌的释放条件
func is_valid_target(hovered_stack: Area2D, active_card: Control) -> bool:
	return HEX_TARGET_RULES.is_stack_valid_target(
		hovered_stack,
		active_card,
		_build_hex_target_rules_context(hovered_stack)
	)


## 收集目标规则需要的地图上下文。
## MainBoard 只负责传递 HexMap 数据，不再复制建造、伤害、治疗等目标细则。
func _build_hex_target_rules_context(center_stack: Area2D) -> Dictionary:
	if not is_instance_valid(hex_map):
		return {}
	return {
		"center_coord": hex_map.stack_nodes.find_key(center_stack),
		"stack_nodes": hex_map.stack_nodes,
		"map_data": hex_map.map_data,
	}


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
	_target_selection_hover_ui_controller.update_hover({
		"cursor_tooltip": cursor_tooltip,
		"set_cursor_tooltip_position": Callable(self, "set_cursor_tooltip_position"),
		"mouse_position": get_global_mouse_position(),
		"is_valid_target": is_valid_target(hovered_stack, active_card),
		"active_card": active_card,
	})
		
func _on_end_combat_pressed():
	if current_battle_state != BattleFlowState.SETTLEMENT:
		return

	end_combat_button.disabled = true
	if Signal_Bus:
		Signal_Bus.emit_combat_ended()

	await _return_to_out_scene()


## 返回局外地图的统一入口。
## 这里做四件事：
## 1. 把局内维护的时代值同步回全局单例
## 2. 组装一份战斗返回 payload，供局外场景未来消费
## 3. 调用 DimMenu 做黑幕过渡
## 4. 以“实例化并切 current_scene”的方式回到 OutScene
func _return_to_out_scene() -> void:
	await _return_flow_controller.return_to_out_scene({
		"push_era_to_global": Callable(self, "_push_era_to_global"),
		"build_return_payload": Callable(self, "_build_combat_return_payload"),
		"scene_log": SceneLog,
		"map_state": MapState,
		"disable_player_inputs": Callable(self, "disable_player_inputs"),
		"hide_ui_for_external_scene": Callable(self, "hide_ui_for_external_scene"),
		"sound_manager": SoundManager,
		"dim": dim,
		"switch_scene_with_data": Callable(self, "_switch_scene_with_data"),
		"out_scene_path": out_scene_path,
	})


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
	return _return_payload_builder.build({
		"map_state": MapState,
		"global_timecoin": GlobalTimecoin,
		"global_db": GlobalDB,
		"battle_tag": incoming_battle_tag,
		"map_seed": incoming_map_seed,
		"era": current_era_value,
	})


## 通用场景切换函数。
## 注意：
## - 当前脚本挂在 ui/Main，而不是整张 in_scene 的根节点上
## - 因此不能简单地 queue_free(self)，而要释放 old_scene 根节点
func _switch_scene_with_data(path: String, payload: Variant = null) -> void:
	var packed_scene := _load_packed_scene_for_switch(path, "返回局外失败")
	if packed_scene == null:
		_recover_dim_after_failed_switch()
		return

	_scene_switch_executor.switch_to_scene({
		"tree": get_tree(),
		"packed_scene": packed_scene,
		"path": path,
		"payload": payload,
		"payload_bridge": _payload_bridge,
		"scene_log": SceneLog,
	})


## 用 ResourceLoader 加载 PackedScene，避免导出版 res:// 场景被 remap 后 FileAccess.file_exists() 误判。
func _load_packed_scene_for_switch(path: String, fail_message: String) -> PackedScene:
	return _scene_switch_loader.load_packed_scene(path, fail_message, SceneLog)


## 切场失败时把当前黑幕退回去，避免玩家停在全黑过渡层。
func _recover_dim_after_failed_switch() -> void:
	_scene_switch_loader.recover_dim_after_failed_switch(dim)


## 统一记录切场错误，导出版可在 user://logs/scene_flow.log 里定位。
func _log_scene_switch_error(message: String, extra: Dictionary = {}) -> void:
	_scene_switch_loader.log_error(message, extra, SceneLog)


## 在新场景进入树之前写入返回 payload。
## OutScene 会把 payload 暂存在 pending_external_event，等 _ready() 完成后再消费。
func _apply_payload_to_new_scene_before_tree(next_scene: Node, payload: Variant) -> void:
	_payload_bridge.apply_to_new_scene_before_tree(next_scene, payload)

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

	var scene_path: String = str(SETTLEMENT_REWARD_SCENE_PATHS[reward_type])
	if _get_settlement_reward_scene(scene_path) == null:
		push_error("无法加载局外收获场景：%s" % scene_path)
		return

	hide_ui_for_external_scene()
	active_settlement_reward_context = reward_context.duplicate()

	var result: Dictionary = _settlement_reward_scene_controller.open_reward_scene({
		"owner": self,
		"reward_type": reward_type,
		"reward_context": reward_context,
		"reward_scene_paths": SETTLEMENT_REWARD_SCENE_PATHS,
		"scene_cache": _settlement_reward_scene_cache,
		"manager_instance": manager_instance,
		"connect_close_callback": Callable(self, "_on_external_scene_exit_pressed"),
	})
	if bool(result.get("opened", false)):
		return

	match str(result.get("reason", "")):
		"unknown_reward_type":
			push_warning("未知的局外收获类型：%s" % reward_type)
		"scene_load_failed":
			push_error("无法加载局外收获场景：%s" % str(result.get("scene_path", "")))
		_:
			push_error("无法打开局外收获场景：%s" % str(result))

# 当局外界面点击离开/下一关时调用这个函数
func _get_settlement_reward_scene(scene_path: String) -> PackedScene:
	return _settlement_reward_scene_controller.get_reward_scene(scene_path, _settlement_reward_scene_cache)


func proceed_to_next_stage():
	current_battle_state = BattleFlowState.COMBAT

	# 把刚才隐藏的按钮全部恢复显示
	if is_instance_valid(deck_button): deck_button.show()
	if is_instance_valid(discard_button): discard_button.show()
	if is_instance_valid(end_turn_button): end_turn_button.show()
	if is_instance_valid(timeline_ui): timeline_ui.show()
	if is_instance_valid(player_hand): player_hand.show()
	if is_instance_valid(hex_map):
		hex_map.set_tiles_interactive(true)
		hex_map.set_visuals_locked(false)
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
	var parsed_payload: Dictionary = _external_payload_parser.parse(payload)
	incoming_external_payload = parsed_payload.get("raw_payload")
	incoming_battle_tag = str(parsed_payload.get("battle_tag", ""))
	incoming_map_seed = str(parsed_payload.get("map_seed", ""))
	if not bool(parsed_payload.get("has_payload", false)):
		return

	# 如果 Main 已经进入节点树，则立刻同步一次；
	# 如果切场时在 add_child() 前预注入，_ready 里的 deferred 会在 HexMap ready 后再补一次。
	if is_inside_tree():
		_apply_incoming_payload_to_hex_map()


func _apply_incoming_payload_to_hex_map() -> void:
	_payload_bridge.forward_to_hex_map(hex_map, incoming_external_payload, SceneLog)


func _ensure_entry_dim_hidden() -> void:
	if not is_instance_valid(dim):
		return

	var dim_rect := dim.get_node_or_null("ColorRect") as ColorRect
	if is_instance_valid(dim_rect) and dim_rect.color.a <= 0.001:
		dim.hide()

## 增强光标提示框，模仿卡牌文本框的样式
func _enhance_cursor_tooltip() -> void:
	cursor_tooltip_panel = _cursor_tooltip_controller.enhance(
		cursor_tooltip,
		tooltip_config,
		_on_cursor_tooltip_visibility_changed
	)
	

## 当cursor_tooltip可见性变化时，同步Panel的可见性
func _on_cursor_tooltip_visibility_changed() -> void:
	_cursor_tooltip_controller.sync_visibility(
		cursor_tooltip,
		cursor_tooltip_panel,
		_build_cursor_tooltip_size_config()
	)

## 设置光标提示框的位置（增强后使用Panel的位置）
func set_cursor_tooltip_position(position: Vector2) -> void:
	_cursor_tooltip_controller.set_position(
		cursor_tooltip,
		cursor_tooltip_panel,
		position,
		_build_cursor_tooltip_size_config()
	)


## 刷新光标提示框尺寸。
## 核心逻辑：先关闭自动换行测自然宽度，再按最小/最大宽度重排，避免中文被压成一字一行。
func _refresh_cursor_tooltip_size() -> void:
	_cursor_tooltip_controller.refresh_size(
		cursor_tooltip,
		cursor_tooltip_panel,
		_build_cursor_tooltip_size_config()
	)


func _build_cursor_tooltip_size_config() -> Dictionary:
	return {
		"min_width": cursor_tooltip_min_width,
		"max_width": cursor_tooltip_max_width,
		"min_height": cursor_tooltip_min_height,
	}


func _build_ui_visibility_config() -> Dictionary:
	return {
		"player_hand": player_hand,
		"deck_pile": deck_pile,
		"discard_pile": discard_pile,
		"deck_button": deck_button,
		"discard_button": discard_button,
		"end_turn_button": end_turn_button,
		"end_combat_button": end_combat_button,
		"height_view_toggle_button": height_view_toggle_button,
		"timeline_ui": timeline_ui,
		"cursor_tooltip": cursor_tooltip,
		"cursor_tooltip_panel": cursor_tooltip_panel,
		"shop_button": shop_button,
		"acquire_reward_button": acquire_reward_button,
		"remove_reward_button": remove_reward_button,
		"craft_reward_button": craft_reward_button,
		"total_enemy_health_bar": total_enemy_health_bar,
		"hex_map": hex_map,
		"timecoin_container": timecoin_container,
		"is_settlement": current_battle_state == BattleFlowState.SETTLEMENT,
		"show_settlement_debug_buttons": show_settlement_debug_buttons,
		"resolution_hides_debug_buttons": _resolution_hides_debug_buttons,
		"hide_tooltip": Callable(self, "hide_tooltip"),
		"prepare_deck_button_for_settlement": Callable(self, "_prepare_deck_button_for_settlement"),
	}


func _build_resolution_debug_buttons() -> Array:
	return [
		win_debug_button,
		combat_victory_debug_button,
		lose_button,
		shop_button,
		acquire_reward_button,
		remove_reward_button,
		craft_reward_button,
	]


## ==========================================
## ★ 局外场景UI管理函数
## ==========================================

## 隐藏除hexmap和时间币外的所有UI，为局外场景做准备
func hide_ui_for_external_scene():
	_ui_visibility_controller.hide_for_external_scene(_build_ui_visibility_config())


## 单体血条由 HexMap/BarManager 动态生成。
## 局外收获阶段要求不显示血条，所以这里统一转发给 BarManager。
func _set_single_health_bars_visible(is_visible: bool) -> void:
	_ui_visibility_controller.set_single_health_bars_visible(hex_map, is_visible)


## 结算期统一隐藏调试入口。
## 覆盖失败、胜利、旧奖励调试等按钮，防止结算界面露出开发控件。
func _hide_debug_buttons_for_resolution() -> void:
	_resolution_hides_debug_buttons = true
	_ui_visibility_controller.hide_debug_buttons(_build_resolution_debug_buttons())
	

func _refresh_height_view_toggle_button_after_external_scene() -> void:
	_ui_visibility_controller.refresh_height_view_toggle_after_external_scene(_build_ui_visibility_config())


## 退出局外场景后，恢复四个局外按钮
func restore_ui_after_external_scene():
	_ui_visibility_controller.restore_after_external_scene(_build_ui_visibility_config())
	

## 完全恢复所有UI（用于返回游戏主界面）
func restore_all_ui():
	_ui_visibility_controller.restore_all(_build_ui_visibility_config())
	

## 外部场景退出按钮回调
func _on_external_scene_exit_pressed(scene_instance: Node):
	var exit_result: Dictionary = _settlement_reward_exit_controller.close_reward_scene(scene_instance)

	# 1. 如果奖励页确认完成，则通知 HexMap 更新建筑状态。
	if bool(exit_result.get("should_consume_settlement_reward", false)):
		var reward_context: Dictionary = exit_result.get("reward_context", {})
		_consume_settlement_reward_context(reward_context)

	# 1. 恢复四个局外按钮
	restore_ui_after_external_scene()

	active_settlement_reward_context.clear()
	
	# 3. 可选：如果需要完全恢复所有UI，可以调用 restore_all_ui()
	# 但根据需求，只恢复四个局外按钮，其他UI保持隐藏


## 消耗一个建筑绑定的收获奖励。
## Main 只负责把“这个奖励已经被确认使用”的事实传回 HexMap；
## 具体高亮、tooltip 文案、碰撞关闭都由 HexMap 统一处理。
func _consume_settlement_reward_context(reward_context: Dictionary) -> void:
	_settlement_reward_consumer.consume(reward_context, hex_map)
	active_settlement_reward_context.clear()

# 信号响应：执行实际的动画转换逻辑
func _on_defeat_triggered():
	_combat_defeat_flow_controller.run_defeat_flow({
		"sound_manager": SoundManager,
		"hide_debug_buttons": Callable(self, "_hide_debug_buttons_for_resolution"),
		"hide_ui_for_external_scene": Callable(self, "hide_ui_for_external_scene"),
		"current_era_value": current_era_value,
		"global_timecoin": GlobalTimecoin,
		"game_over_ui": game_over_ui,
		"saver": Saver,
		"save_slot": 0,
	})

# 按钮点击：只负责发出全局信号
func _on_lose_button_pressed():
	Signal_Bus.emit_defeat_triggered()
	
func _on_combat_victory_debug_button_down() -> void:
	if Signal_Bus:
		Signal_Bus.emit_combat_victory_triggered()

func _on_win_button_button_down() -> void:
	_game_win_flow_controller.run_win_flow({
		"sound_manager": SoundManager,
		"hide_debug_buttons": Callable(self, "_hide_debug_buttons_for_resolution"),
		"win_screen": win,
		"sfx_name": "game_win",
	})


func _hide_settlement_buttons() -> void:
	_ui_visibility_controller.hide_settlement_buttons(_build_ui_visibility_config())


func _show_settlement_buttons() -> void:
	_ui_visibility_controller.show_settlement_buttons(_build_ui_visibility_config())


func _hide_combat_phase_ui_for_settlement() -> void:
	_ui_visibility_controller.hide_combat_phase_for_settlement(_build_ui_visibility_config())


func _prepare_deck_button_for_settlement(reclaim_runtime_cards: bool = false) -> void:
	_snapshot_current_deck_for_settlement()
	is_processing_deck = false
	if reclaim_runtime_cards:
		_reclaim_all_runtime_cards_to_deck()
	update_counts_and_ui()

	if is_instance_valid(deck_button):
		deck_button.show()
		deck_button.mouse_filter = Control.MOUSE_FILTER_PASS
	if is_instance_valid(discard_button):
		discard_button.hide()


func _snapshot_current_deck_for_settlement() -> void:
	_settlement_deck_snapshot_service.snapshot_current_deck(GlobalDB, MapState)


func _reclaim_all_runtime_cards_to_deck() -> void:
	var search_root: Node = get_tree().current_scene if is_instance_valid(get_tree().current_scene) else self
	var drag_controller: Node = get_tree().get_first_node_in_group("DragShapeController")
	_settlement_deck_reclaim_service.reclaim({
		"manager_instance": manager_instance,
		"player_hand": player_hand,
		"discard_pile": discard_pile,
		"deck_pile": deck_pile,
		"search_root": search_root,
		"drag_controller": drag_controller,
	})


func _on_combat_victory_triggered() -> void:
	if current_battle_state != BattleFlowState.COMBAT:
		return

	current_battle_state = BattleFlowState.SETTLEMENT
	await _combat_victory_settlement_controller.enter_settlement({
		"sound_manager": SoundManager,
		"hide_debug_buttons": Callable(self, "_hide_debug_buttons_for_resolution"),
		"disable_player_inputs": Callable(self, "disable_player_inputs"),
		"hide_combat_phase_ui": Callable(self, "_hide_combat_phase_ui_for_settlement"),
		"prepare_deck_button_for_settlement": Callable(self, "_prepare_deck_button_for_settlement"),
		"timeline_manager": timeline_manager,
		"hex_map": hex_map,
		"reward_host": self,
		"total_enemy_health_bar": total_enemy_health_bar,
		"set_single_health_bars_visible": Callable(self, "_set_single_health_bars_visible"),
		"combat_victory_banner": combat_victory_banner,
		"show_settlement_buttons": Callable(self, "_show_settlement_buttons"),
		"banner_text": "战斗胜利",
	})

func _on_timeline_action_executed(_action: TimelineAction) -> void:
	SoundManager.play_sfx("tile_damage")
