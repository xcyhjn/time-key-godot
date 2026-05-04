# 功能: 地块回复特效场景脚本，负责播放场景内已经配置好的 AnimatedSprite2D 动画。
# 核心逻辑: play_at() 接收地块锚点后，自行应用窗口偏移、窗口缩放、层级和播放速度；帧切分留在 tile_recover_vfx.tscn 中手动调试。
class_name TileRecoverVFX
extends Node2D

@export_group("播放设置")
@export var animation_name: StringName = &"recover"
@export var auto_free_on_finish: bool = true
## 运行时强制关闭动画循环，确保一次播放结束后能触发清理。
@export var force_non_loop: bool = true
@export var playback_speed_scale: float = 1.0

@export_group("地块窗口")
## 每个地块用于播放回复动画的固定窗口大小，动画帧会按这个窗口压缩。
@export var window_size: Vector2 = Vector2(160.0, 160.0)
## 窗口相对地块碰撞中心的位置偏移，y 越小越靠上。
@export var window_offset: Vector2 = Vector2(0.0, -20.0)
## 开启时强制拉伸到窗口大小；关闭时等比缩放进窗口。
@export var stretch_to_window: bool = true
## 特效层级，需高于地块和建筑。
@export var vfx_z_index: int = 3500

@onready var player: AnimatedSprite2D = $AnimatedSprite2D


func _ready() -> void:
	if is_instance_valid(player) and not player.animation_finished.is_connected(_on_animation_finished):
		player.animation_finished.connect(_on_animation_finished)
	_apply_scene_settings()


func play_at(tile_anchor_position: Vector2) -> void:
	position = tile_anchor_position + window_offset
	play_once()


func play_once() -> void:
	if not is_instance_valid(player) or player.sprite_frames == null:
		queue_free()
		return

	_apply_scene_settings()

	var resolved_animation: StringName = _resolve_animation_name()
	if resolved_animation == &"":
		queue_free()
		return

	if force_non_loop:
		player.sprite_frames.set_animation_loop(resolved_animation, false)

	player.animation = resolved_animation
	player.frame = 0
	player.frame_progress = 0.0
	player.play(resolved_animation)


func _apply_scene_settings() -> void:
	z_index = vfx_z_index
	if not is_instance_valid(player):
		return

	player.speed_scale = maxf(playback_speed_scale, 0.01)
	_apply_window_scale()


func _apply_window_scale() -> void:
	var frame_texture: Texture2D = _get_first_frame_texture()
	if frame_texture == null:
		return

	var frame_size: Vector2 = frame_texture.get_size()
	if frame_size.x <= 0.0 or frame_size.y <= 0.0:
		return

	var safe_window_size: Vector2 = Vector2(maxf(window_size.x, 1.0), maxf(window_size.y, 1.0))
	if stretch_to_window:
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

	if player.sprite_frames.has_animation(animation_name):
		return animation_name

	var names: PackedStringArray = player.sprite_frames.get_animation_names()
	if names.is_empty():
		return &""
	return StringName(names[0])


func _on_animation_finished() -> void:
	if auto_free_on_finish:
		queue_free()
