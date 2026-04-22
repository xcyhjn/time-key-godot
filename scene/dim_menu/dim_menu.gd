extends CanvasLayer

@onready var rect = $ColorRect

@export_enum("渐暗","渐亮") var mode : int = 0 
@export_enum("自动","手动") var mode2 : int = 0 

var start_opacity
var end_opacity

func _ready() -> void:
	if mode2 == 0:
		use(mode,1)
	
func use(mode_new,_hide):
	start_opacity = Color(0,0,0,0) if (mode_new == 0) else Color(0,0,0,1)
	end_opacity = Color(0,0,0,1) if (mode_new == 0) else Color(0,0,0,0)
	rect = $ColorRect
	rect.color = start_opacity
	self.show()
	var tween = create_tween()
	tween.tween_property(rect,"color",end_opacity,0.4 * Global.speed_index)
	await tween.finished
	tween.kill()
	if _hide == 1:
		self.hide()
