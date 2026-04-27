class_name TotalEnemyHealthBar
extends Node2D

const HEALTHBAR_TEXTURE: Texture2D = preload("res://image/UI_Healthbar.png")
const HEALTHBAR_SHADER: Shader = preload("res://shaders/health_bar.gdshader")
const DEFAULT_FONT: Font = preload("res://fonts/ark-pixel-12px-proportional-zh_cn.otf")

@export_group("布局")
@export var panel_size: Vector2 = Vector2(340.0, 120.0)
@export var health_bar_size: Vector2 = Vector2(300.0, 28.0)
@export var margin_top: float = 24.0
@export var margin_left: float = 56.0

@export_group("胜利阈值")
@export var low_health_ratio: float = 0.1

@export_group("文案")
@export var title_text: String = "敌方总血量"

var hex_map: Node = null
var tracked_enemies: Dictionary = {}
var max_total_health: int = 0
var current_total_health: int = 0
var has_triggered_combat_victory: bool = false

var panel: PanelContainer
var title_label: Label
var health_bar: TextureProgressBar
var info_label: Label
var flash_timer: Timer


func _ready() -> void:
	z_index = 2000
	_build_ui()
	_resolve_hex_map()
	_connect_viewport_resize()
	call_deferred("rebuild_tracking")


func _build_ui() -> void:
	panel = PanelContainer.new()
	panel.name = "Panel"
	panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.custom_minimum_size = panel_size
	add_child(panel)

	var panel_style = StyleBoxFlat.new()
	panel_style.bg_color = Color(0.06, 0.07, 0.1, 0.88)
	panel_style.border_color = Color(0.82, 0.68, 0.28, 1.0)
	panel_style.set_border_width_all(2)
	panel_style.set_corner_radius_all(10)
	panel.add_theme_stylebox_override("panel", panel_style)

	var margin = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 16)
	margin.add_theme_constant_override("margin_right", 16)
	margin.add_theme_constant_override("margin_top", 12)
	margin.add_theme_constant_override("margin_bottom", 12)
	panel.add_child(margin)

	var layout = VBoxContainer.new()
	layout.add_theme_constant_override("separation", 8)
	margin.add_child(layout)

	title_label = Label.new()
	title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title_label.add_theme_font_override("font", DEFAULT_FONT)
	title_label.add_theme_font_size_override("font_size", 24)
	title_label.text = title_text
	layout.add_child(title_label)

	health_bar = TextureProgressBar.new()
	health_bar.name = "EnemyTotalHealth"
	health_bar.custom_minimum_size = health_bar_size
	health_bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	health_bar.texture_over = _make_atlas(Rect2(4, 26, 58, 6))
	health_bar.texture_progress = _make_atlas(Rect2(5, 42, 56, 4))
	health_bar.texture_progress_offset = Vector2(1, 1)
	health_bar.max_value = 100.0
	health_bar.value = 100.0
	health_bar.nine_patch_stretch = true

	var shader_material = ShaderMaterial.new()
	shader_material.shader = HEALTHBAR_SHADER
	shader_material.set_shader_parameter("dissolve_block_size", 40.0)
	shader_material.set_shader_parameter("dissolve_edge_color", Color(2.0, 1.5, 0.5, 1.0))
	health_bar.material = shader_material
	layout.add_child(health_bar)

	info_label = Label.new()
	info_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	info_label.add_theme_font_override("font", DEFAULT_FONT)
	info_label.add_theme_font_size_override("font_size", 18)
	info_label.text = "0 / 0 (0%)"
	layout.add_child(info_label)

	flash_timer = Timer.new()
	flash_timer.one_shot = true
	flash_timer.wait_time = 0.18
	flash_timer.timeout.connect(_on_flash_timeout)
	add_child(flash_timer)

	_update_layout()


func _make_atlas(region: Rect2) -> AtlasTexture:
	var atlas = AtlasTexture.new()
	atlas.atlas = HEALTHBAR_TEXTURE
	atlas.region = region
	return atlas


func _connect_viewport_resize() -> void:
	var viewport = get_viewport()
	if viewport and not viewport.size_changed.is_connected(_update_layout):
		viewport.size_changed.connect(_update_layout)


func _update_layout() -> void:
	if not is_instance_valid(panel):
		return

	panel.position = Vector2(
		margin_left,
		margin_top
	)
	panel.size = panel_size


func _resolve_hex_map() -> void:
	hex_map = get_node_or_null("../../map/HexMap")
	if not is_instance_valid(hex_map) and get_tree().current_scene:
		hex_map = get_tree().current_scene.get_node_or_null("map/HexMap")

	if is_instance_valid(hex_map) and hex_map.has_signal("enemy_roster_changed"):
		if not hex_map.enemy_roster_changed.is_connected(rebuild_tracking):
			hex_map.enemy_roster_changed.connect(rebuild_tracking)
	
	if Signal_Bus and Signal_Bus.has_signal("damage_dealt"):
		if not Signal_Bus.damage_dealt.is_connected(_on_damage_dealt):
			Signal_Bus.damage_dealt.connect(_on_damage_dealt)


func rebuild_tracking() -> void:
	tracked_enemies.clear()
	max_total_health = 0
	current_total_health = 0

	if not is_instance_valid(hex_map):
		_update_display()
		return
	
	var map_data = hex_map.get("map_data")
	if typeof(map_data) != TYPE_DICTIONARY:
		_update_display()
		return

	for coord in map_data.keys():
		var tile_data = map_data[coord]
		var enemy = tile_data.get("landform", null)
		if not _is_trackable_enemy(enemy):
			continue

		var hp = maxi(0, int(round(float(enemy.HP))))
		var max_hp = maxi(0, int(round(float(enemy.Max_Blood))))

		tracked_enemies[enemy] = hp
		max_total_health += max_hp
		current_total_health += hp

		var callback = Callable(self, "_on_enemy_blood_changed").bind(enemy)
		if not enemy.Blood_change.is_connected(callback):
			enemy.Blood_change.connect(callback)

	_update_display()
	_check_combat_victory()


func _is_trackable_enemy(node: Variant) -> bool:
	return is_instance_valid(node) and node is landform and node.Attitude == node.Attitude_Pool.Enemy


func _on_enemy_blood_changed(new_hp: float, enemy: Node) -> void:
	if not tracked_enemies.has(enemy):
		rebuild_tracking()
		return

	var old_hp = tracked_enemies[enemy]
	var next_hp = maxi(0, int(round(new_hp)))
	tracked_enemies[enemy] = next_hp

	var delta = next_hp - old_hp
	current_total_health = clampi(current_total_health + delta, 0, max_total_health)

	_update_display()

	if delta < 0:
		_flash_damage_feedback()

	_check_combat_victory()


func _on_damage_dealt(target: Node, _amount: int) -> void:
	if not _is_trackable_enemy(target):
		return
	
	if not tracked_enemies.has(target):
		rebuild_tracking()
		return
	
	var hp = maxi(0, int(round(float(target.HP))))
	var old_hp = tracked_enemies[target]
	if hp == old_hp:
		return
	
	tracked_enemies[target] = hp
	current_total_health = clampi(current_total_health + (hp - old_hp), 0, max_total_health)
	_update_display()
	_flash_damage_feedback()
	_check_combat_victory()


func _update_display() -> void:
	if not is_instance_valid(health_bar) or not is_instance_valid(info_label):
		return

	var safe_max = maxi(max_total_health, 1)
	health_bar.max_value = safe_max
	health_bar.value = current_total_health

	var percent = 0
	if max_total_health > 0:
		percent = int(round((float(current_total_health) / float(max_total_health)) * 100.0))

	info_label.text = "%d / %d (%d%%)" % [current_total_health, max_total_health, percent]


func _flash_damage_feedback() -> void:
	if not is_instance_valid(health_bar):
		return

	health_bar.modulate = Color(1.0, 0.65, 0.65, 1.0)
	flash_timer.start()


func _on_flash_timeout() -> void:
	if is_instance_valid(health_bar):
		health_bar.modulate = Color.WHITE


func _check_combat_victory() -> void:
	if has_triggered_combat_victory:
		return
	if max_total_health <= 0:
		return

	var ratio = float(current_total_health) / float(max_total_health)
	if ratio <= low_health_ratio:
		has_triggered_combat_victory = true
		if Signal_Bus and Signal_Bus.has_method("emit_combat_victory_triggered"):
			Signal_Bus.emit_combat_victory_triggered()
