extends Node2D

@export_group("系统配置")
@export var map_seed: String = "HelloGodot"
@export var step_x: float = 264.0
@export var step_y: float = 304.0
@export var stagger_y: float = 152.0

@export_group("场景跳转")
@export var move_duration: float = 0.3
@export var switch_delay: float = 0.5 
@export var combat_scene: String = "res://project.tscn" 
@export var event_scene: String = "res://event.tscn"

@onready var gen = $MapGenerator
@onready var view = $MapRenderer
@onready var player_sprite = $Player
@onready var camera = $Camera2D

var received_text: String = "" 
var tile_data = {}
var tile_features = {}
var player_hex = Vector2i.ZERO
var has_cut = false 
var current_tier = 0 
var is_moving = false
var _current_decision = ""

# ==========================================
# 1. 初始化
# ==========================================
func _ready():
	if received_text == "":
		gen.rng.randomize() 
		map_seed = str(gen.rng.seed)
	else:
		map_seed = received_text
		gen.rng.seed = map_seed.hash()
	
	_create_world()
	player_sprite.position = Vector2.ZERO 
	_update_visual_states()

func _create_world():
	tile_data = gen.generate_logical_map()
	tile_features = gen.distribute_features(tile_data)
	view.clear()
	view.draw_map(tile_data, {"x": step_x, "y": step_y, "stagger": stagger_y})
	view.draw_features(tile_features)
	_update_visual_states()

# ==========================================
# 2. 核心移动与决策 (应用 Global.speed)
# ==========================================
func _unhandled_input(event):
	if is_moving: return
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
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
	
	# 获取当前全局动画速度系数
	var s = Global.get_anim_speed()

	# 1. 玩家移动 (受速度系数影响)
	var player_tween = create_tween()
	player_tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	player_tween.tween_property(player_sprite, "position", target_pos, move_duration * s)
	await player_tween.finished

	# 2. 选角层逻辑
	if not has_cut and type >= 6:
		# 摄像机聚焦 (受速度系数影响)
		await camera.focus_on_position(target_pos, Vector2(1.5, 1.5), 0.5 * s)
		
		_current_decision = "" 
		Global.choose.emit(type) 
		
		var on_confirm = func(): _current_decision = "confirm"
		var on_cancel = func(): _current_decision = "cancel"
		Global.choose_confirm.connect(on_confirm, CONNECT_ONE_SHOT)
		Global.choose_cancel.connect(on_cancel, CONNECT_ONE_SHOT)
		
		while _current_decision == "":
			await get_tree().process_frame
		
		if Global.choose_confirm.is_connected(on_confirm): Global.choose_confirm.disconnect(on_confirm)
		if Global.choose_cancel.is_connected(on_cancel): Global.choose_cancel.disconnect(on_cancel)

		if _current_decision == "confirm":
			has_cut = true
			var to_delete = []
			for c in tile_data:
				if not gen.is_in_sector(c, target): to_delete.append(c)
			
			for c in to_delete:
				tile_data.erase(c)
				tile_features.erase(c)
				if view.tiles.has(c):
					view.tiles[c].queue_free()
					view.tiles.erase(c)
			
			await camera.restore_camera(0.4 * s)
			player_hex = target
		else:
			# 取消回滚 (受速度系数影响)
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
		# 普通移动摄像机跟随
		var cam_follow = create_tween()
		cam_follow.tween_property(camera, "position", target_pos, 0.3 * s)

	is_moving = false
	if type == MapGenerator.TileType.BOSS: current_tier += 1
	_update_visual_states()
	
	if type >= 1 and type <= 4:
		await _enter_room_logic(target)

# ==========================================
# 3. 辅助功能
# ==========================================
func _pixel_to_hex(pos: Vector2) -> Vector2i:
	var q = pos.x / step_x
	var r = (pos.y - q * stagger_y) / step_y
	var s = -q - r
	var rq = round(q); var rr = round(r); var rs = round(s)
	if abs(rq - q) > abs(rr - r) and abs(rq - q) > abs(rs - s): rq = -rr - rs
	elif abs(rr - r) > abs(rs - s): rr = -rq - rs
	return Vector2i(int(rq), int(rr))

func _update_visual_states():
	var max_tier_index = gen.layer_boundaries.size() - 1
	var current_vis_radius = gen.layer_boundaries[min(current_tier, max_tier_index)]
	view.update_visuals(player_hex, current_vis_radius, tile_data)

func _enter_room_logic(target):
	# 切场延迟 (受速度系数影响)
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

	if tile_features.has(target):
		var extras = []
		for f in tile_features[target]:
			match f:
				MapGenerator.RoomFeature.ENHANCE: extras.append("enhance")
				MapGenerator.RoomFeature.DELETE: extras.append("delete")
				MapGenerator.RoomFeature.COMBINE: extras.append("combine")
				MapGenerator.RoomFeature.TRADE: extras.append("trade")
		data_str += "|" + ",".join(extras)
	_switch_scene_with_data(target_scene, data_str)

func _switch_scene_with_data(path: String, data: String):
	if path == "" or not FileAccess.file_exists(path): return
	var next_scene = load(path).instantiate()
	var target_node = next_scene.get_node_or_null("Main/Node2D")
	if target_node and "received_text" in target_node:
		target_node.received_text = data
		get_tree().root.add_child(next_scene)
		get_tree().current_scene = next_scene
		queue_free()
