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

# 清除数据（用于新游戏或重置）
func reset():
	is_initialized = false
	tile_data.clear()
	tile_features.clear()
	player_hex = Vector2i.ZERO
	current_tier = 0
	has_cut = false
	chosen_char_index = -1
