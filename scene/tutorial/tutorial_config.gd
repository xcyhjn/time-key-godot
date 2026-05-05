# 功能: 教程流程配置资源，集中声明固定局外地图、教程战斗入口、固定卡组和 Dialogic 时间线名称。
# 核心逻辑: Director 读取该资源后，把数据注入继承自正式场景的教程局外/局内，避免把教程数据硬编码进主流程。
extends Resource
class_name TutorialConfig

const MapGeneratorScript = preload("res://scene/out_scene/map_generator.gd")

@export_group("场景路径")
@export_file("*.tscn") var tutorial_out_scene_path: String = "res://scene/tutorial/tutorial_out_scene.tscn"
@export_file("*.tscn") var tutorial_in_scene_path: String = "res://scene/tutorial/tutorial_in_scene.tscn"

@export_group("教程局外地图")
## 玩家初始所在格。
@export var start_hex: Vector2i = Vector2i.ZERO
## 教程推荐选择的角色格。默认使用右侧第一格，对应 MapGenerator.TileType.CHAR_5。
@export var required_character_hex: Vector2i = Vector2i(1, 0)
## 教程第一场战斗房间格。它必须与角色格相邻，保证沿用原本局外移动逻辑。
@export var first_room_hex: Vector2i = Vector2i(2, 0)
## 进入教程战斗时传给局内的 payload。保留 seed 是为了复用现有 apply_external_event 解析。
@export var first_room_payload: String = "tutorial_battle tutorial_seed"
## 教程固定局外地图。留空时使用 get_default_out_map() 的最小路径地图。
@export var fixed_out_map: Dictionary = {}
## 教程固定房间特性。留空时第一战斗房间不带局外特性。
@export var fixed_out_features: Dictionary = {}

@export_group("教程局内")
## 教程固定卡组，进入 tutorial_in_scene 时会覆盖 GlobalDB.player_deck。
@export var fixed_player_deck: Array[String] = ["1", "1", "1", "2", "2", "wind", "3"]
## 初始是否隐藏时间轴。后续由 TutorialInDirector 根据教学阶段解锁。
@export var hide_timeline_on_start: bool = true
## 初始是否锁住玩家输入。后续由 TutorialInDirector 解锁指定步骤。
@export var lock_player_input_on_start: bool = true

@export_group("Dialogic")
## 局外选角教学使用的 Dialogic timeline 名称；留空时只启用蒙版/锁定，不主动开启对话。
@export var out_scene_timeline: String = ""
## 局外角色确认后播放的 Dialogic timeline 名称；留空时角色确认后不追加对话。
@export var out_scene_after_character_timeline: String = ""
## 局内战斗教学使用的 Dialogic timeline 名称；留空时只启用蒙版/锁定，不主动开启对话。
@export var in_scene_timeline: String = ""


func get_default_out_map() -> Dictionary:
	if not fixed_out_map.is_empty():
		return fixed_out_map.duplicate(true)

	return {
		Vector2i(0, 0): MapGeneratorScript.TileType.START,
		Vector2i(1, 0): MapGeneratorScript.TileType.CHAR_5,
		Vector2i(2, 0): MapGeneratorScript.TileType.NORMAL,
		Vector2i(3, 0): MapGeneratorScript.TileType.BOSS,
	}


func get_default_out_features() -> Dictionary:
	return fixed_out_features.duplicate(true)
