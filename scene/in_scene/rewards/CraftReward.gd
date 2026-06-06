extends CanvasLayer

signal reward_scene_close_requested(scene_instance: Node)

var CardManager = preload("res://addons/card-framework/card_manager.gd")
var draft_card_scene = preload("res://scene/card/DraftCard.tscn")
const CraftRecipeResolverScript = preload("res://scene/in_scene/rewards/rules/CraftRecipeResolver.gd")
const CraftConnectionLinePresenterScript = preload("res://scene/in_scene/rewards/presenters/CraftConnectionLinePresenter.gd")
const CraftResultDescriptionPanelPresenterScript = preload("res://scene/in_scene/rewards/presenters/CraftResultDescriptionPanelPresenter.gd")
const CraftSlotPreviewLayoutPresenterScript = preload("res://scene/in_scene/rewards/presenters/CraftSlotPreviewLayoutPresenter.gd")
const CraftResultDescriptionPositionPresenterScript = preload("res://scene/in_scene/rewards/presenters/CraftResultDescriptionPositionPresenter.gd")
const RewardTooltipAdapterScript = preload("res://scene/in_scene/rewards/presenters/RewardTooltipAdapter.gd")
const RewardTempPileFactoryScript = preload("res://scene/in_scene/rewards/factory/RewardTempPileFactory.gd")
const RewardCardDescriptionExtractorScript = preload("res://scene/in_scene/rewards/presenters/RewardCardDescriptionExtractor.gd")
const RewardCardTextureExtractorScript = preload("res://scene/in_scene/rewards/presenters/RewardCardTextureExtractor.gd")
const RewardRealCardSpawnerScript = preload("res://scene/in_scene/rewards/factory/RewardRealCardSpawner.gd")

enum CraftMode {
	BOARD,
	SELECTING
}

const SLOT_1 := 1
const SLOT_2 := 2

const CRAFTING_RECIPES := {
	# _get_recipe_result() 会自动检查反向 key，因此这里写一条 wind + tower 配方即可。
	"wind_tower": "tornado",
	"lighting_earthquake": "poison",
}

@onready var background_mask: Panel = $BackgroundMask
@onready var title_label: Label = $TitleLabel
@onready var crafting_board: Control = $CraftingBoard
@onready var slot1: Button = $CraftingBoard/Slot1
@onready var slot1_placeholder: Label = $CraftingBoard/Slot1/PlaceholderLabel
@onready var slot1_anchor: Control = $CraftingBoard/Slot1/CardAnchor
@onready var slot2: Button = $CraftingBoard/Slot2
@onready var slot2_placeholder: Label = $CraftingBoard/Slot2/PlaceholderLabel
@onready var slot2_anchor: Control = $CraftingBoard/Slot2/CardAnchor
@onready var result_slot: Button = $CraftingBoard/ResultSlot
@onready var result_placeholder: Label = $CraftingBoard/ResultSlot/PlaceholderLabel
@onready var result_anchor: Control = $CraftingBoard/ResultSlot/CardAnchor
@onready var connection_lines: Line2D = $CraftingBoard/ConnectionLines
@onready var selection_mask: ColorRect = $SelectionMask
@onready var selection_title_label: Label = $SelectionTitleLabel
@onready var deck_scroll_container: ScrollContainer = $DeckScrollContainer
@onready var deck_grid: GridContainer = $DeckScrollContainer/DeckGrid
@onready var result_description_panel: PanelContainer = $ResultDescriptionPanel
@onready var result_description_label: RichTextLabel = $ResultDescriptionPanel/ResultDescriptionMargin/ResultDescriptionLabel
@onready var btn_back: Button = $BtnBack
@onready var btn_confirm: Button = $BtnConfirm
@onready var exit_warning_dialog: ConfirmationDialog = $ExitWarningDialog
@onready var warning_label: Label = $ExitWarningDialog/WarningLabel

var deck_manager = null

var craft_mode: CraftMode = CraftMode.BOARD
var active_slot_index: int = 0
var pending_selected_entry: Dictionary = {}
var slot_entries := {
	SLOT_1: null,
	SLOT_2: null,
}
var slot_preview_cards := {
	SLOT_1: null,
	SLOT_2: null,
}
var result_preview_card: Control = null
var current_result_card_id: String = ""
var current_deck_cards: Array = []
var current_deck_entries: Array = []
var can_close_selection_without_choice: bool = false
var _recipe_resolver = null
var _connection_line_presenter = null
var _result_description_panel_presenter = null
var _slot_preview_layout_presenter = null
var _result_description_position_presenter = null
var _tooltip_adapter = null
var _temp_pile_factory = null
var _card_description_extractor = null
var _card_texture_extractor = null
var _real_card_spawner = null

## 合成页面内部也复用统一的卡牌 Hover Tooltip。
var tooltip_presenter: CardTooltipPresenter = null


func _get_recipe_resolver():
	if _recipe_resolver == null:
		_recipe_resolver = CraftRecipeResolverScript.new()
	return _recipe_resolver


func _get_connection_line_presenter():
	if _connection_line_presenter == null:
		_connection_line_presenter = CraftConnectionLinePresenterScript.new()
	return _connection_line_presenter


func _get_result_description_panel_presenter():
	if _result_description_panel_presenter == null:
		_result_description_panel_presenter = CraftResultDescriptionPanelPresenterScript.new()
	return _result_description_panel_presenter


func _get_slot_preview_layout_presenter():
	if _slot_preview_layout_presenter == null:
		_slot_preview_layout_presenter = CraftSlotPreviewLayoutPresenterScript.new()
	return _slot_preview_layout_presenter


func _get_result_description_position_presenter():
	if _result_description_position_presenter == null:
		_result_description_position_presenter = CraftResultDescriptionPositionPresenterScript.new()
	return _result_description_position_presenter


func _get_tooltip_adapter():
	if _tooltip_adapter == null:
		_tooltip_adapter = RewardTooltipAdapterScript.new()
	return _tooltip_adapter


func _get_temp_pile_factory():
	if _temp_pile_factory == null:
		_temp_pile_factory = RewardTempPileFactoryScript.new()
	return _temp_pile_factory


func _get_card_description_extractor():
	if _card_description_extractor == null:
		_card_description_extractor = RewardCardDescriptionExtractorScript.new()
	return _card_description_extractor


func _get_card_texture_extractor():
	if _card_texture_extractor == null:
		_card_texture_extractor = RewardCardTextureExtractorScript.new()
	return _card_texture_extractor


func _get_real_card_spawner():
	if _real_card_spawner == null:
		_real_card_spawner = RewardRealCardSpawnerScript.new()
	return _real_card_spawner


func _object_has_property(target: Object, property_name: StringName) -> bool:
	if target == null:
		return false
	for property_info in target.get_property_list():
		if property_info.get("name", &"") == property_name:
			return true
	return false

@export_group("场景配置")
@export var deck_grid_columns: int = 4

@export_group("卡牌显示")
@export var card_display_size: Vector2 = Vector2(125, 175)
@export var card_spacing_x: int = 20
@export var card_spacing_y: int = 20
@export var slot_preview_padding: Vector2 = Vector2(16, 16)
@export var incompatible_card_alpha: float = 0.35

@export_group("文案")
@export var board_title_text: String = "合成卡牌"
@export var select_slot_1_text: String = "请选择主卡"
@export var select_slot_2_text: String = "请选择副卡"
@export var no_recipe_text: String = "无可用配方"

@export_group("结果文本框")
@export var result_tooltip_offset_x: int = 18
@export var result_tooltip_offset_y: int = 0
@export var result_tooltip_max_width: int = 260

@export_group("Tooltip资源配置")
@export var tooltip_config: TooltipConfig = preload("res://scene/shared/tooltip/reward_card_tooltip_config.tres")


func _ready() -> void:
	slot1.pressed.connect(_on_slot_pressed.bind(SLOT_1))
	slot2.pressed.connect(_on_slot_pressed.bind(SLOT_2))
	result_slot.pressed.connect(_on_result_slot_pressed)
	btn_back.pressed.connect(_on_back_pressed)
	btn_confirm.pressed.connect(_on_confirm_pressed)
	exit_warning_dialog.confirmed.connect(_on_exit_warning_confirmed)

	await get_tree().process_frame

	if not deck_manager:
		_try_find_card_manager()

	title_label.text = board_title_text
	deck_grid.columns = deck_grid_columns
	deck_grid.add_theme_constant_override("h_separation", card_spacing_x)
	deck_grid.add_theme_constant_override("v_separation", card_spacing_y)
	connection_lines.points = PackedVector2Array()
	_apply_preview_padding()
	_setup_result_description_panel()
	_setup_tooltip_presenter()

	_reset_crafting_state()
	_set_board_mode()


func open() -> void:
	show()
	background_mask.show()
	_reset_crafting_state()
	_set_board_mode()


func close() -> void:
	hide()
	background_mask.hide()
	_reset_crafting_state()

	reward_scene_close_requested.emit(self)


func set_deck_manager(manager) -> void:
	deck_manager = manager


func _set_board_mode() -> void:
	craft_mode = CraftMode.BOARD
	active_slot_index = 0
	pending_selected_entry.clear()
	can_close_selection_without_choice = false

	selection_mask.hide()
	selection_title_label.hide()
	deck_scroll_container.hide()
	btn_confirm.show()
	btn_confirm.text = "确认"
	btn_back.text = "退出"

	_refresh_slot_placeholders()
	_update_result_description()
	_update_connection_lines()
	_refresh_reward_action_buttons()


func _set_selection_mode(slot_index: int) -> void:
	craft_mode = CraftMode.SELECTING
	active_slot_index = slot_index
	pending_selected_entry.clear()
	can_close_selection_without_choice = false

	selection_mask.show()
	selection_title_label.show()
	deck_scroll_container.show()
	btn_confirm.show()
	btn_confirm.text = "确认"
	btn_confirm.disabled = true
	btn_back.disabled = false


func _on_slot_pressed(slot_index: int) -> void:
	if craft_mode == CraftMode.SELECTING:
		return

	await _open_selection_for_slot(slot_index)


func _open_selection_for_slot(slot_index: int) -> void:
	_set_selection_mode(slot_index)
	await _generate_selection_cards(slot_index)


func _generate_selection_cards(slot_index: int) -> void:
	_clear_selection_deck()
	current_deck_entries = _build_selection_entries(slot_index)
	selection_title_label.text = _build_selection_title(slot_index)

	if current_deck_entries.is_empty():
		can_close_selection_without_choice = true
		btn_confirm.disabled = false
		btn_confirm.text = "返回"
		return

	var temp_pile = _create_temp_pile()
	if temp_pile == null:
		return
	temp_pile.visible = false

	for entry in current_deck_entries:
		await _create_selection_card(entry, temp_pile)

	temp_pile.queue_free()

	if not pending_selected_entry.is_empty():
		_apply_selected_entry_to_grid(_entry_key(pending_selected_entry))
		btn_confirm.disabled = false
		btn_back.disabled = true
	elif not _has_clickable_entry(current_deck_entries):
		can_close_selection_without_choice = true
		btn_confirm.disabled = false
		btn_confirm.text = "返回"


func _build_selection_entries(slot_index: int) -> Array:
	var deck_ids: Array[String] = _get_current_deck_card_ids()
	var other_slot_index = SLOT_2 if slot_index == SLOT_1 else SLOT_1
	var current_entry = slot_entries[slot_index]
	var other_entry = slot_entries[other_slot_index]
	var base_entries: Array = []

	for i in range(deck_ids.size()):
		if other_entry != null and i == other_entry["deck_index"]:
			continue

		base_entries.append({
			"deck_index": i,
			"card_id": deck_ids[i],
			"disabled": false,
			"dimmed": false,
			"selected": false,
		})

	if current_entry != null:
		var ordered_entries: Array = []
		for entry in base_entries:
			if entry["deck_index"] == current_entry["deck_index"]:
				entry["selected"] = true
				pending_selected_entry = _copy_entry(entry)
				ordered_entries.append(entry)
				break
		for entry in base_entries:
			if current_entry == null or entry["deck_index"] != current_entry["deck_index"]:
				ordered_entries.append(entry)
		return ordered_entries

	if other_entry != null:
		var compatible_entries: Array = []
		var incompatible_entries: Array = []

		for entry in base_entries:
			if _get_recipe_result(other_entry["card_id"], entry["card_id"]) != "":
				compatible_entries.append(entry)
			else:
				entry["disabled"] = true
				entry["dimmed"] = true
				incompatible_entries.append(entry)

		if compatible_entries.is_empty():
			can_close_selection_without_choice = true

		return compatible_entries + incompatible_entries

	return base_entries


func _build_selection_title(slot_index: int) -> String:
	var current_entry = slot_entries[slot_index]
	var other_slot_index = SLOT_2 if slot_index == SLOT_1 else SLOT_1
	var other_entry = slot_entries[other_slot_index]

	if current_entry != null:
		return "重新选择槽位 %d 卡牌（当前卡已置顶并高亮）" % slot_index

	if other_entry != null:
		return "请选择与槽位 %d 可融合的卡牌（可融合卡在前，不可融合卡已变暗）" % other_slot_index

	return select_slot_1_text if slot_index == SLOT_1 else select_slot_2_text


func _create_selection_card(entry: Dictionary, temp_pile: Node) -> void:
	var draft_card = draft_card_scene.instantiate()
	draft_card.card_id = entry["card_id"]
	draft_card.custom_set_size = card_display_size

	await _steal_card_data(entry["card_id"], draft_card, temp_pile)

	deck_grid.add_child(draft_card)
	current_deck_cards.append(draft_card)
	draft_card.set_meta("deck_index", entry["deck_index"])
	draft_card.call_deferred("set", "original_scale", draft_card.scale)

	if entry["selected"] and draft_card.has_method("set_selected"):
		draft_card.set_selected(true)
		if draft_card.has_method("set_tooltip_enabled"):
			draft_card.set_tooltip_enabled(true)

	if entry["dimmed"]:
		if draft_card.has_method("set_dimmed"):
			draft_card.set_dimmed(true, 1.0 - incompatible_card_alpha)
		if draft_card.has_method("set_hover_effect_enabled"):
			draft_card.set_hover_effect_enabled(false)
	else:
		if draft_card.has_method("set_dimmed"):
			draft_card.set_dimmed(false)
		if draft_card.has_method("set_hover_effect_enabled"):
			draft_card.set_hover_effect_enabled(true)

	if entry["disabled"]:
		draft_card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		if draft_card.has_method("set_tooltip_enabled"):
			draft_card.set_tooltip_enabled(false)
	else:
		draft_card.card_clicked.connect(_on_deck_card_clicked.bind(entry))


func _on_deck_card_clicked(clicked_card: Control, entry: Dictionary) -> void:
	# 如果当前槽位已有卡牌，并且玩家再次点击同一张卡，
	# 就把这个槽位清空，回到“未操作/可重新退出”的状态。
	var current_entry = slot_entries[active_slot_index]
	if current_entry != null and current_entry["deck_index"] == entry["deck_index"]:
		await _clear_active_slot_selection()
		return

	# 选择新卡后，退出按钮禁用，确认按钮启用。
	# 这样选择阶段也遵守“有操作先确认/取消，不能直接退出”的规则。
	pending_selected_entry = _copy_entry(entry)
	_apply_selected_entry_to_grid(entry["deck_index"])
	btn_confirm.disabled = false
	btn_confirm.text = "确认"
	btn_back.disabled = true


func _apply_selected_entry_to_grid(deck_index: int) -> void:
	for card in current_deck_cards:
		if not is_instance_valid(card):
			continue

		var is_selected = card.get_meta("deck_index") == deck_index
		if card.has_method("set_selected"):
			card.set_selected(is_selected)


func _on_confirm_pressed() -> void:
	if craft_mode == CraftMode.BOARD:
		if current_result_card_id.is_empty():
			return
		await _claim_result_card()
		return

	if can_close_selection_without_choice and pending_selected_entry.is_empty():
		_set_board_mode()
		return

	if pending_selected_entry.is_empty():
		return

	await _commit_pending_selection()


## 清空当前正在选择的槽位。
## 这是合成页的“再次点击选定卡牌取消选择”入口，
## 会同步移除槽位预览、结果预览，并回到棋盘模式刷新按钮状态。
func _clear_active_slot_selection() -> void:
	var slot_index = active_slot_index
	slot_entries[slot_index] = null
	pending_selected_entry.clear()
	_clear_slot_preview(slot_index)
	await _refresh_result_preview()
	_clear_selection_deck()
	_set_board_mode()


func _commit_pending_selection() -> void:
	slot_entries[active_slot_index] = _copy_entry(pending_selected_entry)
	await _render_slot_preview(active_slot_index)
	await _refresh_result_preview()
	_clear_selection_deck()
	_set_board_mode()


func _render_slot_preview(slot_index: int) -> void:
	_clear_slot_preview(slot_index)

	var entry = slot_entries[slot_index]
	if entry == null:
		_refresh_slot_placeholders()
		return

	var anchor = _get_slot_anchor(slot_index)
	if not is_instance_valid(anchor):
		return

	var preview_card = await _create_preview_card(entry["card_id"], _get_anchor_preview_size(anchor), false)
	if preview_card == null:
		return
	anchor.add_child(preview_card)
	preview_card.position = Vector2.ZERO
	preview_card.mouse_filter = Control.MOUSE_FILTER_IGNORE
	if preview_card.has_method("set_tooltip_enabled"):
		preview_card.set_tooltip_enabled(false)
	if preview_card.has_method("set_hover_effect_enabled"):
		preview_card.set_hover_effect_enabled(false)
	slot_preview_cards[slot_index] = preview_card
	_refresh_slot_placeholders()


func _refresh_result_preview() -> void:
	_clear_result_preview()

	if slot_entries[SLOT_1] == null or slot_entries[SLOT_2] == null:
		_refresh_slot_placeholders()
		_update_connection_lines()
		return

	current_result_card_id = _get_recipe_result(slot_entries[SLOT_1]["card_id"], slot_entries[SLOT_2]["card_id"])
	if current_result_card_id.is_empty():
		_refresh_slot_placeholders()
		_update_connection_lines()
		return

	var preview_card = await _create_preview_card(current_result_card_id, _get_anchor_preview_size(result_anchor), true)
	if preview_card == null:
		return
	result_anchor.add_child(preview_card)
	preview_card.position = Vector2.ZERO
	result_preview_card = preview_card

	if result_preview_card.has_method("set_selected"):
		result_preview_card.set_selected(true)

	if result_preview_card.has_signal("card_clicked"):
		result_preview_card.card_clicked.connect(_on_result_card_clicked)

	_refresh_slot_placeholders()
	_update_result_description()
	_update_connection_lines()


func _create_preview_card(card_id: String, preview_size: Vector2, tooltip_enabled: bool) -> Control:
	var temp_pile = _create_temp_pile()
	if temp_pile == null:
		return null
	temp_pile.visible = false

	var draft_card = draft_card_scene.instantiate()
	draft_card.card_id = card_id
	draft_card.custom_set_size = preview_size
	await _steal_card_data(card_id, draft_card, temp_pile)
	temp_pile.queue_free()

	draft_card.custom_minimum_size = preview_size
	draft_card.size = preview_size
	draft_card.position = Vector2.ZERO

	if not tooltip_enabled:
		draft_card.mouse_filter = Control.MOUSE_FILTER_IGNORE

	return draft_card


func _on_result_card_clicked(_card: Control) -> void:
	_refresh_reward_action_buttons()


func _on_result_slot_pressed() -> void:
	_refresh_reward_action_buttons()


func _claim_result_card() -> void:
	if craft_mode == CraftMode.SELECTING:
		return
	if current_result_card_id.is_empty():
		return
	if slot_entries[SLOT_1] == null or slot_entries[SLOT_2] == null:
		return

	btn_back.disabled = true

	if is_instance_valid(result_preview_card):
		var tw = create_tween().set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
		tw.tween_property(result_preview_card, "scale", Vector2(1.15, 1.15), 0.12)
		tw.tween_property(result_preview_card, "scale", Vector2.ONE, 0.1)
		await tw.finished

	_apply_crafting_result_to_deck()
	hide_tooltip()
	_reset_crafting_state()
	set_meta("settlement_reward_committed", true)
	close()


func _apply_crafting_result_to_deck() -> void:
	var remove_indices := [
		slot_entries[SLOT_1]["deck_index"],
		slot_entries[SLOT_2]["deck_index"],
	]
	remove_indices.sort()
	remove_indices.reverse()

	for idx in remove_indices:
		if idx >= 0 and idx < GlobalDB.player_deck.size():
			GlobalDB.player_deck.remove_at(idx)

	GlobalDB.player_deck.append(current_result_card_id)

	var main = get_tree().get_first_node_in_group("MainBoard")
	if (
		deck_manager
		and deck_manager.has_method("sync_runtime_deck_from_global")
		and main
		and main.deck_pile
	):
		deck_manager.sync_runtime_deck_from_global(main.deck_pile)
		if main.has_method("update_counts_and_ui"):
			main.update_counts_and_ui()


func _on_back_pressed() -> void:
	if btn_back.disabled:
		return
	if _has_unfinished_crafting():
		exit_warning_dialog.popup_centered()
		return

	close()


func _on_exit_warning_confirmed() -> void:
	_reset_crafting_state()
	close()


func _has_unfinished_crafting() -> bool:
	return slot_entries[SLOT_1] != null or slot_entries[SLOT_2] != null or not current_result_card_id.is_empty()


func _reset_crafting_state() -> void:
	_clear_selection_deck()
	_clear_all_previews()
	slot_entries[SLOT_1] = null
	slot_entries[SLOT_2] = null
	current_result_card_id = ""
	pending_selected_entry.clear()
	active_slot_index = 0
	craft_mode = CraftMode.BOARD
	can_close_selection_without_choice = false
	hide_tooltip()
	_refresh_slot_placeholders()
	_update_result_description()
	_update_connection_lines()
	_refresh_reward_action_buttons()


## 统一维护“退出/确认”按钮状态。
## - 无操作：退出可点，确认禁用。
## - 已选择合成素材但还没形成结果：退出禁用，确认禁用；玩家可再次点击槽位里的已选卡牌取消。
## - 已形成结果：退出禁用，确认启用；确认后写入牌组并退出收获页。
func _refresh_reward_action_buttons() -> void:
	if not is_instance_valid(btn_back) or not is_instance_valid(btn_confirm):
		return
	if craft_mode == CraftMode.SELECTING:
		return

	btn_confirm.show()
	btn_confirm.text = "确认"

	if not current_result_card_id.is_empty():
		btn_back.disabled = true
		btn_confirm.disabled = false
	elif _has_unfinished_crafting():
		btn_back.disabled = true
		btn_confirm.disabled = true
	else:
		btn_back.disabled = false
		btn_confirm.disabled = true


func _clear_selection_deck() -> void:
	for card in current_deck_cards:
		if is_instance_valid(card):
			card.queue_free()

	current_deck_cards.clear()
	current_deck_entries.clear()

	for child in deck_grid.get_children():
		child.queue_free()


func _clear_all_previews() -> void:
	_clear_slot_preview(SLOT_1)
	_clear_slot_preview(SLOT_2)
	_clear_result_preview()


func _clear_slot_preview(slot_index: int) -> void:
	var preview = slot_preview_cards[slot_index]
	if is_instance_valid(preview):
		preview.queue_free()
	slot_preview_cards[slot_index] = null


func _clear_result_preview() -> void:
	if is_instance_valid(result_preview_card):
		result_preview_card.queue_free()
	result_preview_card = null
	current_result_card_id = ""
	_update_result_description()


func _refresh_slot_placeholders() -> void:
	slot1_placeholder.visible = slot_preview_cards[SLOT_1] == null
	slot2_placeholder.visible = slot_preview_cards[SLOT_2] == null

	if result_preview_card != null:
		result_placeholder.visible = false
	elif slot_entries[SLOT_1] != null and slot_entries[SLOT_2] != null:
		result_placeholder.visible = true
		result_placeholder.text = no_recipe_text
	else:
		result_placeholder.visible = true
		result_placeholder.text = "结果槽"


func _update_result_description() -> void:
	if not is_instance_valid(result_description_panel) or not is_instance_valid(result_description_label):
		return

	if is_instance_valid(result_preview_card):
		var description_text = ""
		if result_preview_card.has_method("get_parsed_description"):
			description_text = result_preview_card.get_parsed_description()
		elif _object_has_property(result_preview_card, &"raw_description"):
			description_text = str(result_preview_card.get("raw_description"))

		result_description_label.clear()
		result_description_label.append_text(description_text if description_text != "" else "无效果文本")
		result_description_panel.size = Vector2.ZERO
		result_description_panel.show()
		call_deferred("_position_result_description_panel")
	else:
		result_description_panel.hide()
		result_description_label.clear()


func _setup_result_description_panel() -> void:
	_get_result_description_panel_presenter().setup_panel(result_description_panel, result_description_label)


func _position_result_description_panel() -> void:
	await _get_result_description_position_presenter().position_panel(
		result_description_panel,
		result_description_label,
		result_preview_card,
		result_slot,
		get_viewport().get_visible_rect().size,
		result_tooltip_offset_x,
		result_tooltip_offset_y,
		result_tooltip_max_width
	)


func _update_connection_lines() -> void:
	_get_connection_line_presenter().update_connection_lines(
		connection_lines,
		slot_preview_cards[SLOT_1],
		slot_preview_cards[SLOT_2],
		result_preview_card,
		slot1,
		slot2,
		result_slot
	)


func _get_slot_anchor(slot_index: int) -> Control:
	return slot1_anchor if slot_index == SLOT_1 else slot2_anchor


func _apply_preview_padding() -> void:
	_get_slot_preview_layout_presenter().apply_preview_padding(
		slot1_anchor,
		slot2_anchor,
		result_anchor,
		slot_preview_padding
	)


func _apply_padding_to_anchor(anchor: Control) -> void:
	_get_slot_preview_layout_presenter().apply_padding_to_anchor(anchor, slot_preview_padding)


func _get_anchor_preview_size(anchor: Control) -> Vector2:
	return _get_slot_preview_layout_presenter().get_anchor_preview_size(anchor, card_display_size)


func _get_current_deck_card_ids() -> Array[String]:
	if deck_manager and deck_manager.has_method("get_deck_card_ids"):
		return deck_manager.get_deck_card_ids()
	return GlobalDB.player_deck.duplicate()


func _get_recipe_result(card_a_id: String, card_b_id: String) -> String:
	return _get_recipe_resolver().get_recipe_result(card_a_id, card_b_id, CRAFTING_RECIPES)


func _has_clickable_entry(entries: Array) -> bool:
	for entry in entries:
		if not entry["disabled"]:
			return true
	return false


func _copy_entry(entry: Dictionary) -> Dictionary:
	return {
		"deck_index": entry["deck_index"],
		"card_id": entry["card_id"],
	}


func _entry_key(entry: Dictionary) -> int:
	return entry["deck_index"]


func _steal_card_data(card_id: String, draft_card: Control, temp_pile: Node) -> void:
	if not deck_manager or not deck_manager.card_factory:
		return

	if not is_instance_valid(temp_pile):
		return

	var real_card = await _get_real_card_spawner().spawn_into_temp_pile(card_id, deck_manager.card_factory, temp_pile, self)
	if not is_instance_valid(temp_pile):
		return
	if not real_card:
		return

	draft_card.raw_description = await _extract_card_description(real_card)

	if _object_has_property(real_card, &"active_keywords") and real_card.get("active_keywords") is Array:
		draft_card.active_keywords = real_card.get("active_keywords").duplicate()

	var front_texture = await _extract_front_texture(real_card, card_id)
	if front_texture != null:
		draft_card.texture = front_texture

	temp_pile.remove_card(real_card)
	real_card.queue_free()


func _create_temp_pile() -> Pile:
	return _get_temp_pile_factory().create_temp_pile(deck_manager, "CraftReward")


func _extract_front_texture(real_card: Node, card_id: String) -> Texture2D:
	return await _get_card_texture_extractor().extract_front_texture(real_card, card_id, deck_manager, self)


func _extract_card_description(real_card: Node) -> String:
	return await _get_card_description_extractor().extract_description(real_card, self)


func _try_find_card_manager() -> void:
	var tree_root = get_tree().root
	if tree_root and tree_root.has_meta("card_manager"):
		var card_manager = tree_root.get_meta("card_manager")
		if card_manager is CardManager:
			deck_manager = card_manager
			return

	var scene_root = get_tree().current_scene
	if scene_root and scene_root.has_meta("card_manager"):
		var scene_card_manager = scene_root.get_meta("card_manager")
		if scene_card_manager is CardManager:
			deck_manager = scene_card_manager
			return

	var parent = get_parent()
	while parent:
		if parent is CardManager:
			deck_manager = parent
			return
		parent = parent.get_parent()

	var card_manager_node = get_tree().root.find_child("CardManager", true, false)
	if card_manager_node and card_manager_node is CardManager:
		deck_manager = card_manager_node


## 初始化共享 Tooltip presenter。
## 合成界面的 Hover 卡牌目前只显示主效果框，不显示关键词列。
func _setup_tooltip_presenter() -> void:
	tooltip_presenter = _get_tooltip_adapter().ensure_presenter(tooltip_presenter, self, tooltip_config)


func show_tooltip(card: Control) -> void:
	tooltip_presenter = _get_tooltip_adapter().show_card_tooltip(tooltip_presenter, self, tooltip_config, card)


func hide_tooltip(_card: Control = null) -> void:
	_get_tooltip_adapter().hide_tooltip(tooltip_presenter)
