# 功能: 教程局外场景脚本，继承正式局外流程并替换为固定地图、固定战斗入口和教程引导信号。
# 核心逻辑: _init_new_map 注入 TutorialConfig 的固定地图；_enter_room_logic 对教程战斗房间传入固定 payload 并切到 tutorial_in_scene。
extends "res://scene/out_scene/out_scene_map_exp.gd"
class_name TutorialOutScene

signal tutorial_character_confirmed(character_index: int)
signal tutorial_room_entering(room_hex: Vector2i)

@export_group("教程配置")
@export var tutorial_config: TutorialConfig = preload("res://scene/tutorial/tutorial_first_run.tres")
## 是否限制玩家只能沿教程指定路径移动。
@export var restrict_to_tutorial_path: bool = true


func _init_new_map():
	if tutorial_config == null:
		super._init_new_map()
		return

	map_seed = "tutorial_seed"
	tile_data = tutorial_config.get_default_out_map()
	tile_features = tutorial_config.get_default_out_features()
	player_hex = tutorial_config.start_hex
	player_sprite.texture = view.tex_player_unknown
	_refresh_view()
	apply_tier_camera_limit(0)
	MapState.is_initialized = true


func _move_to(target):
	if restrict_to_tutorial_path and not _is_allowed_tutorial_target(target):
		return

	var was_waiting_for_character: bool = not bool(has_cut)
	await super._move_to(target)

	if was_waiting_for_character and has_cut:
		tutorial_character_confirmed.emit(chosen_char_index)


func _enter_room_logic(target):
	if tutorial_config == null:
		await super._enter_room_logic(target)
		return

	tutorial_room_entering.emit(target)
	await get_tree().create_timer(switch_delay * Global.get_anim_speed()).timeout

	var data_str: String = str(tutorial_config.first_room_payload)
	if data_str.strip_edges() == "":
		data_str = "tutorial_battle tutorial_seed"

	if MapState.has_method("set_active_room_context"):
		MapState.set_active_room_context({
			"source_scene": "tutorial_out_scene",
			"room_hex": target,
			"room_type": tile_data.get(target, 1),
			"room_data": data_str,
			"map_seed": map_seed,
			"current_tier": current_tier,
			"is_tutorial": true,
		})

	await dim.use(0, 0)
	_switch_scene_with_data(tutorial_config.tutorial_in_scene_path, data_str)


func _is_allowed_tutorial_target(target: Vector2i) -> bool:
	if tutorial_config == null:
		return true
	if not has_cut:
		return target == tutorial_config.required_character_hex
	return target == tutorial_config.first_room_hex


func set_tutorial_path_restricted(is_restricted: bool) -> void:
	restrict_to_tutorial_path = is_restricted
