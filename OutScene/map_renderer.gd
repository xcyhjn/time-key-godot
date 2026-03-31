extends Node2D
class_name MapRenderer

@export_group("贴图配置")
@export var tex_normal: Texture2D
@export var tex_elite: Texture2D
@export var tex_event: Texture2D
@export var tex_boss: Texture2D
@export var tex_start: Texture2D
@export var tex_mountain: Texture2D
@export var tex_chars: Array[Texture2D]
@export var icon_enhance: Texture2D
@export var icon_delete: Texture2D
@export var icon_combine: Texture2D
@export var icon_trade: Texture2D
@export var icon_ui_size: float = 80.0

var tiles: Dictionary = {}

func clear():
	for child in get_children(): child.queue_free()
	tiles.clear()

func draw_map(tile_data: Dictionary, step_cfg: Dictionary):
	for coords in tile_data:
		var type = tile_data[coords]
		if type == 0: continue
		var s = Sprite2D.new()
		s.position = Vector2(coords.x * step_cfg.x, coords.y * step_cfg.y + coords.x * step_cfg.stagger)
		s.texture = _get_tile_tex(type)
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

func draw_features(feature_data: Dictionary):
	var f_tex = {0: icon_enhance, 1: icon_delete, 2: icon_combine, 3: icon_trade}
	for coords in feature_data:
		if not tiles.has(coords): continue
		var container = Node2D.new()
		container.z_index = 1
		tiles[coords].add_child(container)
		var features = feature_data[coords]
		var start_x = -(features.size()-1)*(icon_ui_size+8)/2.0
		for i in range(features.size()):
			var fs = Sprite2D.new()
			fs.texture = f_tex[features[i]]
			fs.scale = Vector2(icon_ui_size/fs.texture.get_size().x, icon_ui_size/fs.texture.get_size().y)
			fs.position.x = start_x + i*(icon_ui_size+8)
			container.add_child(fs)

func update_visuals(p_hex: Vector2i, max_radius: int, tile_data: Dictionary):
	for coords in tiles:
		var d_c = (abs(coords.x) + abs(coords.y) + abs(coords.x+coords.y))/2
		var d_p = (abs(coords.x-p_hex.x) + abs(coords.y-p_hex.y) + abs(coords.x+coords.y-p_hex.x-p_hex.y))/2
		var sprite = tiles[coords]
		var type = tile_data.get(coords, 0)
		
		sprite.visible = d_c <= max_radius
		if not sprite.visible: continue
		
		if d_p == 0: sprite.self_modulate = Color(1.2, 1.2, 1.2)
		elif d_p == 1:
			sprite.self_modulate = Color(0.6, 1.2, 0.6) if type > 0 else Color(0.5, 0.5, 0.5)
		else: sprite.self_modulate = Color(1, 1, 1) # 原色
