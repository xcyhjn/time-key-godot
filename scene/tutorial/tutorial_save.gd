# 功能: 教程入口本地记录工具，负责持久化“教程弹窗是否已经触发过”等轻量状态。
# 核心逻辑: 使用 user://tutorial_settings.cfg 保存 prompt_seen，主菜单只通过静态函数读写，避免引入额外自动加载节点。
extends RefCounted
class_name TutorialSave

const SETTINGS_PATH: String = "user://tutorial_settings.cfg"
const SETTINGS_FILE_NAME: String = "tutorial_settings.cfg"
const SECTION: String = "tutorial"
const KEY_PROMPT_SEEN: String = "prompt_seen"
const KEY_TUTORIAL_CHOSEN: String = "tutorial_chosen"


static func should_show_prompt() -> bool:
	return not has_seen_prompt()


static func has_seen_prompt() -> bool:
	var config: ConfigFile = _load_config()
	return bool(config.get_value(SECTION, KEY_PROMPT_SEEN, false))


static func mark_prompt_seen(tutorial_chosen: bool) -> void:
	var config: ConfigFile = _load_config()
	config.set_value(SECTION, KEY_PROMPT_SEEN, true)
	config.set_value(SECTION, KEY_TUTORIAL_CHOSEN, tutorial_chosen)
	var err: int = config.save(SETTINGS_PATH)
	if err != OK:
		push_warning("TutorialSave: 无法写入教程本地记录，错误码: %s" % err)


static func was_tutorial_chosen() -> bool:
	var config: ConfigFile = _load_config()
	return bool(config.get_value(SECTION, KEY_TUTORIAL_CHOSEN, false))


static func reset_prompt_record() -> void:
	var config: ConfigFile = ConfigFile.new()
	var err: int = config.save(SETTINGS_PATH)
	if err != OK:
		push_warning("TutorialSave: 无法重置教程本地记录，错误码: %s" % err)


## 调试入口：如果教程记录文件存在就删除，便于反复验证首次教程弹窗与衔接流程。
static func delete_settings_file_if_exists() -> bool:
	if not FileAccess.file_exists(SETTINGS_PATH):
		return false

	var user_dir: DirAccess = DirAccess.open("user://")
	if user_dir == null:
		push_warning("TutorialSave: 无法打开 user:// 目录，教程记录删除失败。")
		return false

	var err: int = user_dir.remove(SETTINGS_FILE_NAME)
	if err != OK:
		push_warning("TutorialSave: 无法删除教程本地记录，错误码: %s" % err)
		return false
	return true


static func _load_config() -> ConfigFile:
	var config: ConfigFile = ConfigFile.new()
	var err: int = config.load(SETTINGS_PATH)
	if err != OK and err != ERR_FILE_NOT_FOUND:
		push_warning("TutorialSave: 读取教程本地记录失败，错误码: %s" % err)
	return config
