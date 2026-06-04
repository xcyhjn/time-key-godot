# 原文件名: hex_map(地块生成).gd
# 功能: 地块生成与战斗地图管理
extends Node2D
class_name battle

var rng := RandomNumberGenerator.new()

const HEX_COORD_RULES := preload("res://scene/in_scene/hex_map_modules/rules/HexCoordRules.gd")
const HEX_TERRAIN_RULES := preload("res://scene/in_scene/hex_map_modules/rules/HexTerrainRules.gd")
const HEX_TARGET_RULES := preload("res://scene/in_scene/hex_map_modules/rules/HexTargetRules.gd")
const TILE_DESTRUCTION_BATCH_QUEUE := preload("res://scene/in_scene/hex_map_modules/destruction/TileDestructionBatchQueue.gd")
const TILE_DESTRUCTION_MUTATION_SERVICE := preload("res://scene/in_scene/hex_map_modules/destruction/TileDestructionMutationService.gd")
const ENEMY_INTENT_MAP_PRESENTER := preload("res://scene/in_scene/hex_map_modules/presenters/EnemyIntentMapPresenter.gd")
const SETTLEMENT_REWARD_PRESENTER := preload("res://scene/in_scene/hex_map_modules/presenters/SettlementRewardPresenter.gd")
const HEX_MAP_COLLISION_PRESENTER := preload("res://scene/in_scene/hex_map_modules/presenters/HexMapCollisionPresenter.gd")
const HEX_MAP_VISUAL_STATE_PRESENTER := preload("res://scene/in_scene/hex_map_modules/presenters/HexMapVisualStatePresenter.gd")
const TARGET_AOE_HOVER_PRESENTER := preload("res://scene/in_scene/hex_map_modules/presenters/TargetAoeHoverPresenter.gd")
const HEIGHT_VIEW_INDICATOR_PRESENTER := preload("res://scene/in_scene/hex_map_modules/height_view/HeightViewIndicatorPresenter.gd")
const RUNTIME_LANDFORM_REGISTRAR := preload("res://scene/in_scene/hex_map_modules/registrars/RuntimeLandformRegistrar.gd")
const EXTERNAL_RENDER_NODE_REGISTRAR := preload("res://scene/in_scene/hex_map_modules/registrars/ExternalRenderNodeRegistrar.gd")
const HEIGHT_VIEW_STATE_SYNCHRONIZER := preload("res://scene/in_scene/hex_map_modules/height_view/HeightViewStateSynchronizer.gd")
const HEIGHT_VIEW_MAP_TRANSITION_RUNNER := preload("res://scene/in_scene/hex_map_modules/height_view/HeightViewMapTransitionRunner.gd")
const MAP_INTRO_REVEAL_RUNNER := preload("res://scene/in_scene/hex_map_modules/runners/MapIntroRevealRunner.gd")
const TILE_ELEVATION_SERVICE := preload("res://scene/in_scene/hex_map_modules/elevation/TileElevationService.gd")
const HEX_MAP_INPUT_COORDINATOR := preload("res://scene/in_scene/hex_map_modules/input/HexMapInputCoordinator.gd")
const TARGET_HOVER_CONTROLLER := preload("res://scene/in_scene/hex_map_modules/input/TargetHoverController.gd")
const TILE_STACK_FACTORY := preload("res://scene/in_scene/hex_map_modules/factory/TileStackFactory.gd")
const TILE_LANDFORM_ATTACH_SERVICE := preload("res://scene/in_scene/hex_map_modules/factory/TileLandformAttachService.gd")
const TILE_STACK_INITIALIZATION_SERVICE := preload("res://scene/in_scene/hex_map_modules/factory/TileStackInitializationService.gd")
const CARD_MANAGER_LOCATOR := preload("res://scene/in_scene/hex_map_modules/bridges/CardManagerLocator.gd")
const TILE_TURN_BEHAVIOR_RUNNER := preload("res://scene/in_scene/hex_map_modules/turn/TileTurnBehaviorRunner.gd")
const ENEMY_INTENT_FRAME_TEXTURE: Texture2D = preload("res://image/texture/hexagon_frame.png")
const ENEMY_INTENT_TARGET_SHADER: Shader = preload("res://shaders/enemy_intent_target_ripple.gdshader")
#血条信号测试用
signal CreateBar(landform_in: landform, situation: int, x: float, y: float)
signal enemy_roster_changed
signal tile_topology_changed
signal settlement_reward_requested(reward_info: Dictionary)
signal map_intro_reveal_finished
@export_group("Assets")
@export var hex_top_tex: Texture2D
@export var hex_side_tex: Texture2D
@export var block_material: ShaderMaterial
@export var label_bg_tex: Texture2D

@export_group("地块入场动画")
## 是否在局内初次生成地图时播放“倒放湮灭”的涟漪入场。
## 关闭后会回到原本的瞬间生成，并且血条/总血量会立刻刷新。
@export var map_intro_reveal_enabled: bool = true
## 单个地块从完全消融到正常显示的时长。
@export var map_intro_reveal_tile_duration: float = 0.42
## 相邻涟漪层之间的时间差。数值越小，整张地图出现越快。
@export var map_intro_reveal_wave_delay: float = 0.07
## 用屏幕像素距离切分涟漪层。越小层数越多，波纹越细；越大则更像大片推进。
@export var map_intro_reveal_wave_pixel_step: float = 130.0
## 入场动画结束后额外等待一点时间，再放出血条和总血量刷新，避免 UI 抢在最后一层前出现。
@export var map_intro_reveal_finish_delay: float = 0.08
## 涟漪源点相对地图左下角额外外推的像素距离，用来控制第一圈从屏幕外还是角落内侧冒出。
@export var map_intro_reveal_origin_padding: Vector2 = Vector2(80.0, 80.0)
## 入场期间是否先隐藏单体血条、总血量等战斗辅助 UI。
@export var map_intro_reveal_hide_health_ui: bool = true
## 入场期间是否锁住地块鼠标交互，避免还未出现的格子被 hover/点击打断 shader 状态。
@export var map_intro_reveal_lock_interaction: bool = true
## 地块入场补间曲线类型。
@export var map_intro_reveal_trans_type: Tween.TransitionType = Tween.TRANS_SINE
## 地块入场缓动方向。
@export var map_intro_reveal_ease_type: Tween.EaseType = Tween.EASE_OUT

@export_group("敌人意图地图表现")
## 地图意图 overlay 统一使用的框体贴图。推荐使用透明背景的六边形描边。
@export var enemy_intent_frame_tex: Texture2D
## 目标波纹 overlay 使用的独立 shader。若不手动指定，运行时自动回退到默认 shader。
@export var enemy_intent_target_shader: Shader
## 地图意图 overlay 的整体缩放倍率，1.0 为紧贴 hitbox，略大于 1 可以让边框更清晰。
@export var enemy_intent_overlay_scale: float = 1.18
## 地图意图 overlay 的层级，确保高于普通地块贴图与建筑贴图。
@export var enemy_intent_overlay_z_index: int = 200
## 施法者高亮描边粗细。逻辑来源于原 hex shader 的 highlight_width。
@export var enemy_intent_source_highlight_width: float = 4.0
## 施法者普通有效意图时的高亮强度。
@export var enemy_intent_source_valid_highlight_blend: float = 0.85
## 施法者普通有效意图时的描边强度。
@export var enemy_intent_source_valid_selected_blend: float = 0.95
## 施法者覆盖自身时的高亮强度（通常更强、更明显）。
@export var enemy_intent_source_self_highlight_blend: float = 1.0
## 施法者覆盖自身时的描边强度。
@export var enemy_intent_source_self_selected_blend: float = 1.0
## 施法者意图无效时的高亮强度（灰态，应弱于正常意图）。
@export var enemy_intent_source_invalid_highlight_blend: float = 0.55
## 施法者意图无效时的描边强度。
@export var enemy_intent_source_invalid_selected_blend: float = 0.7
## 目标波纹的运动速度。
@export var enemy_intent_target_ripple_speed: float = 3.2
## 目标波纹的密度（越高波纹越密）。
@export var enemy_intent_target_ripple_density: float = 20.0
## 目标波纹最小透明度。
@export var enemy_intent_target_min_alpha: float = 0.2
## 目标波纹最大透明度。
@export var enemy_intent_target_max_alpha: float = 0.95

@export_group("Grid Generation Settings")
@export var map_generation_mode: int = 0  # 0=扇形战斗地图, 1=圆形随机地图

@export_subgroup("扇形战斗地图设置")
@export var nexus_height: int = 8
@export var fan_radius: int = 6
@export var inner_tier_radius: int = 3
@export var fan_angle_span: float = 150.0

@export_subgroup("圆形随机地图设置")
@export var map_radius: int = 4          # 地图半径：1=7格，2=19格，3=37格...
@export var base_h_min: int = 1
@export var base_h_max: int = 2
@export var elite_h_bonus: int = 1       # battle_elite 时整体高度上浮一点（可调）
@export var use_external_seed: bool = true

@export var max_generation_attempts: int = 20

@export_group("Grid Spacing & Hitbox")
@export var spacing_x: float = 168.24
@export var spacing_y: float = 96.24
@export var step_height: float = 48.0
@export var tile_scale: float = 0.6
## 是否启用“智能碰撞箱交互优化”：
## - 闲置态：只允许有敌人建筑的格子参与鼠标命中
## - 地块选择态：允许所有格子参与鼠标命中
## - 平铺视角与3D视角共用同一套规则
@export var enable_smart_collision_interaction: bool = true
@export var hitbox_width: float = 168.0
@export var hitbox_base_height: float = 60.0
@export var hitbox_offset_x: float = 0
@export var hitbox_offset_y: float = -110.0
## 怪物/地貌实体实例化后，挂到地块上的额外偏移。
## 只调整实体节点位置，不影响碰撞箱、地块贴图、意图 overlay。
@export var landform_instance_offset: Vector2 = Vector2(0.0, -20.0)
## 怪物/地貌实体内部贴图的额外偏移。
## 这个参数直接作用于 LandformSprite_*，用于微调真正看见的怪物/建筑图像位置；血条锚点会跟随该贴图。
@export var landform_sprite_offset: Vector2 = Vector2(0.0, -10.0)
## 运行期生成的地貌是否播放像素入场。
## village 扩张、radar 召唤等都走 register_runtime_landform()，这里统一控制它们的出现表现。
@export var runtime_landform_spawn_vfx_enabled: bool = true
## 运行期地貌像素入场耗时。
@export_range(0.01, 3.0, 0.01, "or_greater") var runtime_landform_spawn_vfx_duration: float = 0.45

## 六边形碰撞箱顶部横边相对于最大宽度的比例。
## 0.5 表示顶部横边宽度为整体最大宽度的一半。
@export_range(0.1, 1.0, 0.01) var hitbox_top_width_ratio: float = 0.5
## 碰撞箱的绘制/调试层级。
@export var hitbox_z_index: int = 4096

const REF_SCALE: float = 0.6

# ==========================================
# 高度视图配置（切换视角后的光柱系统）
# ==========================================
@export_group("高度视图配置")
@export var show_height_pillars: bool = true  # 是否生成光柱
@export var show_height_labels: bool = true   # 是否生成数字
@export var height_view_pillar_length: float = 50.0  # 光柱长度（向上延伸像素）
@export var height_view_pillar_width: float = 5.0    # 光柱粗细
@export var height_view_pillar_color: Color = Color(1.0, 1.0, 1.0, 0.8)  # 光柱颜色（白色带透明度）
@export var height_view_pillar_offset: Vector2 = Vector2(0, 0)  # 光柱相对方块的位置偏移

@export_subgroup("高度数字标签配置")
@export var height_label_font_size: int = 32  # 数字字体大小
@export var height_label_color: Color = Color(1.0, 1.0, 1.0, 1.0)  # 数字颜色
@export var height_label_outline_color: Color = Color(0.0, 0.0, 0.0, 1.0)  # 数字描边颜色
@export var height_label_outline_size: int = 2  # 数字描边粗细
@export var height_label_offset: Vector2 = Vector2(-10, -40)  # 数字位置偏移（相对光柱顶部）
@export var height_label_font: Font  # 数字字体（可选）

@export_subgroup("高度视图Shader配置")
@export var height_view_shader_material: ShaderMaterial  # 高度视图专用shader材质
@export var height_view_hover_speed: float = 1.5  # 鼠标悬浮时光柱上下移动速度
@export var height_view_hover_amplitude: float = 15.0  # 鼠标悬浮时光柱上下移动幅度
@export var height_view_click_highlight_color: Color = Color(1.0, 1.0, 1.0, 0.3)  # 点击时白色高亮颜色
@export var height_view_click_highlight_width: float = 4.0  # 点击时白色边框粗细

enum SettlementRewardBindState {
	DEAD,
	ALIVE
}

@export_group("局外收获配置")
## 收获入口绑定到哪类敌方建筑：
## - DEAD: 只绑定已经被打成 Broken 的敌人。
## - ALIVE: 保留旧逻辑，只绑定仍未损毁的敌人。
@export var settlement_reward_bind_state: SettlementRewardBindState = SettlementRewardBindState.DEAD
## 常驻 tooltip 相对地块顶面的偏移。
## x 用来手动水平居中；y 越小 tooltip 越靠上，适合不同尺寸建筑分别微调。
@export var settlement_reward_tooltip_offset: Vector2 = Vector2(-92.0, -230.0)
## 常驻 tooltip 固定尺寸。固定尺寸可以避免文本变化时撑开布局、造成位置跳动。
@export var settlement_reward_tooltip_size: Vector2 = Vector2(184.0, 44.0)
## 常驻 tooltip 字号。
@export var settlement_reward_tooltip_font_size: int = 18
## 常驻 tooltip 层级，必须高于地块和建筑，但仍然跟随地图相机移动。
@export var settlement_reward_tooltip_z_index: int = 3000
## 未使用收获建筑的常驻高亮颜色。
@export var settlement_reward_highlight_color: Color = Color(1.0, 0.78, 0.18, 1.0)
## 鼠标悬浮时叠加的亮色。这里偏白，用来和常驻金色区分。
@export var settlement_reward_hover_color: Color = Color(1.0, 1.0, 1.0, 1.0)
## 常驻金色蒙版强度，只负责变亮，不推动地块悬浮。
@export_range(0.0, 1.0, 0.01) var settlement_reward_highlight_blend: float = 0.65
## 鼠标悬浮时接入 hex shader 顶点上浮的强度。
@export_range(0.0, 1.0, 0.01) var settlement_reward_hover_blend: float = 1.0
## Tooltip hover 时的放大倍率。
@export var settlement_reward_tooltip_hover_scale: Vector2 = Vector2(1.08, 1.08)

# ==========================================
# 地形系统（整合自 node_2d.gd）
# ==========================================
enum TerrainType { BEACH, PLAINS, HILLS, MOUNTAIN }
enum LandformType { NONE, MINE, CAVE, VILLAGE, RUINS }  # 整合自 node_2d.gd，用于兼容性

@export_group("地形系统")
@export var terrain_by_height := {
	1: TerrainType.BEACH,
	2: TerrainType.PLAINS,
	3: TerrainType.PLAINS,
	4: TerrainType.HILLS,
	5: TerrainType.MOUNTAIN,
	6: TerrainType.MOUNTAIN,
}
@export var top_tex_by_terrain: Array[Texture2D]  # 下标用 TerrainType
@export var side_tex_by_terrain: Array[Texture2D]
@export var max_landform_ratio: float = 0.8  # 最多35%格子有地貌（可调）

@export_group("地块生成配额")
#@export var Max_Start_landform = 10 ##初始地块数量
@export var Max_Neutral_landform: int = 10
@export var Max_Enemy_landform: int = 12
# ==========================================
# 地形升降动画配置
# ==========================================
@export_group("地形升降动画 (Elevation Animation)")
@export var ele_anim_duration: float = 0.15  # 升降过程的耗时，调小可加速地块高度变化。
@export var ele_shake_intensity: float = 6.0 # 升降前地壳震动的像素幅度
@export var ele_shake_duration: float = 0.08  # 地壳震动的准备时间，调小可减少升降前摇。
@export var ele_trans_type: Tween.TransitionType = Tween.TRANS_ELASTIC # 弹性缓冲，效果最好
@export var ele_ease_type: Tween.EaseType = Tween.EASE_OUT
@export var elevation_move_distance: float = 48.0 #地块升降高度控制
# ★ 新增：控制物理补块（侧面贴图）生成时的垂直间距
@export var filler_block_spacing: float = 48.0
## 时间轴等待升降/崩塌表现时额外预留的缓冲，避免下一格过早结算。
@export var elevation_resolution_padding: float = 0.05

@export_group("高度限制设置")
@export var max_height: int = 6  ## 超过此高度地块会崩塌
@export var min_height: int = 0  ## 低于此高度地块会湮灭

@export_group("地块消失动画")
## 同一批最多同时消失的地块数量。默认 2，用于让大范围超限时两两崩塌。
@export_range(1, 12, 1, "or_greater") var tile_destruction_batch_size: int = 3
## 每批消失前额外等待一小段时间收集同一波超限地块，避免刚好错帧时只消失 1 块。
@export_range(0.0, 1.0, 0.01, "or_greater") var tile_destruction_batch_collect_delay: float = 0.06
## 消失前左右抖动次数。调小可以加速崩塌表现。
@export_range(0, 20, 1) var tile_destruction_shake_count: int = 3
## 单次左右抖动的耗时。
@export_range(0.0, 1.0, 0.01, "or_greater") var tile_destruction_shake_step_duration: float = 0.015
## 消失前左右抖动的像素幅度。
@export var tile_destruction_shake_distance: float = 15.0
## 像素溶解消失的耗时。
@export_range(0.01, 3.0, 0.01, "or_greater") var tile_destruction_dissolve_duration: float = 0.25
## 每组消失批次之间的间隔。默认 0，两两接力时更快。
@export_range(0.0, 2.0, 0.01, "or_greater") var tile_destruction_batch_interval: float = 0.0

# ==========================================
# ★ 视觉状态机定义
# ==========================================
enum TileVisualState {
	IDLE, HOVER_TARGET_VALID, HOVER_TARGET_INVALID, AOE_RANGE
}

# ★ 新增：全局视角状态机
enum MapViewState { 
	VIEW_3D, 
	VIEW_FLAT 
}
var current_view_state: MapViewState = MapViewState.VIEW_3D

# ==========================================
# ★ 局外收获状态机
# ==========================================
enum SettlementRewardMode {
	DISABLED,
	AVAILABLE
}
var current_settlement_reward_mode: SettlementRewardMode = SettlementRewardMode.DISABLED
var settlement_reward_host: Node = null
var settlement_reward_stacks: Array[Area2D] = []
var settlement_reward_stack_data: Dictionary = {}
var settlement_reward_hovered_stack: Area2D = null

var current_aoe_stacks: Array[Area2D] = []
var landform_pool: Array[Script] = []  # 地貌脚本池，在 _ready 中初始化
var neutral_pool: Array[Script] = []
var enemy_pool: Array[Script] = []

var map_data: Dictionary = { }
# ★ 优化：新增栈缓存字典，方便 O(1) 查找遮挡物
var stack_nodes: Dictionary = { }
var height_view_original_materials: Dictionary = {} #优化后字典，上面那个可能可以删除 TODO 
#var height_view_compressed: bool = false  # 视角切换状态：是否压缩为1格高度
var height_view_hovered_stack: Area2D = null  # 高度视图下鼠标悬浮的地块
var height_view_selected_stack: Area2D = null  # 高度视图下点击选中的地块
#var height_view_original_materials: Dictionary = {}  # 存储地块原始材质（用于恢复）
var is_view_transitioning: bool = false  # ★ 新增：动画状态锁
var is_visuals_locked: bool = false  # ★ 新增：视觉状态锁，防止拖拽时清除高亮和消融效果
# ==========================================
# 外部事件处理系统（整合自 node_2d.gd）
## 外部事件设置器（整合自 node_2d.gd）
func set_received_text(v: String) -> void:
	received_text = v.strip_edges()
	parts = received_text.split("|", false)

	# 局外正式流程传入格式是 "battle_elite <map_seed>"；
	# 旧调试流程可能仍使用 "battle_elite|enhance,combine"。
	# 这里先拆功能后缀，再拆空格 seed，保证 HexMap 在 _ready 建图前能拿到纯 room_type。
	var room_and_seed := parts[0].strip_edges() if parts.size() > 0 else ""
	var room_tokens := room_and_seed.split(" ", false)
	room_type = room_tokens[0] if room_tokens.size() > 0 else ""
	incoming_map_seed = room_tokens[1] if room_tokens.size() > 1 else ""

	if parts.size() > 1 and parts[1] != "":
		features = parts[1].split(",", false)
	else:
		features = PackedStringArray()


var received_text: String = "" : set = set_received_text
var parts: PackedStringArray = PackedStringArray()
var room_type: String = ""
var incoming_map_seed: String = ""
var features: PackedStringArray = PackedStringArray()

var map_root: Node2D
var hovered_stacks: Array[Area2D] = []
var active_stack: Area2D = null
var selected_stack: Area2D = null  # 记录当前被点击选中的地块
var currently_occluding_stacks: Array[Area2D] = []  # 记录当前处于透明湮灭状态的地块
var _enemy_intent_map_presenter := ENEMY_INTENT_MAP_PRESENTER.new()
var _settlement_reward_presenter := SETTLEMENT_REWARD_PRESENTER.new()
var _hex_map_collision_presenter := HEX_MAP_COLLISION_PRESENTER.new()
var _hex_map_visual_state_presenter := HEX_MAP_VISUAL_STATE_PRESENTER.new()
var _target_aoe_hover_presenter := TARGET_AOE_HOVER_PRESENTER.new()
var _height_view_indicator_presenter := HEIGHT_VIEW_INDICATOR_PRESENTER.new()
var _runtime_landform_registrar := RUNTIME_LANDFORM_REGISTRAR.new()
var _external_render_node_registrar := EXTERNAL_RENDER_NODE_REGISTRAR.new()
var _height_view_state_synchronizer := HEIGHT_VIEW_STATE_SYNCHRONIZER.new()
var _height_view_map_transition_runner := HEIGHT_VIEW_MAP_TRANSITION_RUNNER.new()
var _map_intro_reveal_runner := MAP_INTRO_REVEAL_RUNNER.new()
var _tile_elevation_service := TILE_ELEVATION_SERVICE.new()
var _tile_destruction_mutation_service := TILE_DESTRUCTION_MUTATION_SERVICE.new()
var _hex_map_input_coordinator := HEX_MAP_INPUT_COORDINATOR.new()
var _target_hover_controller := TARGET_HOVER_CONTROLLER.new()
var _tile_stack_factory := TILE_STACK_FACTORY.new()
var _tile_landform_attach_service := TILE_LANDFORM_ATTACH_SERVICE.new()
var _tile_stack_initialization_service := TILE_STACK_INITIALIZATION_SERVICE.new()
var _card_manager_locator := CARD_MANAGER_LOCATOR.new()
var _tile_turn_behavior_runner := TILE_TURN_BEHAVIOR_RUNNER.new()
## 鼠标碰撞总开关，拖拽/结算阶段会优先关闭它。
var _tiles_interactive_master_enabled: bool = true
## 记录上一帧是否处于地块选择态，仅在状态变化时刷新碰撞开关。
var _last_target_selection_active: bool = false
## 地图初始入场动画状态包。
## Runner 只读写这份状态，HexMap 继续通过旧公共函数给 BarManager、教程和战斗流程暴露查询入口。
var _map_intro_reveal_state: Dictionary = {
	"is_active": false,
	"has_played": false,
	"pending_requests": [],
	"pending_request_ids": {}
}
var _tile_destruction_queue := TILE_DESTRUCTION_BATCH_QUEUE.new()


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false

## 应用外部事件（整合自 node_2d.gd）
func apply_external_event(payload: String) -> void:
	# payload 示例："battle_elite|enhance,combine"
	received_text = payload  # 会触发 setter，自动更新 room_type/features
	if SceneLog:
		SceneLog.scene_event("HexMap", "apply_external_event", {"payload": payload, "room_type": room_type})

## 处理事件逻辑（整合自 node_2d.gd）
func handle_event_logic():
	# 这里你可以根据接收到的字符串，修改场景的背景颜色、背景音乐或生成参数
	match room_type:
		"battle_normal":
			print(">> 准备：普通战斗环境")
		"battle_elite":
			print(">> 准备：精英挑战环境（地块可能更高）")
		"boss_stage":
			print(">> 准备：BOSS 战环境")

func _ready():
	if SceneLog:
		SceneLog.scene_event("HexMap", "ready", {"received_text": received_text, "room_type": room_type})
	y_sort_enabled = true
	set_process(enable_smart_collision_interaction)
	if enemy_intent_frame_tex == null:
		enemy_intent_frame_tex = ENEMY_INTENT_FRAME_TEXTURE
	if enemy_intent_target_shader == null:
		enemy_intent_target_shader = ENEMY_INTENT_TARGET_SHADER
	# 连接到建筑行为信号
	Signal_Bus.step_next.connect(_on_step_next)
	
	# 打印接收到的信息，方便调试（整合自 node_2d.gd）
	if parts.size() > 1:
		var features = parts[1].split(",")
		# features 将会是 ["enhance", "combine"]
		for feature in features:
			print("该房间拥有特性:", feature)
	print("[HexMap 场景] 已进入，接收到的事件类型为: ", room_type)
	
	map_root = Node2D.new()
	map_root.y_sort_enabled = true
	add_child(map_root)
	
	#以下为两种方式测试用，可能删除第一个
	## 初始化地貌池
	#landform_pool = [
		#preload("res://scene/in_scene/enermy/village.gd"),
		## 可在此添加更多地貌类型
		#preload("res://scene/in_scene/enermy/blockhouse.gd")
	#]
	#★ 第二版 拆分池子
	neutral_pool = [
		preload("res://scene/in_scene/enermy/background.gd") # 你的中立地形脚本
	]
	
	enemy_pool = [
		#preload("res://scene/in_scene/enermy/tower.gd"),
		preload("res://scene/in_scene/enermy/Animal_husbandry.gd"),
		preload("res://scene/in_scene/enermy/iron_mine.gd"),
		preload("res://scene/in_scene/enermy/village.gd"),
		preload("res://scene/in_scene/enermy/blockhouse.gd"),
		preload("res://scene/in_scene/enermy/altar.gd"),
		#preload("res://scene/in_scene/enermy/radar.gd"),
		#preload("res://scene/in_scene/enermy/center_altar.gd")
	]
	var screen_size = get_viewport_rect().size
	map_root.position = Vector2(screen_size.x * 0.5, screen_size.y * 0.5)

	# 2. 根据不同的事件类型，初始化不同的视觉效果（可选）
	handle_event_logic()

	build_map_pipeline()
	if SceneLog:
		SceneLog.scene_event("HexMap", "build_map_pipeline finished", {"room_type": room_type, "map_tiles": map_data.size()})
	# 绑定现有高度视图切换按钮
	var height_view_button = get_node_or_null("../../ui/HeightViewToggleButton")
	
	# 双保险：通过当前场景根节点寻找
	if not height_view_button and get_tree().current_scene:
		height_view_button = get_tree().current_scene.get_node_or_null("ui/HeightViewToggleButton")
		
	if height_view_button:
		height_view_button.pressed.connect(_on_height_view_toggle_pressed)
	else:
		pass

	_refresh_stack_interactivity()


func build_map_pipeline():
	var is_valid = false
	var attempts = 0
	while not is_valid and attempts < max_generation_attempts:
		attempts += 1
		map_data.clear()
		_generate_map_data()
		is_valid = _check_map_validity()

	_assign_terrains_and_enemies()
	_begin_map_intro_reveal_if_needed()
	_render_map()
	_refresh_stack_interactivity()

	if is_map_intro_reveal_active():
		_set_intro_auxiliary_visuals_visible(false)
		_play_map_intro_reveal()
	else:
		_set_intro_auxiliary_visuals_visible(true)
		enemy_roster_changed.emit()


func _process(_delta: float) -> void:
	if not enable_smart_collision_interaction:
		return

	var is_target_selection_active = _is_target_selection_active()
	if is_target_selection_active != _last_target_selection_active:
		_last_target_selection_active = is_target_selection_active
		_refresh_stack_interactivity()


func _generate_map_data():
	match map_generation_mode:
		0: # 扇形战斗地图
			_generate_fan_map_data()
		1: # 圆形随机地图
			_generate_circular_map_data()
		_:
			push_error("无效的地图生成模式: " + str(map_generation_mode))
			_generate_fan_map_data()  # 默认回退

## 扇形坐标采样器 - 返回满足角度范围的六边形坐标
func _get_fan_coords(radius: int, angle_span: float, center_angle: float = 90.0) -> Array[Vector2i]:
	return HEX_COORD_RULES.get_fan_coords(radius, angle_span, center_angle, spacing_x, spacing_y, tile_scale / REF_SCALE)


## 圆形坐标采样器 - 返回圆形区域内的六边形坐标
func _get_circular_coords(radius: int) -> Array[Vector2i]:
	return HEX_COORD_RULES.get_circular_coords(radius)


## 统一数据填充管线 - 将坐标数组转换为地图数据
## @param coords 六边形坐标数组（Vector2i）
## @param shape_type 形状类型："fan" 或 "circular"
## @param shape_params 形状参数（如 angle_span, radius 等）
func _populate_map_data(coords: Array[Vector2i], shape_type: String, shape_params: Dictionary = {}) -> void:
	# 初始化随机数生成器
	if use_external_seed and received_text != "":
		var seed_text := incoming_map_seed if incoming_map_seed != "" else received_text
		rng.seed = seed_text.hash()
	else:
		rng.randomize()
	
	# 清空现有地图数据
	map_data.clear()
	
	# 特殊处理：扇形地图的核心地块
	if shape_type == "fan":
		# 确保中心坐标 (0,0) 作为 nexus_core（使用 Vector2i 统一键类型）
		var center_coord = Vector2i(0, 0)
		map_data[center_coord] = {
			"height": nexus_height,
			"tier": 3,
			"terrain": "nexus_core",
			"terrain_type": "nexus_core",
			"landform": null
		}
	
	# 遍历所有坐标，填充高度和地形
	for coord_v2i in coords:
		# 使用 Vector2i 作为键，统一坐标类型
		var height: int = 0
		var tier: int = 1
		var terrain = ""  # 可存储 String 或 TerrainType 枚举
		
		# 根据形状类型计算高度和 tier
		match shape_type:
			"fan":
				tier = HEX_TERRAIN_RULES.get_fan_tier(coord_v2i, inner_tier_radius)
				height = _roll_height_by_tier_with_rng(tier, rng)
				# 地形根据高度直接计算
				terrain = get_terrain_from_height(height)
			
			"circular":
				height = HEX_TERRAIN_RULES.roll_circular_height_with_rng(room_type, base_h_min, base_h_max, elite_h_bonus, rng)
				# 根据高度获取地形类型
				terrain = get_terrain_from_height(height)
			
			_:
				push_error("未知的形状类型: " + shape_type)
				continue
		
		# 构建地块数据
		var tile_data = {
			"height": height,
			"terrain": terrain,
			"terrain_type": terrain,
			"landform": null
		}
		
		# 扇形地图额外存储 tier 信息
		if shape_type == "fan":
			tile_data["tier"] = tier
		
		map_data[coord_v2i] = tile_data
	


## 带随机数生成器的 tier 高度滚动（用于可重复生成）
func _roll_height_by_tier_with_rng(tier: int, rng: RandomNumberGenerator) -> int:
	return HEX_TERRAIN_RULES.roll_height_by_tier_with_rng(tier, rng)


func _generate_fan_map_data():
	# 使用形状发生器模式：坐标采样 + 数据填充
	var coords = _get_fan_coords(fan_radius, fan_angle_span, 90.0)
	_populate_map_data(coords, "fan", {})

func _generate_circular_map_data():
	# 使用形状发生器模式：坐标采样 + 数据填充
	var coords = _get_circular_coords(map_radius)
	_populate_map_data(coords, "circular", {})

func _roll_height_by_tier(tier: int) -> int:
	return HEX_TERRAIN_RULES.roll_height_by_tier(tier)


func _check_map_validity() -> bool: return true  # 遮挡由于有了透视效果，不一定需要驳回重成了


## 根据高度获取地形类型
func get_terrain_from_height(h: int) -> TerrainType:
	return HEX_TERRAIN_RULES.terrain_from_height(
		h,
		terrain_by_height,
		TerrainType.BEACH,
		TerrainType.PLAINS,
		TerrainType.HILLS,
		TerrainType.MOUNTAIN
	) as TerrainType
# ==========================================
# 2. 配额抽取逻辑
# ==========================================

# ==========================================
# 1. 核心分配逻辑 (严格遵循 35% 密度限制)
# ==========================================
func _assign_terrains_and_enemies():
	rng.randomize()
	
	# 【修复1】：清空上一局残留的高度池，并增加安全性校验
	for h_key in GlobalClock.tile_h_pool.keys():
		GlobalClock.tile_h_pool[h_key].clear()

	var coords_list = map_data.keys()
	coords_list.shuffle()  # 随机打乱顺序，保证生成位置随机
	
	var valid_tile_count = 0 # 记录真正可以放置地貌的地块数量
	
	for coord in coords_list:
		var data = map_data[coord]
		
		# 跳过 nexus 核心
		if data.has("terrain") and str(data["terrain"]) == "nexus_core":
			continue
			
		valid_tile_count += 1
		
		var height = data["height"]
		var terrain_type = data["terrain_type"]
		
		if terrain_type == null:
			terrain_type = get_terrain_from_height(height)
			data["terrain_type"] = terrain_type
			
		# 填入全局高度池（防报错：如果键不存在则动态创建）
		if not GlobalClock.tile_h_pool.has(height):
			GlobalClock.tile_h_pool[height] = []
		GlobalClock.tile_h_pool[height].append(coord)

	# 【核心机制】：应用 max_landform_ratio (0.35) 全局密度上限
	var absolute_max_landforms = int(valid_tile_count * max_landform_ratio)
	var current_placed = 0
	

	# 优先放置中立地形（森林/雪山等），但不超过设定的最大值，也不超过全局总上限
	var neutral_to_place = min(Max_Neutral_landform, absolute_max_landforms - current_placed)
	current_placed += _place_from_pool(neutral_pool, neutral_to_place)
	
	# 随后放置敌对地形（村庄/碉堡等），利用剩余的总配额
	var enemy_to_place = min(Max_Enemy_landform, absolute_max_landforms - current_placed)
	current_placed += _place_from_pool(enemy_pool, enemy_to_place)
	
# ==========================================
# 2. 通用抽取放置函数 (返回实际放置的数量)
# ==========================================
func _place_from_pool(pool: Array[Script], max_count: int) -> int:
	if pool.is_empty() or max_count <= 0:
		return 0
		
	var number = 0
	
	# 1. 保证池子里的每种地貌至少生成一个（保底机制）
	for landform_script in pool:
		if number >= max_count:
			break
			
		var coord = landform_pick(landform_script)
		if coord != Vector2i(-100, -100):
			var inst = landform_script.new(coord, self)
			map_data[coord]["landform"] = inst
			map_data[coord]["landform_type"] = inst.landform_name
			
			if inst.get("Attitude") == inst.Attitude_Pool.Enemy:
				inst.add_to_group("Enemies")
			elif inst.get("Attitude") == inst.Attitude_Pool.Middle:
				inst.add_to_group("Middle")
				
			number += 1

	# 2. 用随机地貌填满剩下的配额
	var remain = max_count - number
	if remain > 0:
		for i in range(remain):
			var landform_script = pool.pick_random()
			var coord = landform_pick(landform_script)
			if coord != Vector2i(-100, -100):
				var inst = landform_script.new(coord, self)
				map_data[coord]["landform"] = inst
				map_data[coord]["landform_type"] = inst.landform_name
				
				if inst.get("Attitude") == inst.Attitude_Pool.Enemy:
					inst.add_to_group("Enemies")
				elif inst.get("Attitude") == inst.Attitude_Pool.Middle:
					inst.add_to_group("Middle")
					
				number += 1
				
	return number
# ==========================================
# 3. 具体坐标筛选器 (使用探针机制)
# ==========================================
func landform_pick(landform_script: Script) -> Vector2i:
	# 实例化一个“探针”仅用于读取规则
	var buffer = landform_script.new(Vector2i(-100, -100), self)
	var buffer_pool: Array = []
	
	# 1. 圈定候选池（依据高度规则）
	if buffer.landform_rules.has("require_height") and buffer.landform_rules["require_height"] != null:
		var valid_heights = buffer.landform_rules["require_height"]
		# 从允许的高度中随机挑一个高度层
		var target_h = valid_heights[randi() % valid_heights.size()]
		if GlobalClock.tile_h_pool.has(target_h):
			buffer_pool = GlobalClock.tile_h_pool[target_h].duplicate()
	else:
		# 没有高度限制，全图可放
		buffer_pool = map_data.keys().duplicate()
		
	# 2. 如果候选池是空的，直接销毁探针返回失败
	if buffer_pool.is_empty():
		buffer.queue_free() 
		return Vector2i(-100, -100)

	# 3. 在候选池中随机抽取并验证
	var coord_index = randi() % buffer_pool.size()
	var test_coord = buffer_pool[coord_index]
	
	# 循环验证，如果该坐标不合法，踢出池子继续抽
	while not buffer_pool.is_empty() and not buffer.get_possible_coords(test_coord, map_data):
		buffer_pool.remove_at(coord_index)
		if buffer_pool.is_empty():
			break
		# 【修复2】：只有在不为空时才计算 modulo，防止除零崩溃
		coord_index = randi() % buffer_pool.size()
		test_coord = buffer_pool[coord_index]

	# 4. 销毁探针，返回结果
	buffer.queue_free()
	
	if buffer_pool.is_empty():
		return Vector2i(-100, -100)
	else:
		return test_coord
		
## 获取地形顶部纹理（支持 TerrainType 枚举或 String 类型）
func get_top_tex(terrain) -> Texture2D:
	# 处理 String 类型（如 "nexus_core"）
	if typeof(terrain) == TYPE_STRING:
		# 如果是特殊地块，返回默认纹理
		if terrain == "nexus_core":
			return hex_top_tex
		# 尝试将字符串转换为 TerrainType 枚举
		match terrain:
			"BEACH": return get_top_tex(TerrainType.BEACH)
			"PLAINS": return get_top_tex(TerrainType.PLAINS)
			"HILLS": return get_top_tex(TerrainType.HILLS)
			"MOUNTAIN": return get_top_tex(TerrainType.MOUNTAIN)
			_: return hex_top_tex
	
	# 处理 TerrainType 枚举类型
	var terrain_int = terrain as int
	if top_tex_by_terrain.size() > terrain_int and top_tex_by_terrain[terrain_int] != null:
		return top_tex_by_terrain[terrain_int]
	return hex_top_tex

## 获取地形侧面纹理（支持 TerrainType 枚举或 String 类型）
func get_side_tex(terrain) -> Texture2D:
	# 处理 String 类型（如 "nexus_core"）
	if typeof(terrain) == TYPE_STRING:
		# 如果是特殊地块，返回默认纹理
		if terrain == "nexus_core":
			return hex_side_tex
		# 尝试将字符串转换为 TerrainType 枚举
		match terrain:
			"BEACH": return get_side_tex(TerrainType.BEACH)
			"PLAINS": return get_side_tex(TerrainType.PLAINS)
			"HILLS": return get_side_tex(TerrainType.HILLS)
			"MOUNTAIN": return get_side_tex(TerrainType.MOUNTAIN)
			_: return hex_side_tex
	
	# 处理 TerrainType 枚举类型
	var terrain_int = terrain as int
	if side_tex_by_terrain.size() > terrain_int and side_tex_by_terrain[terrain_int] != null:
		return side_tex_by_terrain[terrain_int]
	return hex_side_tex

## 地形类型转字符串（用于调试）
func terrain_type_to_string(terrain: TerrainType) -> String:
	return HEX_TERRAIN_RULES.terrain_name(terrain, "UNKNOWN")


func _render_map():
	stack_nodes.clear()
	for coord in map_data.keys():
		_create_stack_at(coord, map_data[coord])


## 判断本次构建是否需要播放初始地块入场。
## 核心逻辑已经交给 MapIntroRevealRunner；HexMap 这里只负责把当前导出变量和视图状态打包进去。
func _begin_map_intro_reveal_if_needed() -> void:
	_map_intro_reveal_state = _map_intro_reveal_runner.begin_if_needed(_build_map_intro_reveal_config())


## 外部节点查询入口：BarManager / UI 可用它判断是否需要延后生成战斗信息。
func is_map_intro_reveal_active() -> bool:
	return _map_intro_reveal_runner.is_active(_map_intro_reveal_state)


## BarManager 的入场期保护开关。
## 返回 true 时，单体血条请求会先进入 HexMap 缓存，等涟漪动画完成再实例化。
func should_defer_intro_health_bars() -> bool:
	return _map_intro_reveal_runner.should_defer_health_bars(_map_intro_reveal_state)


## 暂存入场期间收到的单体血条生成请求。
## 去重、缓存结构和收尾补发已经移到 MapIntroRevealRunner，HexMap 保留旧函数名给 BarManager 调用。
func queue_intro_health_bar_request(landform_in: landform, situation: int, x: float, y: float) -> void:
	_map_intro_reveal_runner.queue_health_bar_request(_map_intro_reveal_state, landform_in, situation, x, y)


## 播放“倒放湮灭”的左下角涟漪入场。
## Tween 分层、等待和收尾都在 MapIntroRevealRunner 中；HexMap 只提供创建 Tween/Timer 的回调。
func _play_map_intro_reveal() -> void:
	_map_intro_reveal_runner.play(_build_map_intro_reveal_config())


func _set_stack_dissolve_blend(stack: Area2D, value: float) -> void:
	if not is_instance_valid(stack):
		return

	var sprites = stack.get_meta("sprites", []) as Array
	for sprite in sprites:
		if is_instance_valid(sprite) and sprite.material:
			sprite.set_instance_shader_parameter("dissolve_blend", value)


## 构造地图初始入场 runner 需要的上下文。
## 所有导出调整仍留在 HexMap Inspector 上；Runner 只消费这份快照和回调，不保存场景节点引用。
func _build_map_intro_reveal_config() -> Dictionary:
	return {
		"state": _map_intro_reveal_state,
		"enabled": map_intro_reveal_enabled,
		"current_view_state": current_view_state,
		"view_state_3d": MapViewState.VIEW_3D,
		"stack_nodes": stack_nodes,
		"wave_pixel_step": map_intro_reveal_wave_pixel_step,
		"wave_delay": map_intro_reveal_wave_delay,
		"tile_duration": map_intro_reveal_tile_duration,
		"finish_delay": map_intro_reveal_finish_delay,
		"origin_padding": map_intro_reveal_origin_padding,
		"hide_health_ui": map_intro_reveal_hide_health_ui,
		"trans_type": map_intro_reveal_trans_type,
		"ease_type": map_intro_reveal_ease_type,
		"set_stack_dissolve": Callable(self, "_set_stack_dissolve_blend"),
		"create_tween": Callable(self, "_create_map_intro_reveal_tween"),
		"create_timer": Callable(self, "_create_map_intro_reveal_timer"),
		"get_bar_manager": Callable(self, "_get_intro_bar_manager"),
		"get_total_enemy_health_bar": Callable(self, "_get_total_enemy_health_bar"),
		"create_health_bar": Callable(self, "_create_intro_health_bar"),
		"refresh_stack_interactivity": Callable(self, "_refresh_stack_interactivity"),
		"emit_enemy_roster_changed": Callable(self, "_emit_intro_enemy_roster_changed"),
		"emit_reveal_finished": Callable(self, "_emit_map_intro_reveal_finished"),
	}


## 给 MapIntroRevealRunner 创建 Tween。
## 这层薄包装让动画仍然由 HexMap 这个场景节点持有，避免 runner 直接依赖 Node.create_tween()。
func _create_map_intro_reveal_tween() -> Tween:
	return create_tween()


## 给 MapIntroRevealRunner 创建一次性计时器。
## 返回 SceneTreeTimer 后由 runner await timeout，保持原本的总等待时间逻辑。
func _create_map_intro_reveal_timer(duration: float) -> SceneTreeTimer:
	return get_tree().create_timer(duration)


## 查找入场动画期间需要隐藏和补发血条的 BarManager。
## 路径留在 HexMap，避免 runner 记住具体节点命名。
func _get_intro_bar_manager() -> Node:
	return get_node_or_null("BarManager")


## 把延迟的单体血条创建请求交回 BarManager。
## 如果 BarManager 不存在或接口缺失，保持旧行为：跳过本次请求，不让入场流程报错中断。
func _create_intro_health_bar(landform_in: landform, situation: int, x: float, y: float) -> void:
	var bar_manager := _get_intro_bar_manager()
	if not is_instance_valid(bar_manager) or not bar_manager.has_method("Create_Blood_Bar"):
		return
	bar_manager.Create_Blood_Bar(landform_in, situation, x, y)


## 入场完成后通知依赖敌人列表的系统刷新。
## 这层回调用来让 runner 不直接持有 HexMap signal。
func _emit_intro_enemy_roster_changed() -> void:
	enemy_roster_changed.emit()


## 入场完成后发出公共完成信号。
## 教程、总血量条和开局回合流程仍然监听 HexMap 原有 signal。
func _emit_map_intro_reveal_finished() -> void:
	map_intro_reveal_finished.emit()


## 完成入场收尾：恢复交互、补发血条请求，并通知总血量/敌人意图等系统刷新。
func _finish_map_intro_reveal() -> void:
	_map_intro_reveal_runner.finish(_build_map_intro_reveal_config())


func _flush_intro_health_bar_requests() -> void:
	_map_intro_reveal_runner.flush_health_bar_requests(_build_map_intro_reveal_config())


func _set_intro_auxiliary_visuals_visible(is_visible: bool) -> void:
	_map_intro_reveal_runner.set_auxiliary_visuals_visible(is_visible, _build_map_intro_reveal_config())


func _get_total_enemy_health_bar() -> Node:
	var total_health_bar = get_node_or_null("../../ui/TotalEnemyHealthBar")
	if not is_instance_valid(total_health_bar) and get_tree().current_scene:
		total_health_bar = get_tree().current_scene.get_node_or_null("ui/TotalEnemyHealthBar")
	return total_health_bar


## 当前是否处于“地块选择态”。
## 只要 CardManager.current_selected_card 有效，就认为玩家正在选择地块目标。
func _is_target_selection_active() -> bool:
	var cm = get_card_manager()
	if not cm:
		return false
	var selected_card = cm.get("current_selected_card")
	return is_instance_valid(selected_card)


## 判断某个格子上当前是否存在敌人建筑。
## 这里优先依赖 Enemies 分组判断，因为敌方地貌在创建时已经统一入组。
func _stack_has_enemy_building(stack: Area2D) -> bool:
	return _hex_map_collision_presenter.stack_has_enemy_building(stack)


## 根据当前状态刷新所有格子的 input_pickable：
## 1. 外部主开关关闭时，全部禁用
## 2. 智能优化关闭时，全部启用
## 3. 地块选择态时，全部启用
## 4. 闲置态时，仅启用敌人建筑格
func _refresh_stack_interactivity() -> void:
	_hex_map_collision_presenter.refresh_stack_interactivity(_build_collision_presenter_config())


## 根据当前导出的碰撞参数，构建标准六边形碰撞多边形。
## 调整 hitbox_width / hitbox_base_height / hitbox_top_width_ratio 时，
## 最终都会通过这一个函数反映到真实碰撞形状上。
func _build_hitbox_polygon() -> PackedVector2Array:
	return _hex_map_collision_presenter.build_hitbox_polygon(_build_collision_presenter_config())


func _get_safe_hitbox_z_index() -> int:
	# CanvasItem 的安全范围通常是 [-4096, 4096]。
	# 这里统一夹紧，避免检查器里被手动改成更大的值时再次刷报错。
	return _hex_map_collision_presenter.get_safe_hitbox_z_index(_build_collision_presenter_config())


## 重新应用单个地块的碰撞箱形状与位置。
func _refresh_collision_for_stack(stack: Area2D) -> void:
	_hex_map_collision_presenter.refresh_collision_for_stack(stack, _build_collision_presenter_config())


## 对当前地图中的所有碰撞箱执行一次重建。
## 调整 hitbox 参数后调用它，就不需要重新生成整张地图。
func rebuild_all_collision_shapes() -> void:
	_hex_map_collision_presenter.rebuild_all_collision_shapes(_build_collision_presenter_config())


## 构造碰撞/输入 presenter 需要的上下文。
## 所有可调项仍在 HexMap Inspector 上导出；presenter 只消费快照，不保存 HexMap 引用。
func _build_collision_presenter_config() -> Dictionary:
	return {
		"stack_nodes": stack_nodes,
		"hitbox_width": hitbox_width,
		"hitbox_base_height": hitbox_base_height,
		"hitbox_top_width_ratio": hitbox_top_width_ratio,
		"hitbox_offset_x": hitbox_offset_x,
		"hitbox_offset_y": hitbox_offset_y,
		"hitbox_z_index": hitbox_z_index,
		"step_height": step_height,
		"tile_scale": tile_scale,
		"ref_scale": REF_SCALE,
		"current_view_state": current_view_state,
		"view_state_flat": MapViewState.VIEW_FLAT,
		"intro_reveal_active": is_map_intro_reveal_active(),
		"intro_lock_interaction": map_intro_reveal_lock_interaction,
		"settlement_reward_available": current_settlement_reward_mode == SettlementRewardMode.AVAILABLE,
		"is_settlement_reward_stack_available": Callable(self, "_is_settlement_reward_stack_available"),
		"tiles_interactive_master_enabled": _tiles_interactive_master_enabled,
		"smart_collision_enabled": enable_smart_collision_interaction,
		"target_selection_active": _is_target_selection_active(),
	}


func _get_hex_pixel_pos(hex_coord: Vector2) -> Vector2:
	return HEX_COORD_RULES.hex_to_pixel(hex_coord, spacing_x, spacing_y, tile_scale / REF_SCALE)


## 创建指定坐标的地块栈。
## 基础容器、地块层 sprite 和碰撞体已经拆到 TileStackFactory；
## 初始地貌挂接和地貌 sprite 收编已经拆到 TileLandformAttachService；
## metadata、输入信号、入场 dissolve 和平铺高度标签收尾已经拆到 TileStackInitializationService。
func _create_stack_at(coord: Vector2i, data: Dictionary):
	var stack_result := _tile_stack_factory.create_stack(
		_build_tile_stack_factory_config(coord, data)
	)
	var stack_container := stack_result["stack"] as Area2D
	var height := int(stack_result["height"])
	var current_step_h := float(stack_result["current_step_h"])
	var top_block_y := float(stack_result["top_block_y"])
	var sprites_in_stack: Array = stack_result["sprites"]
	stack_nodes[coord] = stack_container

	var landform_result := _tile_landform_attach_service.attach(
		coord,
		data,
		stack_container,
		sprites_in_stack,
		_build_tile_landform_attach_config(height, current_step_h, top_block_y)
	)
	var enemy_instance: Variant = landform_result.get("occupant", null)
	sprites_in_stack = landform_result.get("sprites", sprites_in_stack)

	_tile_stack_initialization_service.initialize(
		stack_container,
		_build_tile_stack_initialization_config(stack_container, sprites_in_stack, height, enemy_instance)
	)


## 收集 TileStackFactory 创建基础地块栈所需的上下文。
## 工厂只消费这份快照，不直接读取 HexMap 的导出变量或场景树。
func _build_tile_stack_factory_config(coord: Vector2i, data: Dictionary) -> Dictionary:
	var terrain_type: Variant = data.get("terrain_type", TerrainType.PLAINS)
	return {
		"position": _get_hex_pixel_pos(coord),
		"map_root": map_root,
		"height": int(data["height"]),
		"current_step_h": step_height * (tile_scale / REF_SCALE),
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
		"tile_scale": tile_scale,
		"top_texture": get_top_tex(terrain_type),
		"side_texture": get_side_tex(terrain_type),
		"block_material": block_material,
		"hitbox_polygon": _build_hitbox_polygon(),
		"hitbox_z_index": _get_safe_hitbox_z_index(),
		"hitbox_offset_x": hitbox_offset_x,
		"hitbox_offset_y": hitbox_offset_y,
	}


## 收集初始地貌挂接服务需要的上下文。
## 服务只负责迁移 `_create_stack_at()` 中的旧地貌挂接逻辑，不直接读取 HexMap 成员变量。
func _build_tile_landform_attach_config(height: int, current_step_h: float, top_block_y: float) -> Dictionary:
	return {
		"owner_battle": self,
		"height": height,
		"current_step_h": current_step_h,
		"tile_scale": tile_scale,
		"top_block_y": top_block_y,
		"hitbox_offset_x": hitbox_offset_x,
		"hitbox_offset_y": hitbox_offset_y,
		"landform_instance_offset": landform_instance_offset,
		"block_material": block_material,
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
	}


## 收集地块栈初始化收尾服务需要的上下文。
## 信号回调在这里提前绑定 stack，服务只负责连接，不理解 HexMap 的 hover、点击或高度视图细节。
func _build_tile_stack_initialization_config(
	stack: Area2D,
	sprites_in_stack: Array,
	height: int,
	enemy_instance: Variant
) -> Dictionary:
	return {
		"sprites": sprites_in_stack,
		"height": height,
		"occupant": enemy_instance,
		"intro_reveal_active": is_map_intro_reveal_active(),
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
		"set_stack_dissolve": Callable(self, "_set_stack_dissolve_blend"),
		"mouse_entered_callback": Callable(self, "_on_stack_hover").bind(stack, true),
		"mouse_exited_callback": Callable(self, "_on_stack_hover").bind(stack, false),
		"input_event_callback": Callable(self, "_on_stack_input").bind(stack),
		"create_height_indicator": Callable(self, "_create_height_indicator"),
	}


## 局部刷新单个地块的视觉表现（优化性能，避免全局重绘）
## @param coord 六边形坐标（Vector2i）
func refresh_tile_visual(coord: Vector2i) -> void:
	if not map_data.has(coord):
		return
	
	# 获取地块数据
	var data = map_data[coord]
	
	# 如果已有视觉节点，先移除
	if stack_nodes.has(coord):
		var old_node = stack_nodes[coord]
		if is_instance_valid(old_node):
			old_node.queue_free()
		stack_nodes.erase(coord)
	
	# 重新创建视觉节点（_create_stack_at 使用 Vector2i 坐标）
	_create_stack_at(coord, data)
	_refresh_stack_interactivity()


func get_card_manager() -> Node:
	# CardManager 生命周期跨局内、奖励和局外返回；具体查找顺序统一交给 locator。
	return _card_manager_locator.find(get_tree())


# ==========================================
# ★ 局外收获建筑表现与输入
# ==========================================
## 进入战斗胜利后的收获状态。
## 设计原则:
## - 不重新生成地块，只复用当前 stack 元数据，节约节点和材质开销。
## - 只打开“有未使用奖励”的建筑碰撞，其他地块继续静默。
## - Tooltip 作为 stack 子节点存在，跟随建筑移动，但不参与遮挡消融。
func enter_settlement_reward_mode(host_node: Node = null) -> void:
	settlement_reward_host = host_node
	current_settlement_reward_mode = SettlementRewardMode.AVAILABLE
	_tiles_interactive_master_enabled = true
	set_visuals_locked(true)
	_set_camera_zoom_input_enabled(false)

	_clear_all_aoe_highlights()
	_clear_occlusion_effects()
	clear_enemy_intent_preview(false)
	_collect_settlement_reward_stacks()
	_refresh_stack_interactivity()


## 离开收获状态时调用。
## 例如未来进入下一场战斗前，可以用它移除所有常驻 tooltip 和高亮。
func exit_settlement_reward_mode() -> void:
	current_settlement_reward_mode = SettlementRewardMode.DISABLED
	settlement_reward_host = null
	settlement_reward_hovered_stack = null
	_set_camera_zoom_input_enabled(true)

	var presenter_config := _build_settlement_reward_presenter_config()
	for stack in settlement_reward_stacks:
		if not is_instance_valid(stack):
			continue
		_settlement_reward_presenter.clear_stack(stack, presenter_config)

	settlement_reward_stacks.clear()
	settlement_reward_stack_data.clear()
	_refresh_stack_interactivity()


## 扫描当前地图，把拥有局外奖励接口的建筑登记为可收获 stack。
func _collect_settlement_reward_stacks() -> void:
	settlement_reward_stacks.clear()
	settlement_reward_stack_data.clear()
	var presenter_config := _build_settlement_reward_presenter_config()

	for coord in stack_nodes.keys():
		var stack = stack_nodes[coord]
		if not is_instance_valid(stack):
			continue

		var reward_landform = _get_settlement_reward_landform(stack, coord)
		if not is_instance_valid(reward_landform):
			_settlement_reward_presenter.clear_stack(stack, presenter_config)
			continue

		var reward_info = _build_settlement_reward_info(stack, coord, reward_landform)
		settlement_reward_stacks.append(stack)
		settlement_reward_stack_data[stack] = reward_info
		_refresh_settlement_reward_stack(stack, presenter_config)


## 从 stack / map_data 双通道读取建筑。
## 这样运行期扩张、重绘、局部刷新后，只要任意一侧还保有引用，都能被收获系统识别。
func _get_settlement_reward_landform(stack: Area2D, coord: Variant) -> Node:
	var occupant = stack.get_meta("occupant") if stack.has_meta("occupant") else null
	if _is_valid_settlement_reward_landform(occupant):
		return occupant

	if map_data.has(coord):
		var tile_data = map_data[coord]
		if typeof(tile_data) == TYPE_DICTIONARY:
			var landform_inst = tile_data.get("landform")
			if _is_valid_settlement_reward_landform(landform_inst):
				return landform_inst

	return null


## 判定建筑是否拥有可展示的收获奖励。
## 具体奖励归属由建筑脚本自己提供，HexMap 只消费统一接口。
func _is_valid_settlement_reward_landform(candidate: Variant) -> bool:
	if not is_instance_valid(candidate):
		return false
	if not (candidate is landform):
		return false
	if candidate.Attitude != candidate.Attitude_Pool.Enemy:
		return false
	if not candidate.has_method("has_settlement_reward"):
		return false
	if not candidate.has_settlement_reward():
		return false
	return _matches_settlement_reward_bind_state(candidate)


## 根据导出配置决定奖励入口绑定死亡敌人还是未死亡敌人。
## 这个判断放在 HexMap，避免 landform 基类把奖励资格和战斗生死状态硬耦合。
func _matches_settlement_reward_bind_state(candidate: landform) -> bool:
	match settlement_reward_bind_state:
		SettlementRewardBindState.DEAD:
			return candidate.State_Main == candidate.Main_State_Pool.Broken
		SettlementRewardBindState.ALIVE:
			return candidate.State_Main != candidate.Main_State_Pool.Broken
		_:
			return false


## 把建筑实例整理成 Main 场景可直接消费的上下文。
func _build_settlement_reward_info(stack: Area2D, coord: Variant, reward_landform: Node) -> Dictionary:
	var reward_type = reward_landform.get_settlement_reward_type() if reward_landform.has_method("get_settlement_reward_type") else ""
	var reward_label = reward_landform.get_settlement_reward_label() if reward_landform.has_method("get_settlement_reward_label") else reward_type
	return {
		"stack": stack,
		"coord": coord,
		"landform": reward_landform,
		"reward_type": reward_type,
		"reward_label": reward_label,
	}


## 当前 stack 是否还能被点击进入奖励页。
func _is_settlement_reward_stack_available(stack: Area2D) -> bool:
	if current_settlement_reward_mode != SettlementRewardMode.AVAILABLE:
		return false
	if not settlement_reward_stack_data.has(stack):
		return false

	var reward_info = settlement_reward_stack_data[stack]
	var reward_landform = reward_info.get("landform")
	return (
		_is_valid_settlement_reward_landform(reward_landform)
		and not _is_settlement_reward_used(reward_landform)
	)


## 判断奖励建筑是否已经消费过局外收获。
## 这里只读取 landform 暴露的状态字段，不创建默认值，避免把奖励状态写入不支持该机制的建筑。
func _is_settlement_reward_used(reward_landform: Node) -> bool:
	if not is_instance_valid(reward_landform):
		return true
	if _object_has_property(reward_landform, &"settlement_reward_used"):
		return bool(reward_landform.get("settlement_reward_used"))
	return false


## 鼠标进入/离开奖励建筑时，叠加白色悬浮层并轻微放大 tooltip。
func _handle_settlement_reward_hover(stack: Area2D, is_entered: bool) -> void:
	if not settlement_reward_stack_data.has(stack):
		return
	if not _is_settlement_reward_stack_available(stack):
		return

	if is_entered:
		settlement_reward_hovered_stack = stack
		_settlement_reward_presenter.set_hover_state(stack, true, true, _build_settlement_reward_presenter_config())
	else:
		if settlement_reward_hovered_stack == stack:
			settlement_reward_hovered_stack = null
		_settlement_reward_presenter.set_hover_state(stack, true, false, _build_settlement_reward_presenter_config())


## 点击未使用奖励建筑时，只发出请求信号。
## 真正打开哪个奖励场景由 Main 场景处理，HexMap 不直接依赖 UI 场景路径。
func _handle_settlement_reward_click(stack: Area2D) -> void:
	if not _is_settlement_reward_stack_available(stack):
		return

	var reward_info = settlement_reward_stack_data.get(stack, {}).duplicate()
	settlement_reward_requested.emit(reward_info)


## 奖励场景确认完成后，由 Main 场景回调。
## 使用后保留 tooltip，但移除建筑/地块高亮并关闭该 stack 鼠标互动。
func mark_settlement_reward_used(stack: Area2D) -> void:
	if not settlement_reward_stack_data.has(stack):
		return

	var reward_info = settlement_reward_stack_data[stack]
	var reward_landform = reward_info.get("landform")
	if is_instance_valid(reward_landform) and reward_landform.has_method("mark_settlement_reward_used"):
		reward_landform.mark_settlement_reward_used()
	elif is_instance_valid(reward_landform) and _object_has_property(reward_landform, &"settlement_reward_used"):
		reward_landform.set("settlement_reward_used", true)

	var presenter_config := _build_settlement_reward_presenter_config()
	_settlement_reward_presenter.refresh_stack(stack, reward_info, false, presenter_config)
	_settlement_reward_presenter.animate_tooltip(stack, false, presenter_config)
	if settlement_reward_hovered_stack == stack:
		settlement_reward_hovered_stack = null
	_refresh_stack_interactivity()


## 根据奖励是否可用刷新单个 stack 的视觉与 tooltip。
func _refresh_settlement_reward_stack(stack: Area2D, presenter_config: Dictionary = {}) -> void:
	var is_available = _is_settlement_reward_stack_available(stack)
	var reward_info: Dictionary = settlement_reward_stack_data.get(stack, {})
	if reward_info.is_empty():
		return

	var config := presenter_config if not presenter_config.is_empty() else _build_settlement_reward_presenter_config()
	_settlement_reward_presenter.refresh_stack(stack, reward_info, is_available, config)


## 收集局外收获地图表现调参。
## 所有可调项仍在 HexMap Inspector 上导出；SettlementRewardPresenter 只消费这份快照，不反向读取 HexMap 状态。
func _build_settlement_reward_presenter_config() -> Dictionary:
	return {
		"tooltip_offset": settlement_reward_tooltip_offset,
		"tooltip_size": settlement_reward_tooltip_size,
		"tooltip_font_size": settlement_reward_tooltip_font_size,
		"tooltip_z_index": settlement_reward_tooltip_z_index,
		"highlight_color": settlement_reward_highlight_color,
		"hover_color": settlement_reward_hover_color,
		"highlight_blend": settlement_reward_highlight_blend,
		"hover_blend": settlement_reward_hover_blend,
		"tooltip_hover_scale": settlement_reward_tooltip_hover_scale,
		"current_step_h": step_height * (tile_scale / REF_SCALE),
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
		"block_shader": block_material.shader if block_material != null else null,
	}

# ==========================================
# ★ 统一的点击输入处理 (支持 3D & 平铺视图)
# ==========================================
## 地块 input_event 的旧连接入口。
## 真实分发已经拆到 HexMapInputCoordinator；这里负责把返回状态写回 HexMap。
func _on_stack_input(viewport: Node, event: InputEvent, shape_idx: int, stack: Area2D):
	_apply_input_coordinator_result(
		_hex_map_input_coordinator.handle_stack_input(
			viewport,
			event,
			shape_idx,
			stack,
			_build_hex_map_input_coordinator_config()
		)
	)


## 处理地块左键点击：无论何种视图，逻辑统一下沉。
## 这个入口保留给调试或旧脚本调用，内部委托输入协调器。
func _handle_tile_click(stack: Area2D) -> void:
	_apply_input_coordinator_result(
		_hex_map_input_coordinator.handle_tile_click(
			stack,
			_build_hex_map_input_coordinator_config()
		)
	)


## 收集地块输入协调器需要的运行时上下文。
## Coordinator 只负责编排输入流程；目标规则、奖励点击、视觉状态和敌人意图都继续由 HexMap 旧入口处理。
func _build_hex_map_input_coordinator_config() -> Dictionary:
	return {
		"settlement_reward_available": current_settlement_reward_mode == SettlementRewardMode.AVAILABLE,
		"visuals_locked": is_visuals_locked,
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
		"hovered_stacks": hovered_stacks,
		"selected_stack": selected_stack,
		"height_view_hovered_stack": height_view_hovered_stack,
		"tile_state_idle": int(TileVisualState.IDLE),
		"tile_state_hover_target_valid": int(TileVisualState.HOVER_TARGET_VALID),
		"get_card_manager": Callable(self, "get_card_manager"),
		"is_stack_valid_target": Callable(self, "_is_stack_valid_target"),
		"change_tile_state": Callable(self, "_change_tile_state_from_input_coordinator"),
		"clear_all_aoe_highlights": Callable(self, "_clear_all_aoe_highlights"),
		"cancel_card_selection": Callable(self, "_cancel_card_selection"),
		"handle_settlement_reward_click": Callable(self, "_handle_settlement_reward_click"),
		"handle_settlement_reward_hover": Callable(self, "_handle_settlement_reward_hover"),
		"start_pillar_floating": Callable(self, "_start_pillar_floating_animation"),
		"stop_pillar_floating": Callable(self, "_stop_pillar_floating_animation"),
		"update_highlight": Callable(self, "_update_highlight"),
		"handle_enemy_intent_stack_hover": Callable(self, "_handle_enemy_intent_stack_hover"),
	}


## 回写输入协调器返回的状态。
## 目前只回写普通选中地块、hover 列表和平铺高度视图 hover 地块。
func _apply_input_coordinator_result(result: Dictionary) -> void:
	if result.has("selected_stack"):
		selected_stack = result.get("selected_stack", null)
	if result.has("hovered_stacks"):
		var next_hovered: Variant = result.get("hovered_stacks", [])
		if next_hovered is Array:
			hovered_stacks = next_hovered
	if result.has("height_view_hovered_stack"):
		height_view_hovered_stack = result.get("height_view_hovered_stack", null)


## 输入协调器使用的状态切换适配器。
## GDScript enum 本质是 int；这里直接传递状态值，避免输入模块依赖 HexMap enum 名称。
func _change_tile_state_from_input_coordinator(stack: Area2D, state: int) -> void:
	change_tile_state(stack, state)
# ==========================================
# ★ 悬浮与多地块 AOE 遮罩检测
# ==========================================
## 地块 hover 的旧连接入口。
## 真实分发已经拆到 HexMapInputCoordinator；这里只回写 hover 相关状态。
func _on_stack_hover(stack: Area2D, is_entered: bool):
	_apply_input_coordinator_result(
		_hex_map_input_coordinator.handle_stack_hover(
			stack,
			is_entered,
			_build_hex_map_input_coordinator_config()
		)
	)


## 转发地块 hover 给敌人意图系统。
## 路径查找暂时保留在 HexMap，后续如果拆 TimelineSystem 依赖，可以把它改成初始化时注入。
func _handle_enemy_intent_stack_hover(stack: Area2D, is_entered: bool) -> void:
	var intent_controller = get_node_or_null("../../ui/TimelineSystem/EnemyIntentManager")
	if intent_controller and intent_controller.has_method("handle_map_stack_hover"):
		intent_controller.handle_map_stack_hover(stack, is_entered)


## 刷新卡牌目标 hover 状态。
## 目标中心选择、无卡牌时的清理，以及 AOE/遮挡刷新触发已经拆到 TargetHoverController；
## HexMap 只负责提供旧回调和回写 hovered_stacks、active_stack。
func _update_highlight() -> void:
	_apply_target_hover_controller_result(
		_target_hover_controller.update(
			hovered_stacks,
			active_stack,
			_build_target_hover_controller_config()
		)
	)


## 收集 TargetHoverController 需要的回调。
## 控制器不直接查找 CardManager、MainBoard 或视觉 presenter，所有场景路径仍由 HexMap 统一管理。
func _build_target_hover_controller_config() -> Dictionary:
	return {
		"get_active_card": Callable(self, "_get_current_selected_card_for_hover"),
		"get_main_board": Callable(self, "_get_main_board_for_hover"),
		"clear_all_aoe_highlights": Callable(self, "_clear_all_aoe_highlights"),
		"clear_occlusion_effects": Callable(self, "_clear_occlusion_effects"),
		"update_aoe_display": Callable(self, "_update_aoe_display"),
		"update_occlusion": Callable(self, "_update_occlusion"),
	}


## 给目标 hover 控制器读取当前选中卡牌。
## CardManager 的多路径查找仍保留在 get_card_manager()，后续统一 CardManagerLocator 时再收敛。
func _get_current_selected_card_for_hover() -> Variant:
	var cm := get_card_manager()
	if cm == null:
		return null
	return cm.get("current_selected_card")


## 给目标 hover 控制器读取 MainBoard。
## 这里保留旧 group 查询方式，避免本次拆分改变 UI 生命周期依赖。
func _get_main_board_for_hover() -> Node:
	return get_tree().get_first_node_in_group("MainBoard")


## 回写 TargetHoverController 返回的 hover 状态。
## 控制器只返回状态快照，真正的成员变量仍归 HexMap 持有。
func _apply_target_hover_controller_result(result: Dictionary) -> void:
	if result.has("hovered_stacks"):
		var next_hovered: Variant = result.get("hovered_stacks", [])
		if next_hovered is Array:
			hovered_stacks = next_hovered
	if result.has("active_stack"):
		active_stack = result.get("active_stack", null)
			
func _clear_all_aoe_highlights() -> void:
	var result := _hex_map_visual_state_presenter.clear_aoe_highlights(
		current_aoe_stacks,
		selected_stack,
		active_stack,
		_build_visual_state_presenter_config()
	)
	current_aoe_stacks = result.get("current_aoe_stacks", [])
	selected_stack = result.get("selected_stack", null)
	active_stack = result.get("active_stack", null)
		
## 应用卡牌目标 AOE hover 展示计划。
## 范围收集、目标状态选择和 tooltip 请求已经拆到 TargetAoeHoverPresenter；
## HexMap 这里只负责把计划交给视觉状态 presenter，并沿用旧 MainBoard tooltip 调用。
func _update_aoe_display(card: Control, center_stack: Area2D, main_board: Node) -> void:
	var display_plan := _target_aoe_hover_presenter.build_display_plan(
		card,
		center_stack,
		_build_target_aoe_hover_presenter_config()
	)
	var new_aoe_stacks: Array[Area2D] = display_plan.get("new_aoe_stacks", [])
	var target_state := int(display_plan.get("target_state", int(TileVisualState.HOVER_TARGET_INVALID)))

	current_aoe_stacks = _hex_map_visual_state_presenter.apply_aoe_state(
		current_aoe_stacks,
		new_aoe_stacks,
		int(target_state),
		_build_visual_state_presenter_config()
	)

	if bool(display_plan.get("tooltip_requested", false)):
		_update_target_selection_tooltip(main_board, display_plan)


## 收集 TargetAoeHoverPresenter 需要的运行时配置。
## presenter 需要地图节点表和目标合法性回调，但不直接读取 CardManager 或 MainBoard。
func _build_target_aoe_hover_presenter_config() -> Dictionary:
	return {
		"stack_nodes": stack_nodes,
		"target_valid_state": int(TileVisualState.HOVER_TARGET_VALID),
		"target_invalid_state": int(TileVisualState.HOVER_TARGET_INVALID),
		"is_stack_valid_target": Callable(self, "_is_stack_valid_target"),
	}


## 根据 AOE 展示计划刷新 MainBoard tooltip。
## MainBoard 的接口仍是旧 `update_target_selection_hover(center_stack, card)`，本函数只把 presenter 的请求翻译回旧调用。
func _update_target_selection_tooltip(main_board: Node, display_plan: Dictionary) -> void:
	if not is_instance_valid(main_board):
		return
	if not main_board.has_method("update_target_selection_hover"):
		return
	var tooltip_stack: Variant = display_plan.get("tooltip_stack", null)
	var tooltip_card: Variant = display_plan.get("tooltip_card", null)
	if is_instance_valid(tooltip_stack) and tooltip_stack is Area2D:
		main_board.update_target_selection_hover(tooltip_stack, tooltip_card)
# ==========================================
# ★ 新增：条件地块效果系统
# ==========================================

## 判断地块是否是卡牌的有效目标
func _is_stack_valid_target(stack: Area2D) -> bool:
	var cm = get_card_manager()
	if not cm: return false

	var selected_card = cm.get("current_selected_card")
	return HEX_TARGET_RULES.is_stack_valid_target(
		stack,
		selected_card,
		_build_hex_target_rules_context(stack)
	)


## 收集卡牌目标规则需要的地图上下文。
## 规则模块只读取坐标、map_data 和 stack_nodes；高亮状态与 UI 表现仍留在 HexMap。
func _build_hex_target_rules_context(center_stack: Area2D) -> Dictionary:
	return {
		"center_coord": stack_nodes.find_key(center_stack),
		"stack_nodes": stack_nodes,
		"map_data": map_data,
	}

## 右键取消选中卡牌
func _cancel_card_selection() -> void:

	# 通知卡牌管理器取消选中
	var cm = get_card_manager()
	if cm:
		cm.deselect_card()

	# 清除地块条件效果
	update_all_stack_conditional_effects()

	# 清除选中地块的Shader效果
	if is_instance_valid(selected_stack):
		_tween_shader_param(selected_stack, "is_selected_blend", 0.0, 0.2)
		selected_stack = null



## 更新所有地块的条件效果（在卡牌选中状态变化时调用）
func update_all_stack_conditional_effects() -> void:
	## 原有方法是为了兼容旧版 custom_card.gd 的调用。
	## 既然现在全面使用了状态机，我们只需要在这里强制清空所有高亮即可，防止残留。
	_clear_all_aoe_highlights()
	_clear_occlusion_effects()
	clear_enemy_intent_preview(false)
	
# ==========================================
# ★ 核心：动态湮灭遮挡物
# ==========================================
func _clear_occlusion_effects() -> void:
	currently_occluding_stacks = _hex_map_visual_state_presenter.clear_occlusion_effects(
		stack_nodes,
		currently_occluding_stacks,
		_build_visual_state_presenter_config()
	)


## 敌人意图地图预览入口
## 作用:
## - 根据 EnemyIntentData 在地图上同时显示：
##   1. 施法者本体高亮
##   2. 目标格波纹高亮
## - 这里不做 tooltip，tooltip 由 EnemyIntentPresentationController 统一处理。
func show_enemy_intent_preview(intent_data: EnemyIntentData, source_color: Color, target_color: Color) -> void:
	_enemy_intent_map_presenter.show(
		intent_data,
		stack_nodes,
		source_color,
		target_color,
		_build_enemy_intent_map_presenter_config()
	)


## 清除当前地图上的敌人意图预览
## @param restore_card_hover
## - true: 清完之后重新刷新卡牌选中高亮
## - false: 只清理敌人意图，不打断当前其他流程
func clear_enemy_intent_preview(restore_card_hover: bool = true) -> void:
	_enemy_intent_map_presenter.clear()
	if restore_card_hover:
		_update_highlight()


## 收集敌人意图地图表现调参。
## 所有值仍通过 HexMap 导出变量调整；EnemyIntentMapPresenter 只消费这份配置，不持有场景导出项。
func _build_enemy_intent_map_presenter_config() -> Dictionary:
	return {
		"frame_texture": enemy_intent_frame_tex,
		"target_shader": enemy_intent_target_shader,
		"overlay_scale": enemy_intent_overlay_scale,
		"overlay_z_index": enemy_intent_overlay_z_index,
		"source_valid_highlight_blend": enemy_intent_source_valid_highlight_blend,
		"source_valid_selected_blend": enemy_intent_source_valid_selected_blend,
		"source_self_highlight_blend": enemy_intent_source_self_highlight_blend,
		"source_self_selected_blend": enemy_intent_source_self_selected_blend,
		"source_invalid_highlight_blend": enemy_intent_source_invalid_highlight_blend,
		"source_invalid_selected_blend": enemy_intent_source_invalid_selected_blend,
		"target_ripple_speed": enemy_intent_target_ripple_speed,
		"target_ripple_density": enemy_intent_target_ripple_density,
		"target_min_alpha": enemy_intent_target_min_alpha,
		"target_max_alpha": enemy_intent_target_max_alpha,
		"hitbox_width": hitbox_width,
		"hitbox_base_height": hitbox_base_height,
		"hitbox_offset_x": hitbox_offset_x,
		"hitbox_offset_y": hitbox_offset_y,
	}


func _update_occlusion(target_stack: Area2D):
	currently_occluding_stacks = _hex_map_visual_state_presenter.update_occlusion(
		target_stack,
		stack_nodes,
		currently_occluding_stacks,
		_build_visual_state_presenter_config()
	)
# ==========================================
# 通用 Shader Tween 控制器
# ==========================================
func _tween_shader_param(stack: Area2D, param_name: String, target_val: float, duration: float):
	_hex_map_visual_state_presenter.tween_shader_param(
		stack,
		param_name,
		target_val,
		duration,
		_build_visual_state_presenter_config()
	)


## 收集普通地图视觉状态表现需要的运行时配置。
## HexMap 继续负责导出变量、视图状态和 Tween 创建；HexMapVisualStatePresenter 只消费这份快照。
func _build_visual_state_presenter_config() -> Dictionary:
	return {
		"create_tween": Callable(self, "_create_map_intro_reveal_tween"),
		"tile_state_tween_duration": 0.15,
		"current_step_h": step_height * (tile_scale / REF_SCALE),
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
		"hitbox_width": hitbox_width,
		"occlusion_x_factor": 0.8,
		"occlusion_clear_duration": 0.12,
		"occlusion_fade_duration": 0.25,
		"occlusion_dissolve_blend": 0.8,
	}


## 运行期实体统一注册入口。
## 核心逻辑: 统一写入 map_data、挂接 stack occupant、创建贴图，并按当前视角立即同步位置。
## 说明:
## - 建造卡、敌人召唤、雷达虚影等“局中新增实体”都应该优先走这里。
## - 这样新增实体不会再遗漏平铺视角下落、血条锚点、分组和交互刷新。
func register_runtime_landform(coord: Vector2i, entity: landform, landform_type: String = "") -> bool:
	var result := _runtime_landform_registrar.register_landform(
		coord,
		entity,
		landform_type,
		_build_runtime_landform_registrar_context()
	)
	if not bool(result.get("success", false)):
		return false

	_sync_stack_to_current_view(coord, false)
	_play_runtime_landform_spawn_vfx(entity)
	if bool(result.get("is_enemy", false)):
		enemy_roster_changed.emit()
	_refresh_stack_interactivity()
	tile_topology_changed.emit()
	return true


## 运行期地貌统一入场表现。
## 核心逻辑: 等 add_landform_visual_at() 创建并赋予 shader 后，复用 VFXManager 的 dissolve_blend 反向消融。
func _play_runtime_landform_spawn_vfx(entity: landform) -> void:
	if not runtime_landform_spawn_vfx_enabled:
		return
	if not is_instance_valid(entity):
		return

	VFXManager.play_pixel_spawn_vfx(entity, get_tree(), runtime_landform_spawn_vfx_duration)


## 收集运行期地貌注册所需上下文。
## Registrar 不直接读取 HexMap 成员变量；所有可变依赖都通过这份快照传入。
func _build_runtime_landform_registrar_context() -> Dictionary:
	return {
		"owner_battle": self,
		"stack_nodes": stack_nodes,
		"map_data": map_data,
		"step_height": step_height,
		"tile_scale": tile_scale,
		"ref_scale": REF_SCALE,
		"hitbox_offset_x": hitbox_offset_x,
		"hitbox_offset_y": hitbox_offset_y,
		"landform_instance_offset": landform_instance_offset,
		"block_material": block_material,
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
	}


func _get_stack_height(stack: Area2D) -> int:
	if not is_instance_valid(stack):
		return 1
	if stack.has_meta("height"):
		return max(1, int(stack.get_meta("height")))
	return 1


func _get_stack_sprites(stack: Area2D) -> Array:
	if not is_instance_valid(stack) or not stack.has_meta("sprites"):
		return []
	var stored_sprites: Variant = stack.get_meta("sprites")
	if stored_sprites is Array:
		return stored_sprites
	return []


func _cleanup_stack_sprites(stack: Area2D) -> Array:
	return _runtime_landform_registrar.cleanup_stack_sprites(stack)


func _get_height_view_drop_delta(stack: Area2D) -> float:
	return _height_view_state_synchronizer.get_drop_delta(
		stack,
		_build_height_view_state_synchronizer_config()
	)


func _find_health_bar_for_landform(entity: landform) -> Node:
	var bar_manager := get_node_or_null("BarManager")
	return _external_render_node_registrar.find_health_bar_for_landform(entity, bar_manager)


## 按 occupant 实例查找高度视图缓存需要的血条。
## 全图平铺/3D 过渡沿用旧命名规则 `HealthBar_<instance_id>`，并把 BarManager 路径留在 HexMap 内部。
func _find_height_view_health_bar_for_occupant(occupant: Variant) -> Node:
	var bar_manager := get_node_or_null("BarManager")
	if not is_instance_valid(bar_manager) or not is_instance_valid(occupant):
		return null
	return bar_manager.get_node_or_null("HealthBar_" + str(occupant.get_instance_id()))


func _get_or_create_height_view_cache(stack: Area2D) -> Dictionary:
	return _height_view_state_synchronizer.get_or_create_cache(
		stack,
		_cleanup_stack_sprites(stack),
		_build_height_view_state_synchronizer_config()
	)


func _get_or_add_sprite_cache(cache: Dictionary, sprite: Node2D) -> Dictionary:
	return _height_view_state_synchronizer.get_or_add_sprite_cache(cache, sprite)


func _get_visual_node_position(node: Node) -> Vector2:
	return _height_view_state_synchronizer.get_visual_node_position(node)


func _sync_node_position_y(node: Node, target_y: float, animate: bool) -> void:
	_height_view_state_synchronizer.sync_node_position_y(
		node,
		target_y,
		animate,
		_build_height_view_state_synchronizer_config()
	)


func _sync_cached_node_to_flat(cache: Dictionary, key: String, node: Node, drop_delta: float, animate: bool) -> void:
	_height_view_state_synchronizer.sync_cached_node_to_flat(
		cache,
		key,
		node,
		drop_delta,
		animate,
		_build_height_view_state_synchronizer_config()
	)


## 将指定地块栈立即同步到当前视角。
## 作用: 给运行期后加入的贴图、实体、血条补上平铺视角下落位置和 3D 恢复缓存。
func _sync_stack_to_current_view(coord: Vector2i, animate: bool = false) -> void:
	_height_view_state_synchronizer.sync_stack_to_current_view(
		coord,
		animate,
		_build_height_view_state_synchronizer_config()
	)


## 收集平铺/3D 视角同步所需上下文。
## 同步器只处理缓存与节点位置；具体地块生成、tooltip 配置和切换流程仍由 HexMap 负责。
func _build_height_view_state_synchronizer_config() -> Dictionary:
	return {
		"stack_nodes": stack_nodes,
		"height_view_original_materials": height_view_original_materials,
		"filler_block_spacing": filler_block_spacing,
		"tile_scale": tile_scale,
		"ref_scale": REF_SCALE,
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
		"cleanup_stack_sprites": Callable(self, "_cleanup_stack_sprites"),
		"find_health_bar_for_landform": Callable(self, "_find_health_bar_for_landform"),
		"tween_position_y": Callable(self, "_tween_position_y"),
		"position_tween_duration": 0.3,
		"update_reward_tooltip": Callable(self, "_update_settlement_reward_tooltip_position_for_stack"),
	}


## 收集全图平铺/3D 过渡需要的上下文。
## 过渡执行器只负责编排整张地图的动画循环；状态切换、调参来源和具体 presenter 仍由 HexMap 管理。
func _build_height_view_map_transition_runner_config() -> Dictionary:
	return {
		"stack_nodes": stack_nodes,
		"height_view_original_materials": height_view_original_materials,
		"filler_block_spacing": filler_block_spacing,
		"tile_scale": tile_scale,
		"ref_scale": REF_SCALE,
		"cleanup_stack_sprites": Callable(self, "_cleanup_stack_sprites"),
		"get_stack_height": Callable(self, "_get_stack_height"),
		"find_health_bar_for_occupant": Callable(self, "_find_height_view_health_bar_for_occupant"),
		"tween_position_y": Callable(self, "_tween_position_y"),
		"create_tween": Callable(self, "create_tween"),
		"create_height_indicator": Callable(self, "_create_height_indicator"),
		"remove_height_indicator": Callable(self, "_remove_height_indicator"),
		"position_tween_duration": 0.3,
		"side_fade_duration": 0.3,
	}


## 刷新单个地块的收获 tooltip 位置。
## 该回调给 HeightViewStateSynchronizer 使用，避免同步器直接依赖奖励 Presenter。
func _update_settlement_reward_tooltip_position_for_stack(stack: Area2D) -> void:
	_settlement_reward_presenter.update_tooltip_position(stack, _build_settlement_reward_presenter_config())


## 添加地貌视觉（用于地形实体死亡、损坏或运行期新增时更新视觉）
## 兼容旧路径：如果调用方已经写好 map_data，这里只委托 Registrar 重新挂接实体、刷新贴图和收编 sprites。
func add_landform_visual_at(coord: Vector2i) -> void:
	var result := _runtime_landform_registrar.refresh_visual_at(
		coord,
		_build_runtime_landform_registrar_context()
	)
	if not bool(result.get("success", false)):
		return

	_sync_stack_to_current_view(coord, false)

	if bool(result.get("is_enemy", false)):
		enemy_roster_changed.emit()
	


## 地形名称转换函数（整合自 node_2d.gd）
func terrain_name(t: int) -> String:
	return HEX_TERRAIN_RULES.terrain_name(t)

## 地貌名称转换函数（整合自 node_2d.gd）
func landform_name(l: int) -> String:
	return HEX_TERRAIN_RULES.landform_name(l)

## 对地块应用伤害（整合自 node_2d.gd）
func apply_damage_to_tile(coord: Vector2i, new_damage: int) -> void:
	# 将 Vector2i 转换为 Vector2（用于字典键）
	var coord_v2 = Vector2(coord)
	if not map_data.has(coord_v2):
		return
	map_data[coord_v2]["damage"] = clamp(new_damage, 0, 2)

## 刷新地貌视觉（整合自 node_2d.gd）
func refresh_landform_visual(coord: Vector2i) -> void:
	# 重新生成整个地图（简单实现）
	build_map_pipeline()

## 处理回合结束时的建筑行为
func _on_step_next(step: int, behavior: int) -> void:
	_tile_turn_behavior_runner.run(step, behavior, map_data, rng)

## 未处理的输入事件（整合自 node_2d.gd）
func _unhandled_input(event):
	if event.is_action_pressed("ui_cancel"): # 按下 Esc
		# 这里可以写返回地图的代码
		pass


## ==========================================
## ★ 视角切换系统：压缩地块高度显示
## ==========================================
## 切换视角状态机
func toggle_height_view() -> void:
	if is_view_transitioning: return
	is_view_transitioning = true
	clear_enemy_intent_preview(false)
	
	if current_view_state == MapViewState.VIEW_3D:
		current_view_state = MapViewState.VIEW_FLAT
		_compress_to_single_height_view()
	else:
		current_view_state = MapViewState.VIEW_3D
		_restore_original_height_view()

	await get_tree().create_timer(0.4).timeout
	_refresh_all_settlement_reward_tooltip_positions()
	is_view_transitioning = false


func _refresh_all_settlement_reward_tooltip_positions() -> void:
	if settlement_reward_stacks.is_empty():
		return

	_settlement_reward_presenter.refresh_positions(
		settlement_reward_stacks,
		_build_settlement_reward_presenter_config()
	)


func _set_camera_zoom_input_enabled(enabled: bool) -> void:
	var camera = get_node_or_null("../Camera2D")
	if not is_instance_valid(camera) and get_tree().current_scene:
		camera = get_tree().current_scene.get_node_or_null("map/Camera2D")

	if is_instance_valid(camera) and _object_has_property(camera, &"zoom_input_enabled"):
		camera.set("zoom_input_enabled", enabled)
## 为单个精灵设置shader参数补间（辅助函数）
# ★ 修复：移除强类型限制 (删除了 : Sprite2D)，以兼容 UI 组件 (TextureProgressBar 等)
func _tween_shader_param_single(sprite, param_name: String, target_val: float, duration: float) -> void:
	if not is_instance_valid(sprite) or not sprite.material:
		return
	
	var current_val = sprite.get_instance_shader_parameter(param_name)
	if current_val == null:
		current_val = 0.0
	
	# 创建补间动画
	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tw.tween_method(func(val: float):
		if is_instance_valid(sprite) and sprite.material:
			sprite.set_instance_shader_parameter(param_name, val)
	, current_val, target_val, duration)

func _tween_position_y(node, target_y: float, duration: float) -> void:
	if not is_instance_valid(node): return
	var tw = create_tween().set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	tw.tween_property(node, "position:y", target_y, duration)
	
## 创建高度指示器（光柱 + 高度标签）使用导出参数
func _create_height_indicator(stack: Area2D, original_height: int) -> void:
	_height_view_indicator_presenter.create_indicator(
		stack,
		original_height,
		_build_height_view_indicator_presenter_config()
	)

## 移除高度指示器
func _remove_height_indicator(stack: Area2D) -> void:
	_height_view_indicator_presenter.remove_indicator(stack)

## 按钮按下回调
func _on_height_view_toggle_pressed() -> void:
	toggle_height_view()

## ==========================================
## ★ 高度视图鼠标交互系统
## ==========================================

## 开始光柱浮动动画（鼠标悬停时调用）
func _start_pillar_floating_animation(stack: Area2D) -> void:
	_height_view_indicator_presenter.start_pillar_floating(
		stack,
		_build_height_view_indicator_presenter_config()
	)

## 停止光柱浮动动画（鼠标离开时调用）
func _stop_pillar_floating_animation(stack: Area2D) -> void:
	_height_view_indicator_presenter.stop_pillar_floating(
		stack,
		_build_height_view_indicator_presenter_config()
	)


## 收集高度视图指示器表现调参。
## HexMap 继续持有 Inspector 导出项，Presenter 只消费这份配置快照。
func _build_height_view_indicator_presenter_config() -> Dictionary:
	return {
		"show_pillars": show_height_pillars,
		"show_labels": show_height_labels,
		"pillar_length": height_view_pillar_length,
		"pillar_width": height_view_pillar_width,
		"pillar_color": height_view_pillar_color,
		"pillar_offset": height_view_pillar_offset,
		"pillar_z_index": 1000,
		"label_font_size": height_label_font_size,
		"label_color": height_label_color,
		"label_outline_color": height_label_outline_color,
		"label_outline_size": height_label_outline_size,
		"label_offset": height_label_offset,
		"label_font": height_label_font,
		"label_z_index": 1001,
		"hover_speed": height_view_hover_speed,
		"hover_amplitude": height_view_hover_amplitude,
		"label_bounce_duration": ele_anim_duration * 0.5,
		"hitbox_offset_x": hitbox_offset_x,
		"hitbox_offset_y": hitbox_offset_y,
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
	}


## 统一开启或关闭所有地块的鼠标交互
func set_tiles_interactive(enabled: bool) -> void:
	_tiles_interactive_master_enabled = enabled
	_refresh_stack_interactivity()


## 锁定或解锁地块的视觉状态（保留当前的高亮和消融效果）
func set_visuals_locked(locked: bool) -> void:
	is_visuals_locked = locked
	
	if locked:
		# 进入时间占位放置阶段时，保留高亮，但必须释放防遮挡消融，避免地块“消失”。
		_clear_occlusion_effects()
		clear_enemy_intent_preview(false)
		if get_tree() != null:
			var main_board = get_tree().get_first_node_in_group("MainBoard")
			if main_board and is_instance_valid(main_board.get("cursor_tooltip")):
				main_board.cursor_tooltip.hide()
	else:
		# 解锁时同样主动释放消融，并强制刷新一次，防止鼠标不动导致 Shader 幽灵卡死。
		_clear_occlusion_effects()
		hovered_stacks.clear()
		_update_highlight()
# ==========================================
# ★ 血条 (HealthBuffer) 视觉同步注册系统
# ==========================================
## 当血条生成后，将其视觉组件加入到对应的渲染栈中，以便统一Shader和高亮
func recollect_sprites_for_landform(landform_obj: landform) -> void:
	var bar_manager = get_node_or_null("BarManager")
	_external_render_node_registrar.recollect_for_landform(
		landform_obj,
		bar_manager,
		_build_external_render_node_registrar_context()
	)

# ==========================================
# ★ 血条等外部节点的视觉同步注册系统
# ==========================================

## 接收外部节点（如血条），将其内部的贴图加入地块渲染序列
func register_extra_render_node(coord: Vector2i, node: Node) -> void:
	_external_render_node_registrar.register_node_at(
		coord,
		node,
		_build_external_render_node_registrar_context()
	)


## 收集外部渲染节点注册所需的地图上下文。
## Registrar 只消费 stack_nodes；BarManager、血条生成时机和视角同步仍由 HexMap 调度。
func _build_external_render_node_registrar_context() -> Dictionary:
	return {
		"stack_nodes": stack_nodes,
	}

## 增强版：处理地块升降（融合了安全偏移逻辑）。
## 旧入口名继续保留给时间轴命令和卡牌效果调用；真实编排已经拆到 TileElevationService。
func animate_elevation_change(stack: Area2D, delta_height: int) -> void:
	await _tile_elevation_service.animate(
		stack,
		delta_height,
		_build_tile_elevation_service_config()
	)


## 收集地块升降服务需要的运行时上下文。
## Inspector 可调项仍全部留在 HexMap；服务只消费快照和回调，避免直接查找 BarManager、GlobalClock 或高度视图 presenter。
func _build_tile_elevation_service_config() -> Dictionary:
	return {
		"tree": get_tree(),
		"stack_nodes": stack_nodes,
		"map_data": map_data,
		"tile_h_pool": GlobalClock.tile_h_pool,
		"height_view_original_materials": height_view_original_materials,
		"is_flat_view": current_view_state == MapViewState.VIEW_FLAT,
		"max_height": max_height,
		"min_height": min_height,
		"filler_block_spacing": filler_block_spacing,
		"tile_scale": tile_scale,
		"ref_scale": REF_SCALE,
		"elevation_move_distance": elevation_move_distance,
		"block_material": block_material,
		"ele_shake_intensity": ele_shake_intensity,
		"ele_shake_duration": ele_shake_duration,
		"ele_anim_duration": ele_anim_duration,
		"ele_trans_type": ele_trans_type,
		"ele_ease_type": ele_ease_type,
		"get_side_tex": Callable(self, "get_side_tex"),
		"create_elevation_tween": Callable(self, "_create_tile_elevation_tween"),
		"get_health_bar_for_occupant": Callable(self, "_get_elevation_health_bar_for_occupant"),
		"animate_label_height": Callable(self, "_animate_elevation_height_label"),
		"queue_tile_destruction": Callable(self, "_queue_tile_destruction_and_wait"),
		"emit_tile_topology_changed": Callable(self, "_emit_tile_topology_changed"),
	}


## 创建地块升降 Tween。
## Tween 仍挂在 HexMap 节点下，保持原来的暂停策略和并行动画行为。
func _create_tile_elevation_tween() -> Tween:
	var tw := create_tween()
	tw.set_parallel(true)
	tw.set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	return tw


## 给升降服务查找需要独立移动的外部血条。
## 如果血条已经是地块或其子节点的一部分，就不单独返回，避免同一视觉节点被父级和自身各移动一次。
func _get_elevation_health_bar_for_occupant(occupant: Variant, moving_parts: Array) -> Node:
	var bar_manager = get_node_or_null("BarManager")
	if not is_instance_valid(bar_manager) or not is_instance_valid(occupant):
		return null

	var hb_node = bar_manager.get_node_or_null("HealthBar_" + str(occupant.get_instance_id()))
	if is_instance_valid(hb_node) and hb_node.get_parent() not in moving_parts:
		return hb_node
	return null


## 播放平铺高度视图里的数字变化反馈。
## 样式、字号和弹跳时长继续从 `_build_height_view_indicator_presenter_config()` 读取。
func _animate_elevation_height_label(stack: Area2D, visual_height: int) -> void:
	await _height_view_indicator_presenter.animate_label_height(
		stack,
		visual_height,
		_build_height_view_indicator_presenter_config()
	)


## 给拆出的服务统一发出地图拓扑变化信号。
## 当前服务升降完成和地块销毁两个路径；调用方通过回调触发，避免直接持有 HexMap 信号。
func _emit_tile_topology_changed() -> void:
	tile_topology_changed.emit()
	
## 安全获取地块上的占位实体（地貌或敌人）
func get_entity_at_hex(coord: Vector2i) -> Node:
	if not stack_nodes.has(coord):
		return null
	
	var stack = stack_nodes[coord]
	if is_instance_valid(stack) and stack.has_meta("occupant"):
		var entity = stack.get_meta("occupant")
		if is_instance_valid(entity):
			return entity
		stack.remove_meta("occupant")
	return null


## 回合开始统一状态结算入口。
## 核心逻辑:
## - 先拍下所有建筑的状态层数，避免中毒扩散在同一回合继续连锁触发。
## - 再按快照逐个让建筑处理状态效果，例如中毒扩散、扣血、衰减。
func process_turn_start_statuses() -> void:
	var status_snapshots: Array[Dictionary] = []

	for coord in stack_nodes.keys():
		var entity: Node = get_entity_at_hex(coord)
		if not is_instance_valid(entity):
			continue
		if not entity.has_method("process_turn_start_statuses"):
			continue
		var component: Variant = entity.get("status_component")
		if not is_instance_valid(component) or not component.has_method("get_status_snapshot"):
			continue

		status_snapshots.append({
			"entity": entity,
			"snapshot": component.get_status_snapshot()
		})

	for entry in status_snapshots:
		var entity: Node = entry.get("entity", null)
		if is_instance_valid(entity) and entity.has_method("process_turn_start_statuses"):
			entity.process_turn_start_statuses(entry.get("snapshot", {}), get_tree())

	tile_topology_changed.emit()

## 检查实体是否存活/有效
func is_entity_alive(entity: Node) -> bool:
	if not is_instance_valid(entity) or entity.is_queued_for_deletion():
		return false
	if entity.has("HP") and entity.HP <= 0:
		return false
	return true

## 执行单个地块的真实销毁流程。
## 旧入口名继续给 TileDestructionBatchQueue 调用；真实 mutation 已拆到 TileDestructionMutationService。
func _perform_tile_destruction(stack: Area2D, coord: Vector2i) -> void:
	await _tile_destruction_mutation_service.perform(
		stack,
		coord,
		_build_tile_destruction_mutation_service_config()
	)


## 收集真实销毁服务需要的上下文。
## 导出调参、VFXManager、数据字典和信号入口都从 HexMap 注入，服务本身不直接查找场景节点。
func _build_tile_destruction_mutation_service_config() -> Dictionary:
	return {
		"tree": get_tree(),
		"vfx_manager": VFXManager,
		"stack_nodes": stack_nodes,
		"map_data": map_data,
		"tile_h_pool": GlobalClock.tile_h_pool,
		"shake_count": tile_destruction_shake_count,
		"shake_step_duration": tile_destruction_shake_step_duration,
		"shake_distance": tile_destruction_shake_distance,
		"dissolve_duration": tile_destruction_dissolve_duration,
		"landform_library_holders": [iron_mine, village],
		"refresh_stack_interactivity": Callable(self, "_refresh_stack_interactivity"),
		"emit_enemy_roster_changed": Callable(self, "_emit_enemy_roster_changed"),
		"emit_tile_topology_changed": Callable(self, "_emit_tile_topology_changed"),
	}


## 给拆出的销毁服务统一发出敌人列表变化信号。
## 当前地块销毁可能移除敌人建筑，因此需要通知总血量和意图等系统刷新。
func _emit_enemy_roster_changed() -> void:
	enemy_roster_changed.emit()


## 将超限地块排入两两消失队列，并等待该地块真正完成销毁。
## 核心逻辑: 高度变化先在 animate_elevation_change() 中播完；这里只负责把销毁节奏统一交给批处理。
func _queue_tile_destruction_and_wait(stack: Area2D, coord: Vector2i) -> void:
	await _tile_destruction_queue.queue_and_wait(
		stack,
		coord,
		get_tree(),
		tile_destruction_batch_size,
		tile_destruction_batch_collect_delay,
		tile_destruction_batch_interval,
		Callable(self, "_perform_tile_destruction")
	)

# ==========================================
# ★ 状态机 Shader 驱动引擎
# ==========================================
func change_tile_state(stack: Area2D, new_state: TileVisualState) -> void:
	_hex_map_visual_state_presenter.change_tile_state(
		stack,
		int(new_state),
		_build_visual_state_presenter_config()
	)

## 压缩为平铺视图 (利用缓存精准归位)
func _compress_to_single_height_view() -> void:
	_height_view_map_transition_runner.compress_to_flat(
		_build_height_view_map_transition_runner_config()
	)

## 恢复 3D 视图 (直接从字典中精准读取坐标)
func _restore_original_height_view() -> void:
	_height_view_map_transition_runner.restore_to_3d(
		_build_height_view_map_transition_runner_config()
	)
