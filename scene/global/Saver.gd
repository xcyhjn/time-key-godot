extends Node

signal Buffer_call(type, value)

func _init() -> void:
	Buffer_call.connect(Buffer_Change)

var Save_Buffer : Dictionary = \
{
	"era" : 0,
	"player_location_x" : 0,
	"player_location_y" : 0,
	"path_gone" : [],
	"player_deck" : [],
	"rng" : "",
	"character" : 0,
	"coin" : 0
}

func Buffer_Change(type, value):
	if type == "player_location":
		value.x = Save_Buffer["player_location_x"]

const SAVE_PATH_PREFIX: String = "user://save_"
const SAVE_EXTENSION: String = ".cfg"

const SEC_META: String = "meta"
const SEC_PLAYER: String = "player"
const SEC_MAP: String = "map"
const SEC_DECK: String = "deck"

func Save_game(slot_id: int = 0) -> bool:
	print("\n\n\n\n正在存档………………、\n\n\n\n")
	var config := ConfigFile.new()

	config.set_value(SEC_META, "timestamp", Time.get_unix_time_from_system())
	config.set_value(SEC_META, "era", GlobalClock.era if GlobalClock else 1)

	config.set_value(SEC_PLAYER, "player_hex", MapState.player_hex if MapState else Vector2i.ZERO)
	config.set_value(SEC_PLAYER, "chosen_char_index", MapState.chosen_char_index if MapState else -1)
	config.set_value(SEC_PLAYER, "has_cut", MapState.has_cut if MapState else false)

	config.set_value(SEC_MAP, "map_seed", MapState.map_seed if MapState else "")
	config.set_value(SEC_MAP, "current_tier", MapState.current_tier if MapState else 0)
	config.set_value(SEC_MAP, "is_initialized", MapState.is_initialized if MapState else false)
	config.set_value(SEC_MAP, "tile_data", JSON.stringify(MapState.tile_data if MapState else {}))
	config.set_value(SEC_MAP, "tile_features", JSON.stringify(MapState.tile_features if MapState else {}))
	config.set_value(SEC_MAP, "path_gone", JSON.stringify(MapState.path_gone if MapState else []))

	config.set_value(SEC_DECK, "player_deck", JSON.stringify(GlobalDB.player_deck if GlobalDB else []))

	var path := SAVE_PATH_PREFIX + str(slot_id) + SAVE_EXTENSION
	var err := config.save(path)
	if err != OK:
		push_error("[Saver] 存档失败 slot=%d err=%d" % [slot_id, err])
		return false
	print("[Saver] 存档成功 slot=%d" % slot_id)
	return true

func Load_game(slot_id: int = 0) -> bool:
	var path := SAVE_PATH_PREFIX + str(slot_id) + SAVE_EXTENSION

	if not FileAccess.file_exists(path):
		push_warning("[Saver] 存档不存在 slot=%d" % slot_id)
		return false

	var config := ConfigFile.new()
	var err := config.load(path)
	if err != OK:
		push_error("[Saver] 读档失败 slot=%d err=%d" % [slot_id, err])
		return false

	if MapState:
		MapState.player_hex = config.get_value(SEC_PLAYER, "player_hex", Vector2i.ZERO)
		MapState.chosen_char_index = config.get_value(SEC_PLAYER, "chosen_char_index", -1)
		MapState.has_cut = config.get_value(SEC_PLAYER, "has_cut", false)

		MapState.map_seed = config.get_value(SEC_MAP, "map_seed", "")
		
		MapState.current_tier = config.get_value(SEC_MAP, "current_tier", 0)
		MapState.is_initialized = config.get_value(SEC_MAP, "is_initialized", true)

		MapState.tile_data = _restore_vector2i_key_dict(
			_safe_parse_json_dict(config.get_value(SEC_MAP, "tile_data", "{}"))
		)
		MapState.tile_features = _restore_vector2i_key_dict(
			_safe_parse_json_dict(config.get_value(SEC_MAP, "tile_features", "{}"))
		)
		MapState.path_gone = _safe_parse_json_array(config.get_value(SEC_MAP, "path_gone", "[]"))

	if GlobalDB:
		var deck_arr := _safe_parse_json_array(config.get_value(SEC_DECK, "player_deck", "[]"))
		GlobalDB.player_deck.clear()
		for item in deck_arr:
			GlobalDB.player_deck.append(str(item))

	var saved_era: int = config.get_value(SEC_META, "era", 1)
	if GlobalClock and GlobalClock.has_method("set_current_era"):
		GlobalClock.set_current_era(saved_era)
	elif GlobalClock:
		GlobalClock.era = saved_era

	print("[Saver] 读档成功 slot=%d" % slot_id)
	return true

func Has_save(slot_id: int = 0) -> bool:
	return FileAccess.file_exists(SAVE_PATH_PREFIX + str(slot_id) + SAVE_EXTENSION)

func _safe_parse_json_dict(raw: String) -> Dictionary:
	var result = JSON.parse_string(raw)
	return result if result is Dictionary else {}

func _safe_parse_json_array(raw: String) -> Array:
	var result = JSON.parse_string(raw)
	return result if result is Array else []

func _restore_vector2i_key_dict(raw: Dictionary) -> Dictionary:
	var restored := {}
	for key in raw.keys():
		var vec_key := _str_to_vector2i(key)
		restored[vec_key] = raw[key]
	return restored

func _str_to_vector2i(s: String) -> Vector2i:
	var stripped := s.strip_edges()
	if stripped.begins_with("(") and stripped.ends_with(")"):
		stripped = stripped.substr(1, stripped.length() - 2)
	var parts := stripped.split(",", false)
	if parts.size() >= 2:
		return Vector2i(int(parts[0].strip_edges()), int(parts[1].strip_edges()))
	return Vector2i.ZERO

func Delete_save(slot_id: int = 0) -> bool:
	var path := SAVE_PATH_PREFIX + str(slot_id) + SAVE_EXTENSION

	if not FileAccess.file_exists(path):
		print("[Saver] 无需删除，存档不存在 slot=%d" % slot_id)
		return true

	var err := DirAccess.remove_absolute(path)

	if err != OK:
		push_error("[Saver] 删除存档失败 slot=%d err=%d" % [slot_id, err])
		return false

	print("[Saver] 删除存档成功 slot=%d" % slot_id)
	return true
