# 功能: 局外六边形路线地图控制器，负责地图生成、角色选路、房间跳转与章节层级解锁。
# 核心逻辑: _ready() 根据 MapState 恢复或生成地图；_move_to() 处理玩家移动与进房；_advance_tier_from_boss_resolution() 在 boss 结算返回后解锁下一章并播放生成动画。
extends Control

const RoomResolutionControllerScript = preload("res://scene/out_scene/out_scene_modules/RoomResolutionController.gd")
const ChapterRevealAnimationRunnerScript = preload("res://scene/out_scene/out_scene_modules/ChapterRevealAnimationRunner.gd")

# ==========================================
# 1. 变量与配置
# ==========================================
@export_group("系统配置")
@export var map_seed: String = "HelloGodot"
@export var step_x: float = 272.0
@export var step_y: float = 304.0
@export var stagger_y: float = 152.0

@export_group("场景跳转")
@export var move_duration: float = 0.3
@export var switch_delay: float = 0.5 
@export var combat_scene: String = "res://scene/in_scene/in_scene.tscn" 
@export var event_scene: String = "res://event.tscn"

@export_group("章节生成动画")
## boss 结算返回局外后，是否让新解锁章节的地块从下方升起。
@export var enable_chapter_reveal_animation: bool = true
## 生成动画开始前是否把镜头拉到地图中心，方便玩家看见新章节铺开。
@export var chapter_reveal_focus_camera: bool = true
@export var chapter_reveal_camera_zoom: Vector2 = Vector2(0.4, 0.4)
@export_range(0.0, 3.0, 0.05, "or_greater") var chapter_reveal_camera_duration: float = 0.8
@export_range(0.0, 3.0, 0.05, "or_greater") var chapter_reveal_rise_duration: float = 0.8
@export_range(0.0, 2.0, 0.05, "or_greater") var chapter_reveal_settle_duration: float = 0.25
@export_range(0.0, 1.0, 0.01, "or_greater") var chapter_reveal_delay_random: float = 0.2
@export var chapter_reveal_fall_distance_min: float = 500.0
@export var chapter_reveal_fall_distance_max: float = 700.0
@export_range(0.1, 1.5, 0.05) var chapter_reveal_start_scale: float = 0.8
@export var chapter_reveal_start_tint: Color = Color(0.1, 0.1, 0.1, 0.8)

@onready var gen = $MapGenerator
@onready var view = $MapRenderer
@onready var player_sprite = $Player
@onready var camera = $Camera2D
@onready var dim = $UI/DimMenu
@onready var cartoon = $UI/CartoonUI
@onready var clock = $UI/CartoonUI/Clock
@onready var point = $UI/CartoonUI/Clock/Point
@onready var mask = $UI/CartoonUI/ColorBG
@onready var char_pic = $UI/CartoonUI/MenuUI/Control/character
@onready var era_label: Label = $UI/CartoonUI/MenuUI/Control/era
@onready var process_label: Label = $UI/CartoonUI/MenuUI/Control/process

var received_text: String = "" 
var tile_data = {}
var tile_features = {}
var player_hex = Vector2i.ZERO
var has_cut = false 
var current_tier = 0 
var is_moving = false
var _current_decision = ""
var _cached_map_center: Vector2 = Vector2.ZERO
var _map_center_dirty: bool = true
var chosen_char_index: int = -1
var _room_resolution_controller: Variant = null
var _chapter_reveal_animation_runner: Variant = null
## 从其它场景切回来时注入的外部事件。
## 这里不直接在 apply_external_event() 里处理，是为了确保 OutScene 的节点树先 ready 完成。
var pending_external_event: Variant = null
const HEX_DIRS : Array[Vector2i] = [
	Vector2i(-1, 0),
	Vector2i(1, -1),
	Vector2i(0, -1),
	Vector2i(-1, 0),
	Vector2i(-1, 1),
	Vector2i(0, 1),
]

func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false


func _get_room_resolution_controller() -> Variant:
	if _room_resolution_controller == null:
		_room_resolution_controller = RoomResolutionControllerScript.new()
	return _room_resolution_controller


func _get_chapter_reveal_animation_runner() -> Variant:
	if _chapter_reveal_animation_runner == null:
		_chapter_reveal_animation_runner = ChapterRevealAnimationRunnerScript.new()
	return _chapter_reveal_animation_runner


func _get_chapter_reveal_animation_config() -> Dictionary:
	return {
		"fall_distance_min": chapter_reveal_fall_distance_min,
		"fall_distance_max": chapter_reveal_fall_distance_max,
		"start_scale": chapter_reveal_start_scale,
		"start_tint": chapter_reveal_start_tint,
		"delay_random": chapter_reveal_delay_random,
		"rise_duration": chapter_reveal_rise_duration,
		"settle_duration": chapter_reveal_settle_duration,
	}

# ==========================================
# 2. 初始化逻辑
# ==========================================
func _ready():
	_connect_global_clock_progress_signal()
	if SceneLog:
		SceneLog.scene_event("OutScene", "ready", {
			"loaded": MapState.loaded,
			"is_initialized": MapState.is_initialized,
			"current_tier": MapState.current_tier
		})

	# 检查是否有保存的状态
	if MapState.loaded:
		is_moving = true
		dim.show()
		_load_from_global()
		await dim.use(1,1)
		is_moving = false
	elif MapState.is_initialized:
		is_moving = true
		_load_from_global()
		var type = tile_data[player_hex]
		_init_exist_map()
		_update_visual_states()
		mask._update_shader_screen_size(0.225)
		Global.clock.emit(3)
		await point.stopped
		await mask.start_iris_out(0.23)
		cartoon.move_clock_to_ui(clock)
		await cartoon.finish
		var to_delete_coords = []
		for c in tile_data.keys():
			if not gen.is_in_sector(c, HEX_DIRS[chosen_char_index - 1]):
				to_delete_coords.append(c)
			# 【步骤 B】镜头切远（全局中心）
		var map_center = _get_map_center()
		var s = Global.get_anim_speed()
		await camera.focus_on_position(map_center, Vector2(0.4, 0.4), 0.8 * s)

			# 【步骤 C】播放高性能崩坠动画
		await _execute_shatter_animation_optimized(to_delete_coords, s)

			# 正式移除
		for c in to_delete_coords:
			tile_data.erase(c)
			tile_features.erase(c)
			if view.tiles.has(c):
				view.tiles[c].queue_free()
				view.tiles.erase(c)
		_map_center_dirty = true

		_apply_sector_camera_limits(HEX_DIRS[chosen_char_index - 1])

			# 恢复逻辑并解锁相机
		set_process(true)
		camera._is_locked = false
		camera._target_zoom = camera.zoom
		_update_visual_states()
		MapState.ui_settled = true
		is_moving = false
		MapState.loaded = true
		_update_visual_states()

		if type >= 1 and type <= 4:
			await _enter_room_logic(player_hex)
	else:
		is_moving = true
		_init_new_map()
		_update_visual_states()
		mask._update_shader_screen_size(0.225)
		Global.clock.emit(3)
		await point.stopped
		await mask.start_iris_out(0.23)
		cartoon.move_clock_to_ui(clock)
		await cartoon.finish
		MapState.ui_settled = true
		is_moving = false
		MapState.loaded = true
	
	_update_visual_states()
	_refresh_global_progress_labels()
	await _consume_pending_room_resolution()


func _connect_global_clock_progress_signal() -> void:
	if GlobalClock and GlobalClock.has_signal("progress_changed"):
		if not GlobalClock.progress_changed.is_connected(_on_global_clock_progress_changed):
			GlobalClock.progress_changed.connect(_on_global_clock_progress_changed)


func _on_global_clock_progress_changed(_era_value: int, _phase_value: int) -> void:
	_refresh_global_progress_labels()
	
func _init_new_map():
	if received_text == "":
		gen.rng.randomize()
		map_seed = str(gen.rng.seed)
	else:
		map_seed = received_text
		gen.rng.seed = map_seed.hash()

	tile_data = gen.generate_logical_map()
	tile_features = gen.distribute_features(tile_data)
	player_hex = Vector2i.ZERO # 初始位置
	player_sprite.texture = view.tex_player_unknown
	# 同步到渲染层
	_refresh_view()
	apply_tier_camera_limit(0)
	MapState.is_initialized = true

func _init_exist_map():
	if received_text == "":
		gen.rng.seed = map_seed.to_int()
	else:
		map_seed = received_text
		gen.rng.seed = map_seed.hash()

	tile_data = gen.generate_logical_map()
	tile_features = gen.distribute_features(tile_data)
	player_sprite.texture = view.tex_player_icons[chosen_char_index]
	# 同步到渲染层
	_refresh_view()
	apply_tier_camera_limit(0)
	MapState.is_initialized = true

func apply_tier_camera_limit(tier_index: int):
	var max_idx = gen.layer_boundaries.size() - 1
	var safe_tier = clamp(tier_index, 0, max_idx)
	var radius = gen.layer_boundaries[safe_tier]
	var margin = 300.0
	var w_limit = radius * step_x + margin
	var h_limit = radius * (step_y + stagger_y) + margin
	camera.limit_left = int(-w_limit-1000)
	camera.limit_right = int(w_limit+1000)
	camera.limit_top = int(-h_limit+500)
	camera.limit_bottom = int(h_limit-500)
	camera.limit_smoothed = true

func _apply_sector_camera_limits(chosen_hex: Vector2i):
	var margin = 1400.0 # 留一点缓冲区
	if chosen_hex == Vector2i(0, -1): # 正上方 (或靠近上方的扇区)
		camera.limit_bottom = int(margin)
	elif chosen_hex == Vector2i(0, 1): # 正下方
		camera.limit_top = int(-margin)
	elif chosen_hex == Vector2i(-1, 0): # 左上方
		camera.limit_right = int(margin)
		camera.limit_bottom = int(margin)
	elif chosen_hex == Vector2i(-1, 1): # 左下方
		camera.limit_right = int(margin)
		camera.limit_top = int(-margin)
	elif chosen_hex == Vector2i(1, -1): # 右上方
		camera.limit_left = int(-margin)
		camera.limit_bottom = int(margin)
	elif chosen_hex == Vector2i(1, 0): # 右下方
		camera.limit_left = int(-margin)
		camera.limit_top = int(-margin)
	camera.limit_smoothed = true

func _load_from_global():
	map_seed = MapState.map_seed
	tile_data = MapState.tile_data
	tile_features = MapState.tile_features
	player_hex = MapState.player_hex
	current_tier = MapState.current_tier
	has_cut = MapState.has_cut
	chosen_char_index = MapState.chosen_char_index
	
	if has_cut:
		if chosen_char_index != -1 and chosen_char_index < view.tex_player_icons.size():
			var tex = view.tex_player_icons[chosen_char_index]
			player_sprite.texture = tex
			char_pic.texture = tex
	else:
		player_sprite.texture = view.tex_player_unknown

	player_sprite.visible = true
		
	if MapState.ui_settled:
		cartoon.snap_clock_to_ui(clock)
	
	_refresh_view()
	
	apply_tier_camera_limit(current_tier)
	# 恢复玩家位置（像素）
	player_sprite.position = Vector2(player_hex.x * step_x, player_hex.y * step_y + player_hex.x * stagger_y)
	camera.position = player_sprite.position

## 接收来自其它场景的外部事件。
## 当前主要用于“局内结算结束 -> 返回局外”时携带战斗返回信息。
func apply_external_event(payload: Variant) -> void:
	pending_external_event = payload
	if SceneLog:
		SceneLog.scene_event("OutScene", "apply_external_event", {"payload": payload})


## 刷新局外主 UI 上的时代/阶段文字。
## 这样时代值是否跨场景保留，会在局外立刻可见。
func _refresh_global_progress_labels() -> void:
	if is_instance_valid(era_label):
		var era_value := GlobalClock.get_current_era() if GlobalClock and GlobalClock.has_method("get_current_era") else 1
		era_label.text = "第%d时代" % era_value

	if is_instance_valid(process_label):
		var phase_value := GlobalClock.get_current_phase() if GlobalClock and GlobalClock.has_method("get_current_phase") else 1
		process_label.text = "%d / 8" % phase_value


## 统一消费“从局内返回局外”的房间结算结果。
## 数据来源有两种：
## 1. 显式 apply_external_event(payload)
## 2. 兜底使用 MapState.pending_room_resolution
##
## 这样即使未来切场方式从“手动实例化”改回“change_scene_to_file”，
## 这层接口依然成立，不会把结算结果绑死在某一种转场实现上。
func _consume_pending_room_resolution() -> void:
	var resolution_payload: Dictionary = _get_room_resolution_controller().consume_pending_payload(pending_external_event, MapState)
	if resolution_payload.is_empty():
		return

	await _handle_room_resolution_payload(resolution_payload)
	pending_external_event = null


## 处理从局内返回的房间结算数据。
## 现在会额外识别 boss 房间结算：
## - 普通/精英/事件房只刷新局外 UI。
## - boss 房在确认来自当前章节边界后，推进 current_tier 并播放下一圈地块生成动画。
func _handle_room_resolution_payload(payload: Dictionary) -> void:
	print("[OutScene] 接收到房间结算结果: ", payload)
	if SceneLog:
		SceneLog.scene_event("OutScene", "room resolution received", payload)

	var did_advance_tier := false
	if _should_advance_tier_from_boss_payload(payload):
		did_advance_tier = await _advance_tier_from_boss_resolution(payload)

	# 预留挂点：
	# - 未来可在这里基于 payload["room_context"] 定位局外地图节点
	# - 决定是否把房间改为已完成/已清空/已领取奖励状态
	# - 根据 payload 中的战斗结果分流不同的局外处理
	if _get_room_resolution_controller().should_clear_active_room_context(payload) and MapState.has_method("clear_active_room_context"):
		MapState.clear_active_room_context()

	_refresh_global_progress_labels()
	if did_advance_tier:
		_save_to_global()
	
func _refresh_view():
	view.clear()
	view.draw_map(tile_data, {"x": step_x, "y": step_y, "stagger": stagger_y})
	view.draw_features(tile_features)

func _create_world():
	tile_data = gen.generate_logical_map()
	tile_features = gen.distribute_features(tile_data)
	_map_center_dirty = true
	view.clear()
	view.draw_map(tile_data, {"x": step_x, "y": step_y, "stagger": stagger_y})
	view.draw_features(tile_features)
	_update_visual_states()


func _should_advance_tier_from_boss_payload(payload: Dictionary) -> bool:
	return _get_room_resolution_controller().should_advance_tier_from_boss_payload(
		payload,
		tile_data,
		gen.layer_boundaries,
		current_tier,
		player_hex,
		MapGenerator.TileType.BOSS
	)


func _advance_tier_from_boss_resolution(_payload: Dictionary) -> bool:
	var tier_plan: Dictionary = _get_room_resolution_controller().build_tier_advance_plan(gen.layer_boundaries, current_tier)
	if not bool(tier_plan.get("should_advance", false)):
		return false

	var previous_radius: int = int(tier_plan.get("previous_radius", 0))
	var next_tier: int = int(tier_plan.get("next_tier", current_tier))
	var next_radius: int = int(tier_plan.get("next_radius", previous_radius))
	var reveal_coords: Array = _get_reveal_coords_between_radii(previous_radius, next_radius)

	current_tier = next_tier
	MapState.current_tier = current_tier
	apply_tier_camera_limit(current_tier)

	if reveal_coords.is_empty() or not enable_chapter_reveal_animation:
		_update_visual_states()
		return true

	is_moving = true
	set_process(false)
	_update_visual_states()
	_prepare_chapter_reveal_tiles(reveal_coords)

	var s: float = Global.get_anim_speed()
	if chapter_reveal_focus_camera:
		var target_center: Vector2 = _get_map_center_for_radius(next_radius)
		await camera.focus_on_position(target_center, chapter_reveal_camera_zoom, chapter_reveal_camera_duration * s)

	await _execute_chapter_reveal_animation(reveal_coords, s)

	_update_visual_states()
	set_process(true)
	camera._is_locked = false
	camera._target_zoom = camera.zoom
	is_moving = false
	return true


func _get_reveal_coords_between_radii(previous_radius: int, next_radius: int) -> Array:
	var coords: Array = []
	for c in tile_data.keys():
		if int(tile_data.get(c, 0)) == MapGenerator.TileType.VOID:
			continue
		var dist: int = _get_hex_distance_from_origin(c)
		if dist > previous_radius and dist <= next_radius and view.tiles.has(c):
			coords.append(c)

	coords.sort_custom(func(a, b): return _get_hex_distance_from_origin(a) < _get_hex_distance_from_origin(b))
	return coords


func _prepare_chapter_reveal_tiles(coords_list: Array) -> void:
	_get_chapter_reveal_animation_runner().prepare_tiles(
		coords_list,
		view.tiles,
		Callable(self, "_hex_to_pixel"),
		_get_chapter_reveal_animation_config()
	)


func _execute_chapter_reveal_animation(coords_list: Array, s: float) -> void:
	await _get_chapter_reveal_animation_runner().execute_animation(
		coords_list,
		view.tiles,
		Callable(self, "_hex_to_pixel"),
		self,
		_get_chapter_reveal_animation_config(),
		s
	)


func _get_hex_distance_from_origin(coords: Vector2i) -> int:
	return (abs(coords.x) + abs(coords.y) + abs(coords.x + coords.y)) >> 1


func _hex_to_pixel(coords: Vector2i) -> Vector2:
	return Vector2(coords.x * step_x, coords.y * step_y + coords.x * stagger_y)


func _get_map_center_for_radius(max_radius: int) -> Vector2:
	var sum := Vector2.ZERO
	var count := 0
	for c in tile_data.keys():
		if int(tile_data.get(c, 0)) == MapGenerator.TileType.VOID:
			continue
		if _get_hex_distance_from_origin(c) > max_radius:
			continue
		sum += _hex_to_pixel(c)
		count += 1

	return sum / count if count > 0 else _get_map_center()

# ==========================================
# 3. 核心交互时序
# ==========================================
func _unhandled_input(event):
	if is_moving: return
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and not event.pressed:
			if camera.is_actually_dragging:
				return 
			var target_hex = _pixel_to_hex(get_local_mouse_position())
			if tile_data.has(target_hex):
				if tile_data[target_hex] <= 0: return 
				if gen.get_hex_dist(player_hex, target_hex) == 1:
					_move_to(target_hex)

func _move_to(target):
	is_moving = true
	var type = tile_data[target]
	var old_pos = player_sprite.position
	var old_hex = player_hex
	var target_pos = Vector2(target.x * step_x, target.y * step_y + target.x * stagger_y)
	
	var s = Global.get_anim_speed()

	# 1. 玩家移动
	var player_tween = create_tween()
	player_tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	player_tween.tween_property(player_sprite, "position", target_pos, move_duration * s)
	await player_tween.finished

	# 2. 选角层逻辑
	if not has_cut and type >= 6:
		# 【步骤 A】镜头切近
		await camera.focus_on_position(target_pos, Vector2(1.5, 1.5), 0.5 * s)
		
		_current_decision = "" 
		Global.choose.emit(type) 
		
		var on_confirm = func(): _current_decision = "confirm"
		var on_cancel = func(): _current_decision = "cancel"
		Global.choose_confirm.connect(on_confirm, CONNECT_ONE_SHOT)
		Global.choose_cancel.connect(on_cancel, CONNECT_ONE_SHOT)
		
		while _current_decision == "":
			await get_tree().process_frame

		if _current_decision == "confirm":
			has_cut = true
			chosen_char_index = type - 6
			
			var char_tween = create_tween()
			char_tween.set_parallel(true)
			char_tween.tween_property(player_sprite, "scale", Vector2(0.5, 0.5), 0.1 * s)
			char_tween.tween_property(player_sprite, "self_modulate", Color(2, 2, 2, 1), 0.15 * s) # 瞬间闪白
			await char_tween.finished
			if chosen_char_index >= 0 and chosen_char_index < view.tex_player_icons.size():
				player_sprite.texture = view.tex_player_icons[chosen_char_index]
				char_pic.texture = view.tex_player_icons[chosen_char_index]
			var bang_tween = create_tween()
			bang_tween.set_parallel(true)
			bang_tween.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
			bang_tween.tween_property(player_sprite, "scale", Vector2(1.0, 1.0), 0.3 * s)
			bang_tween.tween_property(player_sprite, "self_modulate", Color(1, 1, 1, 1), 0.3 * s)
			
			# 暂时禁用 Process 逻辑减少 CPU 争抢
			set_process(false)
			
			# 分拣地块
			var to_delete_coords = []
			for c in tile_data.keys():
				if not gen.is_in_sector(c, target): 
					to_delete_coords.append(c)

			# 【步骤 B】镜头切远（全局中心）
			var map_center = _get_map_center()
			await camera.focus_on_position(map_center, Vector2(0.4, 0.4), 0.8 * s)

			# 【步骤 C】播放高性能崩坠动画
			await _execute_shatter_animation_optimized(to_delete_coords, s)
			
			# 正式移除
			for c in to_delete_coords:
				tile_data.erase(c)
				tile_features.erase(c)
				if view.tiles.has(c):
					view.tiles[c].queue_free()
					view.tiles.erase(c)
			_map_center_dirty = true
			
			_apply_sector_camera_limits(target)
			
			# 恢复逻辑并解锁相机
			set_process(true)
			camera._is_locked = false
			camera._target_zoom = camera.zoom
			_update_visual_states()
			
			player_hex = target
			MapState.path_gone.append(target)
		else:
			# 【取消逻辑】
			await camera.restore_camera(0.4 * s)
			var back_tween = create_tween()
			back_tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
			back_tween.tween_property(player_sprite, "position", old_pos, move_duration * s)
			await back_tween.finished
			player_hex = old_hex
			is_moving = false
			_update_visual_states()
			return
	else:
		player_hex = target
		var cam_follow = create_tween()
		cam_follow.tween_property(camera, "position", target_pos, 0.3 * s)

	is_moving = false
	_update_visual_states()
	
	if type >= 1 and type <= 4:
		await _enter_room_logic(target)

# ==========================================
# 4. 优化后的特效函数 (性能提升关键)
# ==========================================
func _execute_shatter_animation_optimized(coords_list: Array, s: float):
	if coords_list.size() == 0: return

	var tiles_ref = view.tiles
	var tween_stage1 = create_tween().set_parallel(true)
	var coords_size = coords_list.size()
	
	for i in range(coords_size):
		var c = coords_list[i]
		if tiles_ref.has(c):
			var tile = tiles_ref[c]
			var delay = randf() * 0.15 
			tween_stage1.tween_property(tile, "self_modulate", Color(0.1, 0.1, 0.1, 0.8), 0.3 * s).set_delay(delay)
			tween_stage1.tween_property(tile, "scale", Vector2(0.8, 0.8), 0.3 * s).set_delay(delay)
	await tween_stage1.finished

	var tween_stage2 = create_tween().set_parallel(true)
	for i in range(coords_size):
		var c = coords_list[i]
		if tiles_ref.has(c):
			var tile = tiles_ref[c]
			var delay = randf() * 0.2
			var fall_dist = 500 + randf() * 200
			var rot = randf_range(-1.2, 1.2)
			
			tween_stage2.tween_property(tile, "modulate:a", 0.0, 0.6 * s).set_delay(delay)
			tween_stage2.tween_property(tile, "position:y", tile.position.y + fall_dist, 0.8 * s).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN).set_delay(delay)
			tween_stage2.tween_property(tile, "rotation", rot, 0.8 * s).set_delay(delay)
	
	await tween_stage2.finished

# ==========================================
# 5. 辅助工具函数
# ==========================================
func _get_map_center() -> Vector2:
	if not _map_center_dirty:
		return _cached_map_center
	if tile_data.is_empty(): 
		_cached_map_center = Vector2.ZERO
		_map_center_dirty = false
		return _cached_map_center
	
	var sum = Vector2.ZERO
	var count = 0
	var keys = tile_data.keys()
	for i in range(keys.size()):
		var c = keys[i]
		sum.x += c.x * step_x
		sum.y += c.y * step_y + c.x * stagger_y
		count += 1
	
	_cached_map_center = sum / count
	_map_center_dirty = false
	return _cached_map_center

func _pixel_to_hex(pos: Vector2) -> Vector2i:
	var q = pos.x / step_x
	var r = (pos.y - q * stagger_y) / step_y
	var s = -q - r
	var rq = round(q)
	var rr = round(r)
	var rs = round(s)
	
	var dq = abs(rq - q)
	var dr = abs(rr - r)
	var ds = abs(rs - s)
	
	if dq > dr and dq > ds:
		rq = -rr - rs
	elif dr > ds:
		rr = -rq - rs
	
	return Vector2i(int(rq), int(rr))

func _update_visual_states():
	if not gen or not view: return
	var max_tier_index = gen.layer_boundaries.size() - 1
	var current_vis_radius = gen.layer_boundaries[min(current_tier, max_tier_index)]
	view.update_visuals(player_hex, current_vis_radius, tile_data)

func _enter_room_logic(target):
	await get_tree().create_timer(switch_delay * Global.get_anim_speed()).timeout
	var type = tile_data[target]
	var data_str = ""
	var target_scene = combat_scene 
	match type:
		MapGenerator.TileType.NORMAL: data_str = "battle_normal"
		MapGenerator.TileType.ELITE: data_str = "battle_elite"
		MapGenerator.TileType.BOSS: data_str = "boss_stage"
		MapGenerator.TileType.EVENT: 
			data_str = "event_stage"
			target_scene = event_scene
	data_str = data_str + " " + map_seed
	if SceneLog:
		SceneLog.scene_event("OutScene", "enter room", {
			"target": target,
			"type": type,
			"payload": data_str,
			"current_tier": current_tier
		})

	# 在切到局内前，先把“当前进入的房间”上下文记到全局。
	# 这样局内战斗结束后返回局外时，仍然知道自己是从哪个局外格子进入的。
	if MapState.has_method("set_active_room_context"):
		MapState.set_active_room_context({
			"source_scene": "out_scene",
			"room_hex": target,
			"room_type": type,
			"room_data": data_str,
			"map_seed": map_seed,
			"current_tier": current_tier,
		})

	Global.dim_in.emit()
	await dim.use(0,0)
	_switch_scene_with_data(target_scene, data_str)

func _save_to_global():
	MapState.map_seed = map_seed
	MapState.tile_data = tile_data
	MapState.tile_features = tile_features
	MapState.player_hex = player_hex
	MapState.current_tier = current_tier
	MapState.has_cut = has_cut
	MapState.chosen_char_index = chosen_char_index
	MapState.ui_settled = true
	MapState.Call_Saver()



func _switch_scene_with_data(path: String, data: String):
	var packed_scene := _load_packed_scene_for_switch(path, "进入局内失败")
	if packed_scene == null:
		_recover_dim_after_failed_switch()
		return

	_save_to_global()
	if SceneLog:
		SceneLog.scene_event("OutScene", "switch scene start", {"path": path, "payload": data})
	var next_scene = packed_scene.instantiate()

	# 局内 HexMap 会在进入树时立刻 build_map_pipeline()。
	# 因此 payload 必须在 add_child() 之前写入，否则导出/运行时可能先用空房间类型建图。
	_apply_payload_before_scene_enters_tree(next_scene, data)

	get_tree().root.add_child(next_scene)
	get_tree().current_scene = next_scene
	if SceneLog:
		SceneLog.scene_event("OutScene", "switch scene success", {"path": path})
	queue_free()


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


## 切场失败时必须把刚刚保持的黑幕退掉，否则玩家会停在全黑画面。
func _recover_dim_after_failed_switch() -> void:
	if is_instance_valid(dim) and dim.has_method("use"):
		dim.use(1, 1)


## 统一记录切场错误，导出版可在 user://logs/scene_flow.log 里定位。
func _log_scene_switch_error(message: String, extra: Dictionary = {}) -> void:
	if SceneLog:
		SceneLog.error_event("OutScene", message, extra)
	push_error("[OutScene] %s %s" % [message, str(extra)])


## 在新场景进入树之前写入外部 payload。
## 核心逻辑:
## - 先写 HexMap，保证它的 _ready() 建图时已经拿到房间类型与 seed。
## - 再写 MainBoard，保留战斗返回时需要的 battle_tag / map_seed。
## - 最后保留根节点和旧路径兜底，兼容教程场景与历史结构。
func _apply_payload_before_scene_enters_tree(next_scene: Node, data: String) -> void:
	var delivered := false

	var hex_map = next_scene.get_node_or_null("map/HexMap")
	if hex_map and hex_map.has_method("apply_external_event"):
		hex_map.apply_external_event(data)
		delivered = true

	var main_board = next_scene.get_node_or_null("ui/Main")
	if main_board and main_board.has_method("apply_external_event"):
		main_board.apply_external_event(data)
		delivered = true

	if not delivered and next_scene.has_method("apply_external_event"):
		next_scene.apply_external_event(data)
		delivered = true

	if not delivered:
		var target_node = next_scene.get_node_or_null("Main/Node2D")
		if target_node and _object_has_property(target_node, &"received_text"):
			target_node.received_text = data
