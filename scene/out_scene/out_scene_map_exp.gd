extends Control

# ==========================================
# 1. 变量与配置
# ==========================================
@export_group("系统配置")
@export var map_seed: String = "HelloGodot"
@export var step_x: float = 264.0
@export var step_y: float = 304.0
@export var stagger_y: float = 152.0

@export_group("场景跳转")
@export var move_duration: float = 0.3
@export var switch_delay: float = 0.5 
@export var combat_scene: String = "res://scene/in_scene/in_scene.tscn" 
@export var event_scene: String = "res://event.tscn"

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

# ==========================================
# 2. 初始化逻辑
# ==========================================
func _ready():
	# 检查是否有保存的状态
	if MapState.is_initialized:
		await dim.use(1,1)
		_load_from_global()
	else:
		_init_new_map()
		_update_visual_states()
		mask._update_shader_screen_size(0.225)
		Global.clock.emit(3)
		await point.stopped
		await mask.start_iris_out(0.23)
		cartoon.move_clock_to_ui(clock)
		MapState.ui_settled = true
	
	_update_visual_states()
	
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
	
	if has_cut:
		if MapState.chosen_char_index != -1:
			var tex = view.tex_player_icons[MapState.chosen_char_index]
			player_sprite.texture = tex
			char_pic.texture = tex
	else:
		player_sprite.texture = view.tex_player_unknown
		
	if MapState.ui_settled:
		cartoon.snap_clock_to_ui(clock)
	
	_refresh_view()
	
	apply_tier_camera_limit(current_tier)
	# 恢复玩家位置（像素）
	player_sprite.position = Vector2(player_hex.x * step_x, player_hex.y * step_y + player_hex.x * stagger_y)
	camera.position = player_sprite.position
	
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
	if type == MapGenerator.TileType.BOSS: 
		current_tier += 1
		apply_tier_camera_limit(current_tier)
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

func _switch_scene_with_data(path: String, data: String):
	if path == "" or not FileAccess.file_exists(path): return
	_save_to_global()
	var next_scene = load(path).instantiate()
	var target_node = next_scene.get_node_or_null("Main/Node2D")
	if target_node and "received_text" in target_node:
		target_node.received_text = data
	get_tree().root.add_child(next_scene)
	get_tree().current_scene = next_scene
	queue_free()
