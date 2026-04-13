class_name GameLogger
extends Node

enum LogLevel {
	DEBUG,
	INFO,
	WARNING,
	ERROR
}

# 日志文件管理配置
@export_group("日志基础设置")
@export var current_log_level: LogLevel = LogLevel.DEBUG
@export var enabled: bool = true
@export var log_to_file: bool = false
@export var log_file_path: String = "user://game.log"
@export var log_to_project: bool = true  #在项目的文件夹中写入日志
@export var project_log_path: String = "res://log/game.log"
@export var show_timestamp: bool = true
@export var show_level: bool = true
@export var show_source: bool = false

# 日志文件管理配置
@export_group("日志文件管理")
@export var max_file_size_mb: float = 10.0  # 单个日志文件最大大小（MB）
@export var max_backup_files: int = 3  # 最大备份文件数量
@export var auto_cleanup: bool = true  # 是否自动清理

# 会话分隔符配置
@export var add_session_separator: bool = true  # 每次启动添加会话分隔符

static var _instance: GameLogger

var session_started: bool = false


static func get_instance() -> GameLogger:
	if not _instance:
		_instance = GameLogger.new()
		_instance._initialize_logger()
	return _instance


static func debug(message: String, source: String = "") -> void:
	get_instance()._log(LogLevel.DEBUG, message, source)


static func info(message: String, source: String = "") -> void:
	get_instance()._log(LogLevel.INFO, message, source)


static func warning(message: String, source: String = "") -> void:
	get_instance()._log(LogLevel.WARNING, message, source)


static func error(message: String, source: String = "") -> void:
	get_instance()._log(LogLevel.ERROR, message, source)


func _log(level: LogLevel, message: String, source: String) -> void:
	if not enabled or level < current_log_level:
		return

	var formatted_message = _format_message(level, message, source)

	_output_to_console(level, formatted_message)

	# 直接写入日志文件（如果启用）
	if log_to_file or log_to_project:
		_output_to_file_sync(formatted_message)


func _format_message(level: LogLevel, message: String, source: String) -> String:
	var parts: PackedStringArray = []

	if show_timestamp:
		parts.append(_get_timestamp())

	if show_level:
		parts.append(_get_level_string(level))

	if show_source and not source.is_empty():
		parts.append("[%s]" % source)

	if parts.is_empty():
		return message
	else:
		# 性能优化点：弃用繁琐的for循环，使用更优雅快速的引擎原生 join 函数拼接
		return "%s %s" % [" ".join(parts), message]


func _get_timestamp() -> String:
	var time = Time.get_datetime_dict_from_system()
	return "[%04d-%02d-%02d %02d:%02d:%02d]" % [
		time.year, time.month, time.day,
		time.hour, time.minute, time.second
	]


func _get_level_string(level: LogLevel) -> String:
	match level:
		LogLevel.DEBUG: return "[DEBUG]"
		LogLevel.INFO: return "[INFO]"
		LogLevel.WARNING: return "[WARNING]"
		LogLevel.ERROR: return "[ERROR]"
	return "[UNKNOWN]"


func _output_to_console(level: LogLevel, message: String) -> void:
	match level:
		LogLevel.DEBUG: print_debug(message)
		LogLevel.INFO: print(message)
		LogLevel.WARNING: push_warning(message)
		LogLevel.ERROR: push_error(message)


# 日志系统初始化
func _initialize_logger() -> void:
	if add_session_separator and not session_started:
		_add_session_separator()
		session_started = true


# ==========================================
# 核心修复与优化：同步文件写入
# ==========================================

func _output_to_file_sync(message: String) -> void:
	if log_to_file:
		_write_log_to_path(log_file_path, message)

	if log_to_project:
		# 保护机制：导出后的游戏 res:// 变为了只读资源，在这里加入判断防止崩溃
		if OS.has_feature("editor") or not project_log_path.begins_with("res://"):
			_write_log_to_path(project_log_path, message)


func _write_log_to_path(file_path: String, message: String) -> void:
	# 1. 确保父目录存在，防止配置了路径但未建文件夹导致的静默失败
	var dir = file_path.get_base_dir()
	if not DirAccess.dir_exists_absolute(dir):
		DirAccess.make_dir_recursive_absolute(dir)

	var file: FileAccess
	
	# 2. 核心修复点：动态判断打开模式，绝不覆盖老内容
	if FileAccess.file_exists(file_path):
		# READ_WRITE 模式专用于追加数据，且永远不会清空原文件
		file = FileAccess.open(file_path, FileAccess.READ_WRITE)
		if file:
			file.seek_end() # 将光标移动到当前内容的最末尾
	else:
		# 不存在则用 WRITE 创建新文件
		file = FileAccess.open(file_path, FileAccess.WRITE)

	# 3. 极简直接落盘
	if file:
		file.store_line(message)
		
		# 顺便获取大小（合并了原来需要单独开关文件检查大小的繁琐开销）
		var current_size = file.get_length()
		
		# 调用 close 会瞬间把数据强行塞进硬盘，绝无缓冲，防死机丢失
		file.close() 

		# 4. 判断大小直接触发轮换
		if auto_cleanup and current_size > (max_file_size_mb * 1024 * 1024):
			_rotate_log_files(file_path)
	else:
		push_error("GameLogger: 无法打开或创建日志文件 -> ", file_path)


static func set_log_level(level: LogLevel) -> void: get_instance().current_log_level = level
static func set_enabled(value: bool) -> void: get_instance().enabled = value
static func set_log_to_file(value: bool) -> void: get_instance().log_to_file = value
static func set_log_to_project(value: bool) -> void: get_instance().log_to_project = value

# 便捷的日志级别设置方法
static func enable_debug_logging() -> void:
	set_log_level(LogLevel.DEBUG)
	print("GameLogger: 已启用DEBUG级别日志")

static func enable_info_logging() -> void:
	set_log_level(LogLevel.INFO)
	print("GameLogger: 已启用INFO级别日志")

static func enable_warning_logging() -> void:
	set_log_level(LogLevel.WARNING)
	print("GameLogger: 已启用WARNING级别日志")

static func enable_error_logging() -> void:
	set_log_level(LogLevel.ERROR)
	print("GameLogger: 已启用ERROR级别日志")


# ==========================================
# 日志文件管理功能
# ==========================================

func _rotate_log_files(file_path: String) -> void:
	# 删除最旧的备份文件
	var backup_pattern = file_path + ".%d"
	var oldest_backup = backup_pattern % max_backup_files
	if FileAccess.file_exists(oldest_backup):
		DirAccess.remove_absolute(oldest_backup)

	# 重命名现有备份文件
	for i in range(max_backup_files - 1, 0, -1):
		var current_backup = backup_pattern % i
		var next_backup = backup_pattern % (i + 1)
		if FileAccess.file_exists(current_backup):
			DirAccess.rename_absolute(current_backup, next_backup)

	# 重命名当前日志文件为备份1
	var first_backup = backup_pattern % 1
	if FileAccess.file_exists(file_path):
		DirAccess.rename_absolute(file_path, first_backup)


# 手动清理日志文件
static func cleanup_log_files() -> void:
	var instance = get_instance()
	if instance.log_to_file: instance._force_cleanup_file(instance.log_file_path)
	if instance.log_to_project: instance._force_cleanup_file(instance.project_log_path)


func _force_cleanup_file(file_path: String) -> void:
	if not auto_cleanup or not FileAccess.file_exists(file_path):
		return
	var file = FileAccess.open(file_path, FileAccess.READ)
	if file:
		var size = file.get_length()
		file.close()
		if size > (max_file_size_mb * 1024 * 1024):
			_rotate_log_files(file_path)


# 添加会话分隔符
func _add_session_separator() -> void:
	var separator = "=".repeat(80)
	var timestamp = _get_timestamp()
	var session_start_message = "%s\n会话开始: %s\n%s" % [separator, timestamp, separator]
	
	if log_to_file: _write_log_to_path(log_file_path, session_start_message)
	if log_to_project: _write_log_to_path(project_log_path, session_start_message)


# 手动删除所有日志文件
static func delete_all_log_files() -> void:
	var instance = get_instance()
	var paths = [instance.log_file_path, instance.project_log_path]
	
	for path in paths:
		if FileAccess.file_exists(path):
			DirAccess.remove_absolute(path)
		for i in range(1, instance.max_backup_files + 1):
			var backup_file = path + ".%d" % i
			if FileAccess.file_exists(backup_file):
				DirAccess.remove_absolute(backup_file)


# 获取日志文件信息
static func get_log_file_info() -> Dictionary:
	var instance = get_instance()
	var info = { }
	info["user_log"] = instance._get_single_file_info(instance.log_file_path, instance.max_backup_files)
	info["project_log"] = instance._get_single_file_info(instance.project_log_path, instance.max_backup_files)
	return info


func _get_single_file_info(file_path: String, max_backups: int) -> Dictionary:
	var info = { "main_file_size": 0, "main_file_exists": false, "backups": [] }

	# 避免了文件不存在时的报错
	if FileAccess.file_exists(file_path):
		var file = FileAccess.open(file_path, FileAccess.READ)
		if file:
			info["main_file_size"] = file.get_length()
			info["main_file_exists"] = true
			file.close()

	for i in range(1, max_backups + 1):
		var backup_path = file_path + ".%d" % i
		var backup_info = { "size": 0, "exists": false }
		
		if FileAccess.file_exists(backup_path):
			var backup_file = FileAccess.open(backup_path, FileAccess.READ)
			if backup_file:
				backup_info["size"] = backup_file.get_length()
				backup_info["exists"] = true
				backup_file.close()
				
		info["backups"].append(backup_info)

	return info
