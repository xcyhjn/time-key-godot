extends TextureProgressBar


func _ready() -> void:
	Global.phase_change.connect(update)
	update()
	

func update():
	value = 100.0 * Global.phase / max_value
