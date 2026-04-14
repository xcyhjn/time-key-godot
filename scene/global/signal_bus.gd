extends Node

class_name SignalBus

# ==========================================
# 回合管理信号
# ==========================================
signal turn_started(era_value: int)
signal turn_ended()
signal combat_ended()
signal new_turn_starting()
signal step_next(step: int, behavior: int)  # 新增：建筑行为触发信号

# ==========================================
# 卡牌系统信号
# ==========================================
signal card_drawn(card: Control, count: int)
signal card_discarded(card: Control)
signal card_played(card: Control)
signal deck_shuffled()
signal hand_full()
signal deck_empty()
signal discard_pile_empty()

# ==========================================
# 时间轴信�?# ==========================================
signal timeline_action_added(action: TimelineAction)
signal timeline_action_removed(action: TimelineAction)
signal timeline_action_hovered(action: TimelineAction, is_hovering: bool)
signal timeline_resolve_requested()
signal timeline_resolved()
signal timeline_cleared()

# ==========================================
# UI更新信号
# ==========================================
signal ui_needs_update()
signal tooltip_shown(card: Control)
signal tooltip_hidden()
signal deck_count_updated(count: int)
signal discard_count_updated(count: int)

# ==========================================
# 游戏状态信�?# ==========================================
signal player_inputs_enabled()
signal player_inputs_disabled()
signal game_paused()
signal game_resumed()
signal victory_triggered()
signal defeat_triggered()
# ==========================================
# 时间币信号
# ==========================================
signal timecoin_updated(current_amount: int, delta: int)
signal timecoin_insufficient(requested: int, available: int)

# ==========================================
# 战斗相关信号
# ==========================================
signal enemy_intent_generated(enemy: Node)
signal damage_dealt(target: Node, amount: int)
signal damage_received(source: Node, amount: int)
signal enemy_defeated(enemy: Node)
signal player_health_changed(new_health: int, delta: int)

# ==========================================
# 地图和地块信�?# ==========================================
signal tile_hovered(tile: Area2D, is_hovering: bool)
signal tile_selected(tile: Area2D)
signal tile_deselected(tile: Area2D)
signal valid_target_hovered(tile: Area2D, card: Control)

# ==========================================
# 调试和日志（可选）
# ==========================================
@export var enable_signal_logging: bool = false


func _ready() -> void:
	if enable_signal_logging:
		GameLogger.info("[SignalBus] 信号总线已初始化，日志记录已启用", "SignalBus")


func log_signal(signal_name: String, args: Array = []) -> void:
	if not enable_signal_logging:
		return

	var arg_str = ""
	if args.size() > 0:
		arg_str = " -> " + str(args)

	GameLogger.debug("[SignalBus] %s%s" % [signal_name, arg_str], "SignalBus")

# ==========================================
# 辅助函数：安全发送信号（带日志）
# ==========================================


func emit_turn_started(era_value: int) -> void:
	log_signal("turn_started", [era_value])
	turn_started.emit(era_value)


func emit_turn_ended() -> void:
	log_signal("turn_ended")
	turn_ended.emit()


func emit_combat_ended() -> void:
	log_signal("combat_ended")
	combat_ended.emit()


func emit_card_drawn(card: Control, count: int) -> void:
	log_signal("card_drawn", [card, count])
	card_drawn.emit(card, count)


func emit_card_discarded(card: Control) -> void:
	log_signal("card_discarded", [card])
	card_discarded.emit(card)


func emit_card_played(card: Control) -> void:
	log_signal("card_played", [card])
	card_played.emit(card)


func emit_deck_shuffled() -> void:
	log_signal("deck_shuffled")
	deck_shuffled.emit()


func emit_timeline_action_added(action: TimelineAction) -> void:
	log_signal("timeline_action_added", [action])
	timeline_action_added.emit(action)


func emit_timeline_action_removed(action: TimelineAction) -> void:
	log_signal("timeline_action_removed", [action])
	timeline_action_removed.emit(action)


func emit_timeline_action_hovered(action: TimelineAction, is_hovering: bool) -> void:
	log_signal("timeline_action_hovered", [action, is_hovering])
	timeline_action_hovered.emit(action, is_hovering)


func emit_timeline_resolve_requested() -> void:
	log_signal("timeline_resolve_requested")
	timeline_resolve_requested.emit()


func emit_timeline_resolved() -> void:
	log_signal("timeline_resolved")
	timeline_resolved.emit()


func emit_ui_needs_update() -> void:
	log_signal("ui_needs_update")
	ui_needs_update.emit()


func emit_tooltip_shown(card: Control) -> void:
	log_signal("tooltip_shown", [card])
	tooltip_shown.emit(card)


func emit_tooltip_hidden() -> void:
	log_signal("tooltip_hidden")
	tooltip_hidden.emit()


func emit_player_inputs_enabled() -> void:
	log_signal("player_inputs_enabled")
	player_inputs_enabled.emit()


func emit_player_inputs_disabled() -> void:
	log_signal("player_inputs_disabled")
	player_inputs_disabled.emit()


func emit_damage_dealt(target: Node, amount: int) -> void:
	log_signal("damage_dealt", [target, amount])
	damage_dealt.emit(target, amount)


func emit_enemy_intent_generated(enemy: Node) -> void:
	log_signal("enemy_intent_generated", [enemy])
	enemy_intent_generated.emit(enemy)


func emit_tile_hovered(tile: Area2D, is_hovering: bool) -> void:
	log_signal("tile_hovered", [tile, is_hovering])
	tile_hovered.emit(tile, is_hovering)


func emit_valid_target_hovered(tile: Area2D, card: Control) -> void:
	log_signal("valid_target_hovered", [tile, card])
	valid_target_hovered.emit(tile, card)


func emit_timecoin_updated(current_amount: int, delta: int) -> void:
	log_signal("timecoin_updated", [current_amount, delta])
	timecoin_updated.emit(current_amount, delta)


func emit_timecoin_insufficient(requested: int, available: int) -> void:
	log_signal("timecoin_insufficient", [requested, available])
	timecoin_insufficient.emit(requested, available)

func emit_defeat_triggered() -> void:
	log_signal("defeat_triggered")
	defeat_triggered.emit()
