# 功能: 敌方总血量 UI，汇总当前地图上所有敌方地貌的生命值。
# 核心逻辑: rebuild_tracking 负责重新收集敌人并绑定血量信号；地图入场动画期间会延后初次刷新和显示。
class_name TotalEnemyHealthBar
extends Node2D

# 这个脚本采用“累计上限”模型：
# 新敌人首次出现时把 Max_Blood 加进总上限；敌人死亡或节点消失只扣当前血量，不回退总上限。

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

@export_group("血量登记")
## 开启后，敌人首次登记进总血条后 Max_Blood 会永久计入本场上限。
## 死亡、临时消失、地块高度超限删除都只会让当前血量归零，不会减少上限。
@export var preserve_registered_max_health: bool = true
## 开启后，战斗中途生成的新敌人也会加入总血量上限。
@export var register_runtime_spawned_enemies: bool = true
## 开启后，已登记敌人离开地图或节点释放时，按“已被击败”处理为 0 当前血量。
@export var removed_enemy_counts_as_zero: bool = true
## 开启后，若已登记敌人的 Max_Blood 后续变大，总血量上限会同步增加。
@export var allow_registered_max_health_growth: bool = true

@export_group("文案")
@export var title_text: String = "敌方总血量"

@export_group("Intro Animation")
## 是否启用总血条的局内入场浮现。
@export var intro_enabled: bool = true
## 是否只在本次进入局内时播放一次。
@export var intro_play_once: bool = true
## 总血条入场时长。
@export var intro_duration: float = 0.38
## 入场时整体向下偏移的起始距离。
@export var intro_start_offset: Vector2 = Vector2(0.0, -18.0)
## 入场时的起始缩放。
@export var intro_start_scale: Vector2 = Vector2(0.92, 0.92)
## 入场动画曲线。
@export var intro_trans_type: Tween.TransitionType = Tween.TRANS_BACK
@export var intro_ease_type: Tween.EaseType = Tween.EASE_OUT

var hex_map: Node = null
## key = enemy.get_instance_id()，value = { enemy, current_hp, max_hp, is_removed }。
## 使用实例 ID 做 key，可以避免节点释放后 Dictionary 直接持有失效对象作为 key。
var tracked_enemies: Dictionary = {}
var max_total_health: int = 0
var current_total_health: int = 0
var has_triggered_combat_victory: bool = false
var _intro_has_played: bool = false
var _intro_in_progress: bool = false
var _has_completed_initial_roster_sync: bool = false

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
	if _should_wait_for_map_intro_reveal():
		hide()
		if not hex_map.map_intro_reveal_finished.is_connected(_on_map_intro_reveal_finished):
			hex_map.map_intro_reveal_finished.connect(_on_map_intro_reveal_finished)
	else:
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


## 初次进入局内时，等待 HexMap 的地块涟漪入场完成后再显示/刷新总血量。
func _should_wait_for_map_intro_reveal() -> bool:
	return (
		is_instance_valid(hex_map)
		and hex_map.has_signal("map_intro_reveal_finished")
		and hex_map.has_method("is_map_intro_reveal_active")
		and hex_map.is_map_intro_reveal_active()
	)


func _on_map_intro_reveal_finished() -> void:
	rebuild_tracking()


func rebuild_tracking() -> void:
	var seen_enemy_ids: Dictionary = {}

	if not is_instance_valid(hex_map):
		_recalculate_current_total_health()
		_update_display()
		return
	
	var map_data = hex_map.get("map_data")
	if typeof(map_data) != TYPE_DICTIONARY:
		_recalculate_current_total_health()
		_update_display()
		return

	for coord in map_data.keys():
		var tile_data = map_data[coord]
		if typeof(tile_data) != TYPE_DICTIONARY:
			continue

		var enemy = tile_data.get("landform", null)
		if not _is_trackable_enemy(enemy):
			continue

		var enemy_id := _get_enemy_tracking_id(enemy)
		seen_enemy_ids[enemy_id] = true
		_register_or_refresh_enemy(enemy)

	for enemy_id in tracked_enemies.keys():
		if seen_enemy_ids.has(enemy_id):
			continue
		_mark_enemy_removed(enemy_id, false)

	_has_completed_initial_roster_sync = true
	_recalculate_current_total_health()
	_update_display()
	_check_combat_victory()


func _is_trackable_enemy(node: Variant) -> bool:
	return is_instance_valid(node) and node is landform and node.Attitude == node.Attitude_Pool.Enemy


func _get_enemy_tracking_id(enemy: Node) -> int:
	return int(enemy.get_instance_id())


func _get_enemy_max_health(enemy: Node) -> int:
	if not is_instance_valid(enemy):
		return 0
	return maxi(0, int(round(float(enemy.Max_Blood))))


func _get_enemy_current_health(enemy: Node) -> int:
	if not is_instance_valid(enemy):
		return 0

	var hp := maxi(0, int(round(float(enemy.HP))))
	var max_hp := _get_enemy_max_health(enemy)
	if max_hp > 0:
		hp = mini(hp, max_hp)
	return hp


## 登记新敌人，或刷新已登记敌人的节点引用与当前血量。
## 上限默认只增不减，避免死亡/消失导致胜利阈值的分母越来越小。
func _register_or_refresh_enemy(enemy: Node) -> void:
	if not _is_trackable_enemy(enemy):
		return

	var enemy_id := _get_enemy_tracking_id(enemy)
	var observed_max_hp := _get_enemy_max_health(enemy)
	var observed_hp := _get_enemy_current_health(enemy)

	if not tracked_enemies.has(enemy_id):
		if _has_completed_initial_roster_sync and not register_runtime_spawned_enemies:
			return

		tracked_enemies[enemy_id] = {
			"enemy": enemy,
			"max_hp": observed_max_hp,
			"current_hp": observed_hp,
			"is_removed": false,
		}
		max_total_health += observed_max_hp
		current_total_health = clampi(current_total_health + observed_hp, 0, max_total_health)
		_connect_enemy_health_signals(enemy, enemy_id)
		return

	var data: Dictionary = tracked_enemies[enemy_id]
	var old_max_hp := int(data.get("max_hp", 0))
	if observed_max_hp > old_max_hp and allow_registered_max_health_growth:
		max_total_health += observed_max_hp - old_max_hp
		data["max_hp"] = observed_max_hp
	elif observed_max_hp < old_max_hp and not preserve_registered_max_health:
		max_total_health = maxi(0, max_total_health - (old_max_hp - observed_max_hp))
		data["max_hp"] = observed_max_hp

	data["enemy"] = enemy
	data["is_removed"] = false
	tracked_enemies[enemy_id] = data
	_connect_enemy_health_signals(enemy, enemy_id)
	_set_tracked_enemy_current_hp(enemy_id, observed_hp, false)


func _connect_enemy_health_signals(enemy: Node, enemy_id: int) -> void:
	if not is_instance_valid(enemy):
		return

	var blood_callback = Callable(self, "_on_enemy_blood_changed").bind(enemy_id)
	if enemy.has_signal("Blood_change") and not enemy.Blood_change.is_connected(blood_callback):
		enemy.Blood_change.connect(blood_callback)

	var exit_callback = Callable(self, "_on_enemy_tree_exited").bind(enemy_id)
	if not enemy.tree_exited.is_connected(exit_callback):
		enemy.tree_exited.connect(exit_callback)


func _set_tracked_enemy_current_hp(enemy_id: int, next_hp: int, should_flash: bool) -> void:
	if not tracked_enemies.has(enemy_id):
		return

	var data: Dictionary = tracked_enemies[enemy_id]
	var old_hp := int(data.get("current_hp", 0))
	var max_hp := int(data.get("max_hp", 0))
	if max_hp > 0:
		next_hp = clampi(next_hp, 0, max_hp)
	else:
		next_hp = maxi(0, next_hp)

	if next_hp == old_hp:
		return

	var delta := next_hp - old_hp
	data["current_hp"] = next_hp
	tracked_enemies[enemy_id] = data
	current_total_health = clampi(current_total_health + delta, 0, max_total_health)

	_update_display()

	if should_flash and delta < 0:
		_flash_damage_feedback()

	_check_combat_victory()


func _mark_enemy_removed(enemy_id: int, should_flash: bool) -> void:
	if not tracked_enemies.has(enemy_id):
		return

	var data: Dictionary = tracked_enemies[enemy_id]
	data["is_removed"] = true
	tracked_enemies[enemy_id] = data

	if removed_enemy_counts_as_zero:
		_set_tracked_enemy_current_hp(enemy_id, 0, should_flash)


func _recalculate_current_total_health() -> void:
	current_total_health = 0
	for enemy_id in tracked_enemies.keys():
		var data: Dictionary = tracked_enemies[enemy_id]
		current_total_health += maxi(0, int(data.get("current_hp", 0)))
	current_total_health = clampi(current_total_health, 0, max_total_health)


func _on_enemy_blood_changed(new_hp: float, enemy_id: int) -> void:
	if not tracked_enemies.has(enemy_id):
		rebuild_tracking()
		return

	var next_hp = maxi(0, int(round(new_hp)))
	_set_tracked_enemy_current_hp(enemy_id, next_hp, true)


## tree_exited 也会在重挂父节点时短暂触发，所以延后一帧确认它是否真的离开战斗。
func _on_enemy_tree_exited(enemy_id: int) -> void:
	call_deferred("_resolve_enemy_tree_exit", enemy_id)


func _resolve_enemy_tree_exit(enemy_id: int) -> void:
	if not tracked_enemies.has(enemy_id):
		return

	var data: Dictionary = tracked_enemies[enemy_id]
	var enemy = data.get("enemy", null)
	if is_instance_valid(enemy) and enemy.is_inside_tree():
		return

	_mark_enemy_removed(enemy_id, false)
	_update_display()
	_check_combat_victory()


func _on_damage_dealt(target: Node, _amount: int) -> void:
	if not _is_trackable_enemy(target):
		return
	
	var enemy_id := _get_enemy_tracking_id(target)
	if not tracked_enemies.has(enemy_id):
		_register_or_refresh_enemy(target)
	if not tracked_enemies.has(enemy_id):
		return
	
	var hp = _get_enemy_current_health(target)
	var data: Dictionary = tracked_enemies[enemy_id]
	var old_hp := int(data.get("current_hp", 0))
	if hp == old_hp:
		return
	
	_set_tracked_enemy_current_hp(enemy_id, hp, hp < old_hp)


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


## 局内首波 UI 入场时调用。会和 TimelineUI、CartoonUI 一起并行播放。
func play_intro() -> void:
	if not intro_enabled:
		show()
		_apply_intro_final_state()
		_intro_has_played = true
		_intro_in_progress = false
		return
	if _intro_in_progress:
		return
	if _intro_has_played and intro_play_once:
		show()
		_apply_intro_final_state()
		return

	show()
	_intro_in_progress = true
	_apply_intro_hidden_state()

	var safe_duration := maxf(intro_duration, 0.01)
	var tween := create_tween().set_parallel(true)
	tween.tween_property(self, "position", Vector2.ZERO, safe_duration).set_trans(intro_trans_type).set_ease(intro_ease_type)
	tween.tween_property(self, "scale", Vector2.ONE, safe_duration).set_trans(intro_trans_type).set_ease(intro_ease_type)
	tween.tween_property(self, "modulate:a", 1.0, safe_duration * 0.85).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	tween.finished.connect(_finish_intro, CONNECT_ONE_SHOT)


func is_intro_in_progress() -> bool:
	return _intro_in_progress


func _apply_intro_hidden_state() -> void:
	position = intro_start_offset
	scale = intro_start_scale
	modulate.a = 0.0


func _apply_intro_final_state() -> void:
	position = Vector2.ZERO
	scale = Vector2.ONE
	modulate.a = 1.0


func _finish_intro() -> void:
	_intro_in_progress = false
	_intro_has_played = true
	_apply_intro_final_state()


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
