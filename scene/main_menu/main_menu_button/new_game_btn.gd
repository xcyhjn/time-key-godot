extends Button


func _ready() -> void:
	# 利用 Lambda 匿名函数直接接管鼠标悬浮信号，为主界面的 Shader 传递开关。
	# 这两行代码完全独立，不会影响你后续写按下的业务逻辑。
	mouse_entered.connect(func(): material.set_shader_parameter("is_hovered", true))
	mouse_exited.connect(func(): material.set_shader_parameter("is_hovered", false))
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
