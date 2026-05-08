# 功能: 极简场景流转日志工具，只记录资源加载、场景切换和关键 payload，便于导出版排查黑屏/切场失败。
# 核心逻辑: append() 同时输出到控制台与 user://logs/scene_flow.log；scene_event()/error_event() 用统一格式记录场景流转节点。
extends Node

const LOG_DIR: String = "user://logs"
const LOG_PATH: String = "user://logs/scene_flow.log"


func _ready() -> void:
	_ensure_log_dir()
	append("BOOT", "SceneLog ready")


func scene_event(source: String, message: String, extra: Dictionary = {}) -> void:
	append(source, _format_message(message, extra))


func error_event(source: String, message: String, extra: Dictionary = {}) -> void:
	var line: String = _format_message("ERROR: %s" % message, extra)
	push_error("[%s] %s" % [source, line])
	_append_to_file("[%s][%s] %s" % [_timestamp(), source, line])


func append(source: String, message: String) -> void:
	var line: String = "[%s][%s] %s" % [_timestamp(), source, message]
	print(line)
	_append_to_file(line)


func clear_log() -> void:
	_ensure_log_dir()
	var file: FileAccess = FileAccess.open(LOG_PATH, FileAccess.WRITE)
	if file != null:
		file.store_line("[%s][SceneLog] log cleared" % _timestamp())


func _format_message(message: String, extra: Dictionary) -> String:
	if extra.is_empty():
		return message

	var parts: Array[String] = []
	for key in extra.keys():
		parts.append("%s=%s" % [str(key), str(extra[key])])
	return "%s | %s" % [message, ", ".join(parts)]


func _timestamp() -> String:
	var milliseconds: int = Time.get_ticks_msec() % 1000
	return Time.get_datetime_string_from_system(true) + (".%03d" % milliseconds)


func _ensure_log_dir() -> void:
	DirAccess.make_dir_recursive_absolute(LOG_DIR)


func _append_to_file(line: String) -> void:
	_ensure_log_dir()
	var file: FileAccess = FileAccess.open(LOG_PATH, FileAccess.READ_WRITE)
	if file == null:
		return
	file.seek_end()
	file.store_line(line)
