# 功能: 保存局外地图与跨场景进度快照，作为 OutScene、InScene 与存档系统之间的轻量状态中心。
# 核心逻辑: reset() 清空本轮 run；active_room_context 记录当前进入房间；pending_room_resolution 暂存局内结算返回；progress_snapshot 兜底保存时代、阶段、时间币和牌组。
extends Node

# 需要保存的核心数据
var is_initialized: bool = false # 标记是否已经生成过地图
var map_seed: String = ""
var tile_data: Dictionary = {}
var tile_features: Dictionary = {}
var player_hex: Vector2i = Vector2i.ZERO
var current_tier: int = 0
var has_cut: bool = false
var chosen_char_index: int = -1
var ui_settled: bool = false
var path_gone = []
var loaded : bool = false

## 当前正在进入的房间上下文。
## 用途：
## - 从局外进入局内时，记录“玩家是从哪个格子进入了哪种房间”。
## - 等局内战斗结束回到局外时，可以继续基于这份上下文做节点结算、房间清理、奖励回填。
## - 现在先只保存数据，不强行在这里执行具体房间逻辑。
var active_room_context: Dictionary = {}

## 等待局外场景消费的“房间结算结果”。
## 用途：
## - 局内结算结束后，把返回结果先暂存到 MapState。
## - OutScene 重建完成后再统一消费，避免切场时信息丢失。
var pending_room_resolution: Dictionary = {}

## 全局进度快照。
## 这是“跨场景双保险”层：
## - 正常情况下，GlobalTimecoin / GlobalClock / GlobalDB 这些 autoload 会自然保留。
## - 如果某个场景在切换过程中没有正确拿到 autoload，或者未来改了切场方式，
##   我们仍然可以从 MapState 把关键进度同步回来。
var progress_snapshot: Dictionary = {
	"timecoins": 0,
	"era": 1,
	"phase": 1,
	"deck_snapshot": [],
}

# 清除数据（用于新游戏或重置）
func reset():
	is_initialized = false
	tile_data.clear()
	tile_features.clear()
	player_hex = Vector2i.ZERO
	current_tier = 0
	has_cut = false
	chosen_char_index = -1
	ui_settled = false
	loaded = false
	active_room_context.clear()
	pending_room_resolution.clear()
	path_gone.clear()
	progress_snapshot = {
		"timecoins": 0,
		"era": 1,
		"phase": 1,
		"deck_snapshot": [],
	}


## 记录“当前正在进入的房间”上下文。
## 这里使用深拷贝，避免外部后续修改原字典时把全局状态意外污染。
func set_active_room_context(context: Dictionary) -> void:
	active_room_context = context.duplicate(true)


## 读取当前房间上下文。
## 调用方拿到的是副本，可以安全改动。
func get_active_room_context() -> Dictionary:
	return active_room_context.duplicate(true)


## 清空当前房间上下文。
## 通常在局外成功消费完房间结算结果后调用。
func clear_active_room_context() -> void:
	active_room_context.clear()


## 存入一份等待局外消费的房间结算结果。
func set_pending_room_resolution(resolution: Dictionary) -> void:
	pending_room_resolution = resolution.duplicate(true)


## 只读查看当前是否有未消费的房间结算结果。
func peek_pending_room_resolution() -> Dictionary:
	return pending_room_resolution.duplicate(true)


## 消费一份房间结算结果。
## 这是“取一次就清空”的接口，适合 OutScene 在 _ready 后统一处理。
func consume_pending_room_resolution() -> Dictionary:
	var resolution_copy := pending_room_resolution.duplicate(true)
	pending_room_resolution.clear()
	return resolution_copy


## 手动清空等待中的房间结算结果。
## 某些异常恢复场景下可能需要直接丢弃旧数据。
func clear_pending_room_resolution() -> void:
	pending_room_resolution.clear()


## 存储全局进度快照。
## 为了避免只更新一个字段时把其它字段抹掉，这里采用 merge 方式写入。
func set_progress_snapshot(snapshot: Dictionary) -> void:
	var merged := progress_snapshot.duplicate(true)
	for key in snapshot.keys():
		merged[key] = snapshot[key]
	progress_snapshot = merged


## 读取当前全局进度快照副本。
func get_progress_snapshot() -> Dictionary:
	return progress_snapshot.duplicate(true)


## 单独写入时间币快照。
func set_saved_timecoins(amount: int) -> void:
	progress_snapshot["timecoins"] = max(amount, 0)


## 读取时间币快照。
func get_saved_timecoins() -> int:
	return max(int(progress_snapshot.get("timecoins", 0)), 0)


## 单独写入时代/阶段快照。
func set_saved_era_progress(era_value: int, phase_value: int = 1) -> void:
	progress_snapshot["era"] = max(era_value, 1)
	progress_snapshot["phase"] = max(phase_value, 1)


## 读取时代快照。
func get_saved_era() -> int:
	return max(int(progress_snapshot.get("era", 1)), 1)


## 读取阶段快照。
func get_saved_phase() -> int:
	return max(int(progress_snapshot.get("phase", 1)), 1)


## 写入牌组快照。
func set_saved_deck(deck_snapshot: Array) -> void:
	progress_snapshot["deck_snapshot"] = deck_snapshot.duplicate(true)


## 读取牌组快照。
func get_saved_deck() -> Array:
	var snapshot = progress_snapshot.get("deck_snapshot", [])
	return snapshot.duplicate(true) if snapshot is Array else []

func Call_Saver():
	Saver.Buffer_call.emit("era", GlobalClock.era)
	Saver.Buffer_call.emit("character", chosen_char_index)
	Saver.Buffer_call.emit("rng", map_seed)
	Saver.Buffer_call.emit("player_location", player_hex)
	Saver.Buffer_call.emit("coin", GlobalTimecoin.current_timecoins)
	Saver.Buffer_call.emit("player_deck", GlobalDB.player_deck)
	Saver.Buffer_call.emit("path_gone", path_gone)
	Saver.Buffer_call.emit("tier", current_tier)
	Saver.Save_game(0)
