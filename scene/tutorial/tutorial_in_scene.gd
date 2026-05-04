# 功能: 教程局内根节点桥接脚本，负责在正式局内子节点 ready 前注入教程全局数据，并转发局外 payload。
# 核心逻辑: _enter_tree 提前覆盖 GlobalDB.player_deck；apply_external_event 继续把 payload 转交给正式 ui/Main，保持原切场协议不变。
extends Control
class_name TutorialInScene

@export_group("教程配置")
@export var tutorial_config: TutorialConfig = preload("res://scene/tutorial/tutorial_first_run.tres")


func _enter_tree() -> void:
	_apply_fixed_deck_before_board_ready()


func apply_external_event(payload: Variant) -> void:
	var main_board = get_node_or_null("ui/Main")
	if is_instance_valid(main_board) and main_board.has_method("apply_external_event"):
		main_board.apply_external_event(str(payload))


func _apply_fixed_deck_before_board_ready() -> void:
	if tutorial_config == null or not GlobalDB:
		return

	GlobalDB.player_deck = tutorial_config.fixed_player_deck.duplicate()
	if MapState and MapState.has_method("set_saved_deck"):
		MapState.set_saved_deck(GlobalDB.player_deck)
