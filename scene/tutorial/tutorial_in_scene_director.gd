# 功能: 教程局内流程导演，负责在正式局内场景基础上应用固定卡组、初始 UI 锁定、Dialogic 对话与可选蒙版引导。
# 核心逻辑: 初始化时只绑定节点、隐藏时间轴、锁定输入并启动对话；蒙版默认不自动出现，由 Dialogic 事件或调试代码主动调用 show_first_combat_focus。
extends Node
class_name TutorialInSceneDirector

@export_group("教程配置")
@export var tutorial_config: TutorialConfig = preload("res://scene/tutorial/tutorial_first_run.tres")
@export var main_board_path: NodePath = NodePath("../ui/Main")
@export var timeline_ui_path: NodePath = NodePath("../ui/TimelineUI")
@export var hex_map_path: NodePath = NodePath("../map/HexMap")
@export var mask_layer_path: NodePath = NodePath("../TutorialGuideLayer/TutorialMaskLayer")

@export_group("蒙版自动显示")
@export var auto_show_combat_focus_after_map_intro: bool = false
@export var first_combat_focus_radius: float = 96.0

var main_board: Node = null
var timeline_ui: CanvasItem = null
var hex_map: Node = null
var mask_layer: TutorialMaskLayer = null


func _ready() -> void:
	call_deferred("_setup_tutorial_state")


func _setup_tutorial_state() -> void:
	await get_tree().process_frame
	main_board = get_node_or_null(main_board_path)
	timeline_ui = get_node_or_null(timeline_ui_path) as CanvasItem
	hex_map = get_node_or_null(hex_map_path)
	mask_layer = get_node_or_null(mask_layer_path) as TutorialMaskLayer

	if tutorial_config == null:
		return

	if is_instance_valid(mask_layer):
		mask_layer.clear_focus()

	if is_instance_valid(hex_map) and hex_map.has_signal("map_intro_reveal_finished"):
		if not hex_map.map_intro_reveal_finished.is_connected(_on_map_intro_reveal_finished):
			hex_map.map_intro_reveal_finished.connect(_on_map_intro_reveal_finished)

	if tutorial_config.hide_timeline_on_start and is_instance_valid(timeline_ui):
		timeline_ui.hide()

	if tutorial_config.lock_player_input_on_start:
		_set_player_input_enabled(false)
		if is_instance_valid(hex_map) and hex_map.has_method("set_tiles_interactive"):
			hex_map.set_tiles_interactive(false)

	_start_dialogic_timeline(tutorial_config.in_scene_timeline)

	if auto_show_combat_focus_after_map_intro and _can_show_focus_without_waiting_for_intro():
		show_first_combat_focus()


func _on_map_intro_reveal_finished() -> void:
	if auto_show_combat_focus_after_map_intro:
		show_first_combat_focus()


func unlock_combat_input() -> void:
	_set_player_input_enabled(true)
	if is_instance_valid(hex_map) and hex_map.has_method("set_tiles_interactive"):
		hex_map.set_tiles_interactive(true)


func show_timeline() -> void:
	if is_instance_valid(timeline_ui):
		timeline_ui.show()


func clear_mask() -> void:
	if is_instance_valid(mask_layer):
		mask_layer.clear_focus()


func show_first_combat_focus() -> void:
	if not is_instance_valid(mask_layer) or not is_instance_valid(hex_map):
		return

	var stack_nodes: Variant = hex_map.get("stack_nodes")
	if typeof(stack_nodes) != TYPE_DICTIONARY:
		return

	var stack_dict: Dictionary = stack_nodes
	if stack_dict.is_empty():
		return

	for stack in stack_dict.values():
		var stack_canvas_item: CanvasItem = stack as CanvasItem
		if is_instance_valid(stack_canvas_item):
			mask_layer.focus_world_position(stack_canvas_item, Vector2.ZERO, first_combat_focus_radius)
			return


func _set_player_input_enabled(enabled: bool) -> void:
	if not is_instance_valid(main_board):
		return
	if enabled and main_board.has_method("enable_player_inputs"):
		main_board.enable_player_inputs()
	elif not enabled and main_board.has_method("disable_player_inputs"):
		main_board.disable_player_inputs()


func _can_show_focus_without_waiting_for_intro() -> bool:
	if not is_instance_valid(hex_map):
		return false
	if not hex_map.has_method("is_map_intro_reveal_active"):
		return true
	return not hex_map.is_map_intro_reveal_active()


func _start_dialogic_timeline(timeline_name: String) -> void:
	if timeline_name.strip_edges() == "":
		return
	if not is_instance_valid(Dialogic):
		return
	Dialogic.start(timeline_name)
