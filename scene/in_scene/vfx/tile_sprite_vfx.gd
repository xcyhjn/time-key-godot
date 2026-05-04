# 功能: 通用地块序列帧特效脚本，供 recover、燃烧、护盾等所有地块动画场景复用。
# 核心逻辑: play_at() 接收 VFXManager 传入的地块局部锚点，再按 TileVFXProfile 应用偏移、缩放、层级、播放速度和自动清理。
class_name TileSpriteVFX
extends Node2D

const DEFAULT_PROFILE: TileVFXProfile = preload("res://scene/in_scene/vfx/default_tile_vfx_profile.tres")

@export var animation_name: StringName = &""
@export var profile: TileVFXProfile = DEFAULT_PROFILE

@onready var player: AnimatedSprite2D = $AnimatedSprite2D


func _ready() -> void:
	if is_instance_valid(player) and not player.animation_finished.is_connected(_on_animation_finished):
		player.animation_finished.connect(_on_animation_finished)
	_apply_profile_settings()


func set_profile_if_empty(default_profile: TileVFXProfile) -> void:
	if profile == null:
		profile = default_profile


func play_at(tile_anchor_position: Vector2) -> void:
	var active_profile: TileVFXProfile = _get_active_profile()
	position = tile_anchor_position + active_profile.window_offset
	play_once()


func play_once() -> void:
	if not is_instance_valid(player) or player.sprite_frames == null:
		queue_free()
		return

	_apply_profile_settings()

	var resolved_animation: StringName = _resolve_animation_name()
	if resolved_animation == &"":
		queue_free()
		return

	var active_profile: TileVFXProfile = _get_active_profile()
	if active_profile.force_non_loop:
		player.sprite_frames.set_animation_loop(resolved_animation, false)

	player.animation = resolved_animation
	player.frame = 0
	player.frame_progress = 0.0
	player.play(resolved_animation)


func _apply_profile_settings() -> void:
	var active_profile: TileVFXProfile = _get_active_profile()
	z_index = active_profile.vfx_z_index
	if not is_instance_valid(player):
		return

	player.speed_scale = maxf(active_profile.playback_speed_scale, 0.01)
	_apply_window_scale(active_profile)


func _apply_window_scale(active_profile: TileVFXProfile) -> void:
	var frame_texture: Texture2D = _get_first_frame_texture()
	if frame_texture == null:
		return

	var frame_size: Vector2 = frame_texture.get_size()
	if frame_size.x <= 0.0 or frame_size.y <= 0.0:
		return

	var safe_window_size: Vector2 = Vector2(maxf(active_profile.window_size.x, 1.0), maxf(active_profile.window_size.y, 1.0))
	if active_profile.stretch_to_window:
		player.scale = Vector2(safe_window_size.x / frame_size.x, safe_window_size.y / frame_size.y)
	else:
		var uniform_scale: float = minf(safe_window_size.x / frame_size.x, safe_window_size.y / frame_size.y)
		player.scale = Vector2.ONE * uniform_scale


func _get_first_frame_texture() -> Texture2D:
	if not is_instance_valid(player) or player.sprite_frames == null:
		return null

	var resolved_animation: StringName = _resolve_animation_name()
	if resolved_animation == &"":
		return null

	if player.sprite_frames.get_frame_count(resolved_animation) <= 0:
		return null
	return player.sprite_frames.get_frame_texture(resolved_animation, 0)


func _resolve_animation_name() -> StringName:
	if not is_instance_valid(player) or player.sprite_frames == null:
		return &""

	if animation_name != &"" and player.sprite_frames.has_animation(animation_name):
		return animation_name

	var names: PackedStringArray = player.sprite_frames.get_animation_names()
	if names.is_empty():
		return &""
	return StringName(names[0])


func _get_active_profile() -> TileVFXProfile:
	if profile != null:
		return profile
	return DEFAULT_PROFILE


func _on_animation_finished() -> void:
	if _get_active_profile().auto_free_on_finish:
		queue_free()
