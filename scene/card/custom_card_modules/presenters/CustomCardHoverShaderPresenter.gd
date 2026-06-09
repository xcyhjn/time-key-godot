extends RefCounted


## CustomCardHoverShaderPresenter 只负责切换卡牌 hover shader 参数。
## 它不负责判断卡牌状态、不请求 tooltip、不创建 tween，也不处理选中、拖拽或出牌流程。

func set_hover_shader(card_material: Material, front_face_texture: Variant, active: bool) -> void:
	if card_material is ShaderMaterial:
		card_material.set_shader_parameter("is_hovered", active)
	elif front_face_texture is CanvasItem and front_face_texture.material is ShaderMaterial:
		front_face_texture.material.set_shader_parameter("is_hovered", active)
