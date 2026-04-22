extends Node2D
class_name VictoryOrchestrator

@onready var animation_player: AnimationPlayer = $AnimationRoot/AnimationPlayer
@onready var key_sprite: Sprite2D = $AnimationRoot/KeyAnchor/KeySprite

@export var target_zoom: float = 2.5
@export var zoom_duration: float = 1.0

var main_camera: Camera2D
signal ended

func _ready() -> void:
	z_index = 2000
	if key_sprite: key_sprite.modulate.a = 0

## 启动胜利演出
func start_performance(target_pos: Vector2) -> void:
	self.global_position = target_pos
	main_camera = get_viewport().get_camera_2d()
	var target = get_viewport().get_visible_rect().size / 2.0
	var tween = create_tween().set_parallel(true).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	tween.tween_property(main_camera, "global_position", target, zoom_duration)
	tween.tween_property(main_camera, "zoom", Vector2(target_zoom, target_zoom), zoom_duration)
	await tween.finished
	_play_victory_animation()

## 播放钥匙升起动画
func _play_victory_animation() -> void:
	if key_sprite: key_sprite.modulate.a = 1
	if animation_player and animation_player.has_animation("victory_rise"):
		animation_player.play("victory_rise")
		var t = create_tween()
		t.tween_method(func(_v): if main_camera: main_camera.global_position = key_sprite.global_position, 
			0, 1, animation_player.get_animation("victory_rise").length)
		await animation_player.animation_finished
		ended.emit()
	
