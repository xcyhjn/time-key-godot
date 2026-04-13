extends Node2D
class_name MapRenderer

@export_group("贴图配置")
@export var tex_normal: Texture2D
@export var tex_elite: Texture2D
@export var tex_event: Texture2D
@export var tex_boss: Texture2D
@export var tex_start: Texture2D
@export var tex_mountain: Texture2D
@export var tex_player_unknown: Texture2D
@export var tex_chars: Array[Texture2D]
@export var tex_player_icons: Array[Texture2D]
@export var icon_enhance: Texture2D
@export var icon_delete: Texture2D
@export var icon_combine: Texture2D
@export var icon_trade: Texture2D
@export var icon_ui_size: float = 80.0

var tiles: Dictionary = {}
var _tex_map: Dictionary = {}
var _feature_tex_map: Dictionary = {}

func _ready():
	_build_texture_maps()

func _build_texture_maps():
	_tex_map[-1] = tex_mountain
	_tex_map[1] = tex_normal
	_tex_map[2] = tex_elite
	_tex_map[3] = tex_event
	_tex_map[4] = tex_boss
	_tex_map[5] = tex_start
	
	_feature_tex_map[0] = icon_enhance
	_feature_tex_map[1] = icon_delete
	_feature_tex_map[2] = icon_combine
	_feature_tex_map[3] = icon_trade

func clear():
	for child in get_children(): child.queue_free()
	tiles.clear()

func draw_map(tile_data: Dictionary, step_cfg: Dictionary):
	var step_x = step_cfg.x
	var step_y = step_cfg.y
	var stagger = step_cfg.stagger
	
	for coords in tile_data:
		var type = tile_data[coords]
		if type == 0: continue
		var s = Sprite2D.new()
		s.position = Vector2(coords.x * step_x, coords.y * step_y + coords.x * stagger)
		s.texture = _get_tile_tex_cached(type)
		s.visible = false 
		add_child(s)
		tiles[coords] = s

func _get_tile_tex(type):
	if type >= 6: return tex_chars[type-6] if (type-6) < tex_chars.size() else null
	match type:
		-1: return tex_mountain
		1: return tex_normal
		2: return tex_elite
		3: return tex_event
		4: return tex_boss
		5: return tex_start
	return null

func _get_tile_tex_cached(type):
	if type >= 6: 
		var idx = type - 6
		return tex_chars[idx] if idx < tex_chars.size() else null
	return _tex_map.get(type, null)

func draw_features(feature_data: Dictionary):
	for coords in feature_data:
		if not tiles.has(coords): continue
		var container = Node2D.new()
		container.z_index = 1
		tiles[coords].add_child(container)
		var features = feature_data[coords]
		var features_size = features.size()
		var start_x = -(features_size - 1) * (icon_ui_size + 8) / 2.0
		for i in range(features_size):
			var fs = Sprite2D.new()
			fs.texture = _feature_tex_map.get(features[i], null)
			if fs.texture:
				var tex_size = fs.texture.get_size().x
				fs.scale = Vector2(icon_ui_size / tex_size, icon_ui_size / tex_size)
			fs.position.x = start_x + i * (icon_ui_size + 8)
			container.add_child(fs)

func update_visuals(p_hex: Vector2i, max_radius: int, tile_data: Dictionary):
	var p_x = p_hex.x
	var p_y = p_hex.y
	
	for coords in tiles:
		var c_x = coords.x
		var c_y = coords.y
		var d_c = (abs(c_x) + abs(c_y) + abs(c_x + c_y)) >> 1
		var d_p = (abs(c_x - p_x) + abs(c_y - p_y) + abs(c_x + c_y - p_x - p_y)) >> 1
		var sprite = tiles[coords]
		var type = tile_data.get(coords, 0)
		
		sprite.visible = d_c <= max_radius
		if not sprite.visible: continue
		
		if d_p == 0: 
			sprite.self_modulate = Color(1.2, 1.2, 1.2)
		elif d_p == 1:
			sprite.self_modulate = Color(0.6, 1.2, 0.6) if type > 0 else Color(0.5, 0.5, 0.5)
		else: 
			sprite.self_modulate = Color(1, 1, 1)
