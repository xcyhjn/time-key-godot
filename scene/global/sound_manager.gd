extends Node

# ==========================================
# 音频总线枚举
# ==========================================
enum Bus {
	MASTER,
	MUSIC,
	SFX,
}

const MUSIC_BUS := &"Music"
const SFX_BUS := &"SFX"
const FADE_DB_MIN := -40.0

## BGM 默认音量 (dB)
const BGM_DEFAULT_DB := -3.0
## SFX 默认音量 (dB)
const SFX_DEFAULT_DB := -3.0

# ==========================================
# 音频文件路径（准备好音频后放入对应路径即可生效）
# ==========================================
const BGM_PATHS := {
	main_menu = "res://audio/bgm/main_menu.ogg",
	in_game = "res://audio/bgm/battle.ogg",
	battle_boss = "res://audio/bgm/battle_boss.ogg",
}

const SFX_PATHS := {
	clock_tick = "res://audio/sfx/clock_tick.ogg",
	choose_layer = "res://audio/sfx/choose_role.ogg",
	walk_dim = "res://audio/sfx/walk_dim.ogg",
	draw_card = "res://audio/sfx/draw_card.ogg",
	shuffle = "res://audio/sfx/card_shuffle.ogg",
	confirm_timeline = "res://audio/sfx/confirm_timeline.ogg",
	tile_damage = "res://audio/sfx/tile_damage.ogg",
	combat_win = "res://audio/sfx/victory_short.ogg",
	game_win = "res://audio/sfx/victory_long.ogg",
	game_over = "res://audio/sfx/game_over.ogg",
}

# ==========================================
# BGM 播放器池（双播放器，交叉淡入淡出 + 循环）
# ==========================================
var music_audio_player_count: int = 2
var current_music_player_index: int = 0
var music_players: Array[AudioStreamPlayer]
var music_fade_duration: float = 1.0

# ==========================================
# SFX 播放器池（一次性音效）
# ==========================================
var sfx_audio_player_count: int = 6
var sfx_players: Array[AudioStreamPlayer]

# ==========================================
# 循环 SFX 播放器（victory_short / game_over 等需要循环 + 可淡出的音效）
# ==========================================
var _looping_sfx_player: AudioStreamPlayer
var _looping_sfx_active: bool = false

# ==========================================
# 音频资源缓存
# ==========================================
var _bgm_cache: Dictionary = {}
var _sfx_cache: Dictionary = {}

# ==========================================
# 初始化
# ==========================================
func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_init_music_players()
	_init_sfx_players()
	_init_looping_sfx_player()
	_connect_signals()
	print("[SoundManager] 音频系统初始化完成")

func _init_music_players() -> void:
	for _i in music_audio_player_count:
		var p := AudioStreamPlayer.new()
		p.process_mode = Node.PROCESS_MODE_ALWAYS
		p.bus = MUSIC_BUS
		p.volume_db = FADE_DB_MIN
		p.finished.connect(_on_bgm_finished.bind(p))
		add_child(p)
		music_players.append(p)

func _init_sfx_players() -> void:
	for _i in sfx_audio_player_count:
		var p := AudioStreamPlayer.new()
		p.bus = SFX_BUS
		p.volume_db = SFX_DEFAULT_DB
		add_child(p)
		sfx_players.append(p)

func _init_looping_sfx_player() -> void:
	_looping_sfx_player = AudioStreamPlayer.new()
	_looping_sfx_player.bus = SFX_BUS
	_looping_sfx_player.volume_db = FADE_DB_MIN
	_looping_sfx_player.finished.connect(_on_looping_sfx_finished)
	add_child(_looping_sfx_player)

# ==========================================
# 信号连接
# ==========================================
func _connect_signals() -> void:
	if Global.has_signal("choose"):
		_connect_signal_once(Global.choose, _on_choose_layer)
	if Global.has_signal("dim_in"):
		_connect_signal_once(Global.dim_in, _on_dim_in)

	# Signal_Bus 信号
	_connect_signal_once(Signal_Bus.card_drawn, _on_card_drawn)
	_connect_signal_once(Signal_Bus.deck_shuffled, _on_deck_shuffled)
	_connect_signal_once(Signal_Bus.timeline_action_added, _on_timeline_action_added)
	_connect_signal_once(Signal_Bus.combat_victory_triggered, _on_combat_victory)
	_connect_signal_once(Signal_Bus.defeat_triggered, _on_game_over)
	_connect_signal_once(Signal_Bus.tile_selected, _on_tile_selected)

# ==========================================
# BGM 控制
# ==========================================
func _connect_signal_once(source_signal: Signal, callback: Callable) -> void:
	if not source_signal.is_connected(callback):
		source_signal.connect(callback)


func play_bgm(key: String) -> void:
	var stream := _get_bgm(key)
	if stream == null:
		return
	_play_music_internal(stream)

func stop_bgm(fade_duration: float = -1.0) -> void:
	var current := music_players[current_music_player_index]
	if not current.playing:
		return
	if fade_duration < 0.0:
		fade_duration = music_fade_duration
	if fade_duration <= 0.0:
		current.stop()
		current.stream = null
		current.volume_db = FADE_DB_MIN
		current_music_player_index = 1 if current_music_player_index == 0 else 0
		return
	_fade_out_and_stop(current, fade_duration)
	current_music_player_index = 1 if current_music_player_index == 0 else 0

func stop_all() -> void:
	for p in music_players:
		if p.playing:
			p.stop()
		p.stream = null
		p.volume_db = FADE_DB_MIN
	stop_looping_sfx(0.0)
	for p in sfx_players:
		if p.playing:
			p.stop()
		p.stream = null

func play_bgm_main_menu() -> void:
	play_bgm("main_menu")

func play_bgm_in_game() -> void:
	play_bgm("in_game")

func _play_music_internal(stream: AudioStream) -> void:
	var current := music_players[current_music_player_index]
	if current.stream == stream and current.playing:
		return

	var next_idx := 0 if current_music_player_index == 1 else 1
	var next := music_players[next_idx]

	if next.playing:
		next.stop()

	next.stream = stream
	next.volume_db = FADE_DB_MIN
	next.play()
	_fade_in(next)

	_fade_out_and_stop(current)
	current_music_player_index = next_idx

func _on_bgm_finished(player: AudioStreamPlayer) -> void:
	# BGM 循环：播放完毕后重新播放
	if player.stream and player == music_players[current_music_player_index]:
		player.play()

func _fade_in(player: AudioStreamPlayer) -> void:
	var t := create_tween()
	t.tween_property(player, "volume_db", BGM_DEFAULT_DB, music_fade_duration)

func _fade_out_and_stop(player: AudioStreamPlayer, fade_duration: float = -1.0) -> void:
	if not player.playing:
		player.stream = null
		return
	if fade_duration < 0.0:
		fade_duration = music_fade_duration
	var t := create_tween()
	t.tween_property(player, "volume_db", FADE_DB_MIN, fade_duration)
	t.tween_callback(func():
		player.stop()
		player.stream = null
	)

# ==========================================
# SFX 控制（一次性）
# ==========================================
func play_sfx(key: String, random_pitch: bool = false) -> void:
	var stream := _get_sfx(key)
	if stream == null:
		return
	_play_sfx_internal(stream, random_pitch)

func _play_sfx_internal(stream: AudioStream, random_pitch: bool = false) -> void:
	var pitch := 1.0
	if random_pitch:
		pitch = randf_range(0.9, 1.1)

	for player in sfx_players:
		if not player.playing:
			player.stream = stream
			player.pitch_scale = pitch
			player.play()
			return

	var oldest := sfx_players[0]
	oldest.stop()
	oldest.stream = stream
	oldest.pitch_scale = pitch
	oldest.play()

# ==========================================
# 循环 SFX 控制（用于 victory_short / game_over 等需要循环 + 淡出的音效）
# ==========================================
func play_looping_sfx(key: String) -> void:
	var stream := _get_sfx(key)
	if stream == null:
		return
	_looping_sfx_player.stop()
	_looping_sfx_player.stream = stream
	_looping_sfx_player.volume_db = FADE_DB_MIN
	_looping_sfx_player.play()
	_looping_sfx_active = true
	var t := create_tween()
	t.tween_property(_looping_sfx_player, "volume_db", SFX_DEFAULT_DB, 0.3)

func stop_looping_sfx(fade_duration: float = 0.5) -> void:
	if not _looping_sfx_active and not _looping_sfx_player.playing:
		return
	_looping_sfx_active = false
	if fade_duration <= 0.0:
		_looping_sfx_player.stop()
		_looping_sfx_player.stream = null
		_looping_sfx_player.volume_db = FADE_DB_MIN
		return
	var t := create_tween()
	t.tween_property(_looping_sfx_player, "volume_db", FADE_DB_MIN, fade_duration)
	t.tween_callback(func():
		_looping_sfx_player.stop()
		_looping_sfx_player.stream = null
	)

func _on_looping_sfx_finished() -> void:
	if _looping_sfx_active and _looping_sfx_player.stream:
		_looping_sfx_player.play()

# ==========================================
# 音量控制
# ==========================================
func set_volume(bus: Bus, v: float) -> void:
	var bus_name := ""
	match bus:
		Bus.MASTER: bus_name = &"Master"
		Bus.MUSIC: bus_name = MUSIC_BUS
		Bus.SFX: bus_name = SFX_BUS

	var idx := AudioServer.get_bus_index(bus_name)
	if idx < 0:
		push_warning("[SoundManager] 音频总线 '%s' 不存在，请先在 Audio 面板创建" % bus_name)
		return

	var db := linear_to_db(v)
	AudioServer.set_bus_volume_db(idx, db)

# ==========================================
# 音频资源加载
# ==========================================
func _get_bgm(key: String) -> AudioStream:
	if not BGM_PATHS.has(key):
		push_warning("[SoundManager] 未定义 BGM: " + key)
		return null
	return _load_audio(BGM_PATHS[key], _bgm_cache)

func _get_sfx(key: String) -> AudioStream:
	if not SFX_PATHS.has(key):
		push_warning("[SoundManager] 未定义 SFX: " + key)
		return null
	return _load_audio(SFX_PATHS[key], _sfx_cache)

func _load_audio(path: String, cache: Dictionary) -> AudioStream:
	if cache.has(path):
		return cache[path]
	if not ResourceLoader.exists(path):
		push_warning("[SoundManager] 音频文件未找到: " + path)
		return null
	var res := load(path) as AudioStream
	if res:
		cache[path] = res
	return res

# ==========================================
# 信号回调 — 每条对应一个音效
# ==========================================
func _on_choose_layer(_type: int) -> void:
	play_sfx("choose_layer")

func _on_dim_in() -> void:
	stop_bgm()
	play_sfx("walk_dim")

func _on_card_drawn(_card: Control, _count: int) -> void:
	play_sfx("draw_card")

func _on_deck_shuffled() -> void:
	play_sfx("shuffle")

## 时间轴上有玩家卡牌被放入（槽变蓝），播放确认音效
func _on_timeline_action_added(action: TimelineAction) -> void:
	if action.type == TimelineAction.Type.PLAYER:
		play_sfx("confirm_timeline")

func _on_tile_selected(_tile: Area2D) -> void:
	play_sfx("tile_damage")

func _on_combat_victory() -> void:
	play_looping_sfx("combat_win")

func _on_game_win() -> void:
	play_looping_sfx("game_win")

func _on_game_over() -> void:
	play_looping_sfx("game_over")
