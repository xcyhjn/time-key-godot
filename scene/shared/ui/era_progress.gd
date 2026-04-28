extends TextureProgressBar


func _ready() -> void:
	fill_mode = TextureProgressBar.FILL_LEFT_TO_RIGHT

	if GlobalClock and GlobalClock.has_signal("progress_changed"):
		GlobalClock.progress_changed.connect(_on_global_clock_progress_changed)

	update()
	

func update():
	var phase_value := 1
	if GlobalClock and GlobalClock.has_method("get_current_phase"):
		phase_value = int(GlobalClock.get_current_phase())

	value = clamp(float(phase_value), min_value, max_value)


func _on_global_clock_progress_changed(_era_value: int, _phase_value: int) -> void:
	update()
