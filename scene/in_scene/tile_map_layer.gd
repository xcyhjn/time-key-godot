extends TileMapLayer

@export var camera: Camera2D
@export var chunk_radius: int = 35 # 【调大这个数值】如果缩放后还是穿帮，继续改大（如 50）

var previous_camera_grid_pos: Vector2i = Vector2i(-999, -999) # 初始值设为不可能的坐标

func _ready() -> void:
	# 确保在绝对底层
	z_index = -100 

func _process(_delta: float) -> void:
	if not is_instance_valid(camera): return
	
	# 获取相机的局部网格坐标
	var current_camera_grid_pos = local_to_map(to_local(camera.global_position))
	
	# 只有当相机跨越到新的格子时，才重新铺砖（极大节省性能）
	if current_camera_grid_pos != previous_camera_grid_pos:
		_update_infinite_grid(current_camera_grid_pos)
		previous_camera_grid_pos = current_camera_grid_pos

func _update_infinite_grid(center_pos: Vector2i) -> void:
	clear() # 清空旧地砖
	
	# 在相机周围生成矩形地砖阵列
	for x in range(center_pos.x - chunk_radius, center_pos.x + chunk_radius):
		for y in range(center_pos.y - chunk_radius, center_pos.y + chunk_radius):
			# 关键修复：第一个 Vector2i(x,y) 是铺砖的真实世界位置
			# 第二个 0 是你在 TileSet 里的源 ID（通常是0）
			# 第三个 Vector2i(0,0) 意味着“永远取这张图集的第 1 张图”
			set_cell(Vector2i(x, y), 0, Vector2i(0, 0))
