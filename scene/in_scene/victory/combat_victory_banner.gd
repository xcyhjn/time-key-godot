class_name CombatVictoryBanner
extends CanvasLayer

signal banner_finished

@export var banner_text: String = "战斗胜利"

@onready var center_container: CenterContainer = $CenterContainer
@onready var panel: PanelContainer = $CenterContainer/PanelContainer
@onready var label: Label = $CenterContainer/PanelContainer/Label

var is_playing: bool = false


func _ready() -> void:
	hide()
	_apply_styles()


func _apply_styles() -> void:
	var style = StyleBoxFlat.new()
	style.bg_color = Color(0.08, 0.09, 0.13, 0.92)
	style.border_color = Color(0.95, 0.82, 0.28, 1.0)
	style.set_border_width_all(3)
	style.set_corner_radius_all(16)
	panel.add_theme_stylebox_override("panel", style)

	label.text = banner_text


func play_banner(text: String = banner_text) -> void:
	if is_playing:
		return

	is_playing = true
	label.text = text
	show()

	panel.scale = Vector2(0.82, 0.82)
	panel.modulate = Color(1.0, 1.0, 1.0, 0.0)
	label.modulate = Color(1.0, 1.0, 1.0, 0.0)

	var tw = create_tween().set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	tw.set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tw.tween_property(panel, "scale", Vector2.ONE, 0.22)
	tw.parallel().tween_property(panel, "modulate:a", 1.0, 0.18)
	tw.parallel().tween_property(label, "modulate:a", 1.0, 0.18)
	tw.tween_interval(0.75)
	tw.tween_property(panel, "modulate:a", 0.0, 0.28).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN)
	tw.parallel().tween_property(label, "modulate:a", 0.0, 0.22).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN)

	await tw.finished
	hide()
	is_playing = false
	banner_finished.emit()
