# 功能: 教程局外流程导演，负责连接固定局外地图、教程蒙版与 Dialogic 对话。
# 核心逻辑: 初始化时只完成节点绑定和信号连接；蒙版默认不自动出现，由 Dialogic 事件或调试代码主动调用 show_character_focus/show_first_room_focus。
extends Node
class_name TutorialOutSceneDirector

@export_group("教程配置")
@export var tutorial_config: TutorialConfig = preload("res://scene/tutorial/tutorial_first_run.tres")
@export var out_scene_path: NodePath = NodePath("..")
@export var mask_layer_path: NodePath = NodePath("../TutorialGuideLayer/TutorialMaskLayer")
@export var character_focus_radius: float = 92.0
@export var room_focus_radius: float = 92.0

@export_group("蒙版自动显示")
@export var auto_show_character_focus_on_start: bool = false
@export var auto_show_room_focus_after_character_confirmed: bool = false

var _active_timeline_name: String = ""
var _queued_timeline_name: String = ""
var out_scene: TutorialOutScene = null
var mask_layer: TutorialMaskLayer = null


func _ready() -> void:
	call_deferred("_setup")


func _setup() -> void:
	out_scene = get_node_or_null(out_scene_path) as TutorialOutScene
	mask_layer = get_node_or_null(mask_layer_path) as TutorialMaskLayer
	if not is_instance_valid(out_scene) or tutorial_config == null:
		return

	if is_instance_valid(mask_layer):
		mask_layer.clear_focus()

	if not out_scene.tutorial_character_confirmed.is_connected(_on_character_confirmed):
		out_scene.tutorial_character_confirmed.connect(_on_character_confirmed)
	if not out_scene.tutorial_room_entering.is_connected(_on_room_entering):
		out_scene.tutorial_room_entering.connect(_on_room_entering)
	_connect_dialogic_signals()

	if auto_show_character_focus_on_start:
		show_character_focus()

	_start_dialogic_timeline(tutorial_config.out_scene_timeline)


func show_character_focus() -> void:
	if tutorial_config == null:
		return
	_focus_out_tile(tutorial_config.required_character_hex, character_focus_radius)


func show_first_room_focus() -> void:
	if tutorial_config == null:
		return
	_focus_out_tile(tutorial_config.first_room_hex, room_focus_radius)


func clear_mask() -> void:
	if is_instance_valid(mask_layer):
		mask_layer.clear_focus()


func _on_character_confirmed(_character_index: int) -> void:
	if auto_show_room_focus_after_character_confirmed:
		show_first_room_focus()
	_start_or_queue_dialogic_timeline(tutorial_config.out_scene_after_character_timeline)


func _on_room_entering(_room_hex: Vector2i) -> void:
	clear_mask()


func _focus_out_tile(target_hex: Vector2i, radius: float) -> void:
	if not is_instance_valid(mask_layer) or not is_instance_valid(out_scene):
		return
	var world_position: Vector2 = Vector2(
		target_hex.x * out_scene.step_x,
		target_hex.y * out_scene.step_y + target_hex.x * out_scene.stagger_y
	)
	mask_layer.focus_world_position(out_scene.view, world_position, radius)


func _start_dialogic_timeline(timeline_name: String) -> void:
	if timeline_name.strip_edges() == "":
		return
	if not is_instance_valid(Dialogic):
		return
	_active_timeline_name = timeline_name
	Dialogic.start(timeline_name)


func _start_or_queue_dialogic_timeline(timeline_name: String) -> void:
	if timeline_name.strip_edges() == "":
		return
	if _active_timeline_name != "":
		_queued_timeline_name = timeline_name
		return
	_start_dialogic_timeline(timeline_name)


func _connect_dialogic_signals() -> void:
	if not is_instance_valid(Dialogic):
		return
	if Dialogic.has_signal("timeline_ended") and not Dialogic.timeline_ended.is_connected(_on_dialogic_timeline_ended):
		Dialogic.timeline_ended.connect(_on_dialogic_timeline_ended)
	if Dialogic.has_signal("signal_event") and not Dialogic.signal_event.is_connected(_on_dialogic_signal_event):
		Dialogic.signal_event.connect(_on_dialogic_signal_event)


func _on_dialogic_timeline_ended() -> void:
	_active_timeline_name = ""
	if _queued_timeline_name == "":
		return

	var next_timeline: String = _queued_timeline_name
	_queued_timeline_name = ""
	_start_dialogic_timeline(next_timeline)


func _on_dialogic_signal_event(argument: Variant) -> void:
	var signal_name: String = ""
	if typeof(argument) == TYPE_DICTIONARY:
		signal_name = str(argument.get("name", argument.get("signal", "")))
	else:
		signal_name = str(argument)

	match signal_name:
		"show_character_focus":
			show_character_focus()
		"show_first_room_focus":
			show_first_room_focus()
		"clear_mask":
			clear_mask()
