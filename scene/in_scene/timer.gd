extends AnimatedSprite2D

func _ready() -> void:
	material.set_shader_parameter("glow_intensity", 0.0)
	modulate.a = 0
	pass

func Frame_Up(Ani : bool = true):
	if Ani:
		play_glow_transition()
	if frame < 7:
		frame += 1
	else:
		frame = 0



func Toggle_modulate(invisiable : bool = true):
	#print("正在调整透明度……可见设置为" + str(invisiable))
	if modulate.a != 1:
		#print("正在提高透明度")
		up_modulate()
	else:
		if invisiable:
			#print("正在降低透明度至不可见")
			await invisiable_modulate()
		else:
			#print("正在降低透明度至可见")
			await visiable_modulate()


func invisiable_modulate():
	var tween = create_tween()
	tween.tween_property(self, "modulate:a", 0.0, 1.0).set_ease(Tween.EASE_IN)

func up_modulate():
	var tween = create_tween()
	tween.tween_property(self, "modulate:a", 1.0, 2.0).set_ease(Tween.EASE_OUT)

func visiable_modulate():
	var tween = create_tween()
	tween.tween_property(self, "modulate:a", 0.0, 1.0).set_ease(Tween.EASE_IN)

func play_glow_transition():
	var tween = create_tween()
	tween.tween_method(
		func(value): material.set_shader_parameter("glow_intensity", value),
		0.0, 1.0, 0.2
	).set_ease(Tween.EASE_OUT)
	tween.tween_method(
		func(value): material.set_shader_parameter("glow_intensity", value),
		1.0, 0.0, 0.8
	).set_ease(Tween.EASE_IN)
