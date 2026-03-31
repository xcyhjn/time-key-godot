extends Node2D

@export_group("地块贴图")
@export var hex_top_tex: Texture2D
@export var hex_side_tex: Texture2D
@export var block_material: ShaderMaterial

@export_group("地块参数")
@export var spacing_x: float = 168.24
@export var spacing_y: float = 96.24
@export var step_height: float = 48.0
@export var tile_scale: float = 0.6

const REF_SCALE: float = 0.6
var map_root: Node2D

# --- 关键：接收从地图场景传来的变量 ---
var received_text: String = "" # 用于接收 pending_data
var parts = received_text.split("|")
var room_type = parts[0]

func _ready():
	self.y_sort_enabled = true
	
	parts = received_text.split("|")
	room_type = parts[0]
	
	# 1. 打印接收到的信息，方便调试
	if parts.size() > 1:
		var features = parts[1].split(",") 
		# features 将会是 ["enhance", "combine"]
		for feature in features:
			print("该房间拥有特性:", feature)
	print("[Project 场景] 已进入，接收到的事件类型为: ", room_type)
	
	map_root = Node2D.new()
	map_root.y_sort_enabled = true
	add_child(map_root)

	var screen_size = get_viewport_rect().size
	map_root.position = Vector2(screen_size.x * 0.5, screen_size.y * 0.5)

	# 2. 根据不同的事件类型，初始化不同的视觉效果（可选）
	handle_event_logic()

	# 3. 生成网格
	generate_hex_grid()

func handle_event_logic():
	# 这里你可以根据接收到的字符串，修改场景的背景颜色、背景音乐或生成参数
	match room_type:
		"battle_normal":
			print(">> 准备：普通战斗环境")
		"battle_elite":
			print(">> 准备：精英挑战环境（地块可能更高）")
		"boss_stage":
			print(">> 准备：BOSS 战环境")

func generate_hex_grid():
	var hex_directions = [
		Vector2(0, 0), Vector2(1, 0), Vector2(1, -1),
		Vector2(0, -1), Vector2(-1, 0), Vector2(-1, 1), Vector2(0, 1)
	]

	var ratio = tile_scale / REF_SCALE
	var final_sp_x = spacing_x * ratio
	var final_sp_y = spacing_y * ratio
	var final_step_h = step_height * ratio

	# 根据事件类型调整基础高度
	var base_h_min = 1
	var base_h_max = 5
	
	if received_text == "battle_elite":
		pass

	var central_height = randi_range(base_h_min, base_h_max)
	
	for hex_coord in hex_directions:
		var height: int
		if hex_coord == Vector2(0, 0): height = central_height
		elif hex_coord == Vector2(1, -1) or hex_coord == Vector2(0, -1): height = randi_range(central_height, base_h_max + 2)
		else: height = randi_range(1, central_height + 1)
		
		var screen_x = hex_coord.x * final_sp_x
		var screen_y = hex_coord.y * final_sp_y + (hex_coord.x * final_sp_y * 0.5)
		
		create_stack_at(Vector2(screen_x, screen_y), height, final_step_h)

func create_stack_at(pos: Vector2, height: int, current_step_h: float):
	var stack_container = Area2D.new()
	stack_container.position = pos
	stack_container.y_sort_enabled = true 
	map_root.add_child(stack_container)
	
	var sprites_in_stack = []
	
	for i in range(height):
		var sprite = Sprite2D.new()
		sprite.texture = hex_top_tex if i == height - 1 else hex_side_tex
		sprite.centered = false
		sprite.offset = Vector2(-256, -400) 
		sprite.position.y = -i * current_step_h
		sprite.scale = Vector2(tile_scale, tile_scale)
		sprite.z_index = i 
		
		if block_material:
			sprite.material = block_material.duplicate()
		
		if i < height - 1:
			sprite.modulate = Color(0.8, 0.8, 0.8)
			
		stack_container.add_child(sprite)
		sprites_in_stack.append(sprite)
	
	var label = add_height_label(stack_container, height, current_step_h)
	label.visible = false 
	
	var collision = CollisionShape2D.new()
	var shape = CircleShape2D.new()
	shape.radius = 80.0 * tile_scale 
	collision.shape = shape
	collision.position = Vector2(0, -(height - 1) * current_step_h - 50 * tile_scale)
	stack_container.add_child(collision)

	stack_container.mouse_entered.connect(_on_stack_hover.bind(sprites_in_stack, label, true))
	stack_container.mouse_exited.connect(_on_stack_hover.bind(sprites_in_stack, label, false))

func _on_stack_hover(sprites: Array, label: Label, is_on: bool):
	label.visible = is_on
	for s in sprites:
		if s.material:
			s.material.set_shader_parameter("is_highlighted", is_on)

func add_height_label(_parent_node: Node2D, _height_value: int, current_step_h: float) -> Label:
	var label = Label.new()
	label.text = "H: " + str(_height_value)
	label.add_theme_font_size_override("font_size", 24)
	label.add_theme_color_override("font_outline_color", Color.BLACK)
	label.add_theme_constant_override("outline_size", 4)
	
	var vertical_offset = -(_height_value * current_step_h) - 100 
	label.position = Vector2(-30, vertical_offset)
	label.z_index = 100 
	_parent_node.add_child(label)
	return label

# 如果需要返回地图，可以添加一个返回功能
func _unhandled_input(event):
	if event.is_action_pressed("ui_cancel"): # 按下 Esc
		# 这里可以写返回地图的代码
		pass
