extends ColorRect

func play_burn_out(s: float):
	if material:
		var tw = create_tween().set_parallel(true).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
		tw.tween_property(material, "shader_parameter/grayscale_amount", 1.0, 1.5 * s)
		tw.tween_property(material, "shader_parameter/burn_intensity", 0.5, 1.8 * s)
