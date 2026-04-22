extends TextureButton

@export var target_label: Label
@onready var shadow_rect = $TextureRect # 获取子节点 ColorRect

func _ready() -> void:
	mouse_entered.connect(func(): 
		if material:
			material.set_shader_parameter("is_hovered", true)
		if target_label and target_label.material:
			target_label.material.set_shader_parameter("is_hovered", true)
		if shadow_rect and shadow_rect.material:
			shadow_rect.material.set_shader_parameter("is_hovered", true)
	)
	
	mouse_exited.connect(func(): 
		if material:
			material.set_shader_parameter("is_hovered", false)
		if target_label and target_label.material:
			target_label.material.set_shader_parameter("is_hovered", false)
		if shadow_rect and shadow_rect.material:
			shadow_rect.material.set_shader_parameter("is_hovered", false)
	)
