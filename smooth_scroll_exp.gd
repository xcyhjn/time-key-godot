extends Node2D

# ==============================================================================
# Godot 4 六边形地图生成系统 - 研究报告附带完整实现
# ==============================================================================
# 功能概要：
# 1. 生成半径为 N 的正六边形网格（Cube/Axial 坐标系）。
# 2. 使用 MultiMeshInstance2D 实现 7000+ 单元的高性能渲染。
# 3. 程序化生成网格几何体（SurfaceTool），无美术资源依赖。
# 4. 实现扇区划分、随机填充、以及全貌/局部视图切换逻辑。
# ==============================================================================

# --- 可调整的 Uniforms (导出变量) ---
@export_group("鼠标交互配置 (Mouse Interaction)")
## 鼠标滚轮缩放的灵敏度 (每次滚动的比例)
@export var zoom_step: float = 0.1
## 缩放平滑度 (数值越大越快，越小越平滑)
@export var zoom_smoothness: float = 10.0
## 鼠标拖动的灵敏度
@export var drag_sensitivity: float = 1.0

# --- 内部变量 ---
var _target_zoom: Vector2 # 目标缩放值，用于平滑过渡
var _is_dragging: bool = false # 标记鼠标是否按下

@export_group("地图基础配置 (Map Settings)")
## 六边形总数控制：通过半径控制。总数公式 = 3*n*(n+1)+1。
## 半径 50 对应约 7651 个六边形；半径 60 对应约 10981 个。
@export var map_radius: int = 50

## 六边形尺寸（外接圆半径）。
## 需求：对角线 101 -> 半径 50.5。
@export var hex_size: float = 50.5

@export_group("视觉样式配置 (Visual Style)")
## 生成六边形的边框粗细。
@export var border_thickness: float = 3.0

## 4. 未填充时六边形的边框颜色 (灰色)。
@export var color_empty_border: Color = Color(0.5, 0.5, 0.5, 1.0)

## 3. 生成六边形的填充颜色 (绿色)。
@export var color_filled_fill: Color = Color(0.2, 0.8, 0.2, 1.0)

## 生成六边形的边框颜色 (黑色，用于区分绿色块)。
@export var color_filled_border: Color = Color(0.0, 0.0, 0.0, 1.0)

@export_group("生成逻辑配置 (Generation Logic)")
## 5. 每次按按钮生成的六边形数量最小值。
@export var spawn_min: int = 30
## 5. 每次按按钮生成的六边形数量最大值。
@export var spawn_max: int = 50

@export_group("视图控制 (View Control)")
## 放大后摄像机的移动速度。
@export var camera_speed: float = 1500.0

# --- 内部变量与系统组件 ---

# 数据存储：字典 { Vector2i(q, r) : is_occupied(bool) }
# 使用轴向坐标 (q, r)，其中 s = -q-r
var grid_data: Dictionary = {}

# 渲染节点
var mm_background: MultiMeshInstance2D # 背景层（空心）
var mm_foreground: MultiMeshInstance2D # 前景层（实心）

# 交互组件
var camera: Camera2D
var ui_layer: CanvasLayer
var status_label: Label

# 状态标记
var is_zoomed: bool = false
var zoom_overview: Vector2 = Vector2.ONE # 全貌缩放比
var zoom_detail: Vector2 = Vector2.ONE   # 局部缩放比

# 数学常量
const SQRT3 = 1.73205080757

# --- 生命周期方法 ---

func _ready() -> void:
	# 1. 强制设置窗口大小 (需求: 1280*800)
	# 注意：在实际项目中建议在“项目设置”中配置，此处代码用于确保运行时尺寸。
	get_window().size = Vector2i(1280, 800)
	
	# 2. 初始化核心系统
	_init_grid_data()      # 构建坐标数据
	_setup_rendering()     # 构建渲染管线和网格
	_setup_camera()        # 配置摄像机
	_setup_ui()            # 构建 UI 界面
	
	# 3. 生成初始状态 (中心 + 一圈)
	_spawn_initial_cluster()
	
	# 4. 首次渲染更新
	_update_foreground_visuals()
	_target_zoom = camera.zoom
	print("系统就绪。网格半径: %d, 总单元数: %d" % [map_radius, grid_data.size()])

func _process(delta: float) -> void:
	# 仅在放大状态下允许键盘移动摄像机
	if is_zoomed:
		_handle_camera_movement(delta)
	
	# 2. [新增] 平滑缩放插值
	# 利用 lerp 让 current zoom 逐渐接近 target zoom
	if camera.zoom.distance_squared_to(_target_zoom) > 0.00001:
		camera.zoom = camera.zoom.lerp(_target_zoom, zoom_smoothness * delta)

# --- 核心模块 I: 渲染管线搭建 ---
func _unhandled_input(event: InputEvent) -> void:
	# 1. 处理鼠标滚轮缩放
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			# 向上滚：放大 (Zoom In)
			_change_zoom_target(1.0 + zoom_step)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			# 向下滚：缩小 (Zoom Out)
			_change_zoom_target(1.0 / (1.0 + zoom_step))
		
		# 2. 处理鼠标按住拖动 (中键或左键)
		if event.button_index == MOUSE_BUTTON_LEFT or event.button_index == MOUSE_BUTTON_MIDDLE:
			if event.pressed:
				_is_dragging = true
			else:
				_is_dragging = false

	# 3. 处理拖动位移
	if event is InputEventMouseMotion and _is_dragging:
		# 移动量 = 鼠标相对移动 / 当前缩放倍率 (这样放大后拖动不会太快)
		camera.position -= event.relative * drag_sensitivity / camera.zoom.x
		_clamp_camera_position() # 限制范围
		
		# 如果手动拖动了，我们就认为进入了“放大/自由”状态
		is_zoomed = true 

# [辅助函数] 安全地修改缩放目标
func _change_zoom_target(multiplier: float) -> void:
	_target_zoom *= multiplier
	
	# 限制缩放范围
	# 最小缩放：全貌视图 (zoom_overview)
	# 最大缩放：局部视图的 2 倍 (zoom_detail * 2)，避免放得太大看不清
	var min_z = zoom_overview.x
	var max_z = zoom_detail.x * 2.0
	
	_target_zoom.x = clamp(_target_zoom.x, min_z, max_z)
	_target_zoom.y = clamp(_target_zoom.y, min_z, max_z)
	
	# 如果正在滚动，也视为进入了缩放状态
	is_zoomed = true
	
func _setup_rendering() -> void:
	# 创建背景层（显示空心灰色六边形）
	mm_background = MultiMeshInstance2D.new()
	mm_background.name = "BackgroundGrid"
	mm_background.z_index = 0 # 底层
	add_child(mm_background)
	
	# 生成空心网格模型
	var mesh_bg = _generate_procedural_mesh(hex_size, border_thickness, color_empty_border, Color.TRANSPARENT, false)
	mm_background.multimesh = MultiMesh.new()
	mm_background.multimesh.mesh = mesh_bg
	mm_background.multimesh.transform_format = MultiMesh.TRANSFORM_2D
	mm_background.multimesh.instance_count = grid_data.size()
	
	# 填充背景层实例变换（一次性静态填充）
	var idx = 0
	for hex in grid_data:
		var pos = _hex_to_pixel(hex)
		var xform = Transform2D(0.0, pos) # 无旋转，位移为 pos
		mm_background.multimesh.set_instance_transform_2d(idx, xform)
		idx += 1
		
	# 创建前景层（显示实心绿色六边形）
	mm_foreground = MultiMeshInstance2D.new()
	mm_foreground.name = "ForegroundGrid"
	mm_foreground.z_index = 1 # 顶层
	add_child(mm_foreground)
	
	# 生成实心网格模型 (带黑色边框)
	var mesh_fg = _generate_procedural_mesh(hex_size, border_thickness, color_filled_border, color_filled_fill, true)
	mm_foreground.multimesh = MultiMesh.new()
	mm_foreground.multimesh.mesh = mesh_fg
	mm_foreground.multimesh.transform_format = MultiMesh.TRANSFORM_2D
	# 预分配最大容量，避免运行时重新分配缓冲
	mm_foreground.multimesh.instance_count = grid_data.size()
	mm_foreground.multimesh.visible_instance_count = 0 # 初始不可见

# 使用 SurfaceTool 程序化生成六边形 Mesh
func _generate_procedural_mesh(radius: float, thick: float, border_col: Color, fill_col: Color, is_filled: bool) -> ArrayMesh:
	var st = SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	
	# 计算内径和外径
	var r_out = radius
	var r_in = radius - thick
	
	# 生成 6 个扇区
	for i in range(6):
		# Godot 坐标系：-90度为正上方 (Pointy Top 的顶点)
		# 角度：-90, -30, 30, 90, 150, 210
		var deg_start = -90 + (i * 60)
		var deg_end = -90 + ((i + 1) * 60)
		
		var rad_start = deg_to_rad(deg_start)
		var rad_end = deg_to_rad(deg_end)
		
		# 计算外圈顶点
		var v_out_1 = Vector2(cos(rad_start), sin(rad_start)) * r_out
		var v_out_2 = Vector2(cos(rad_end), sin(rad_end)) * r_out
		
		# 计算内圈顶点
		var v_in_1 = Vector2(cos(rad_start), sin(rad_start)) * r_in
		var v_in_2 = Vector2(cos(rad_end), sin(rad_end)) * r_in
		
		# 1. 构建边框 (由两个三角形组成的四边形)
		st.set_color(border_col)
		# 三角形 1
		st.add_vertex(Vector3(v_out_1.x, v_out_1.y, 0))
		st.add_vertex(Vector3(v_out_2.x, v_out_2.y, 0))
		st.add_vertex(Vector3(v_in_1.x, v_in_1.y, 0))
		# 三角形 2
		st.add_vertex(Vector3(v_in_1.x, v_in_1.y, 0))
		st.add_vertex(Vector3(v_out_2.x, v_out_2.y, 0))
		st.add_vertex(Vector3(v_in_2.x, v_in_2.y, 0))
		
		# 2. 构建填充 (如果需要)
		if is_filled:
			st.set_color(fill_col)
			st.add_vertex(Vector3(0, 0, 0)) # 中心点
			st.add_vertex(Vector3(v_in_1.x, v_in_1.y, 0))
			st.add_vertex(Vector3(v_in_2.x, v_in_2.y, 0))
			
	st.index()
	return st.commit()

# --- 核心模块 II: 网格数据与算法 ---

func _init_grid_data() -> void:
	# 遍历生成半径为 map_radius 的所有六边形坐标
	# 约束条件：max(|q|, |r|, |s|) <= N
	for q in range(-map_radius, map_radius + 1):
		var r_min = max(-map_radius, -q - map_radius)
		var r_max = min(map_radius, -q + map_radius)
		for r in range(r_min, r_max + 1):
			grid_data[Vector2i(q, r)] = false # false = 空

# 坐标转换：Axial -> Pixel (Pointy Top)
func _hex_to_pixel(hex: Vector2i) -> Vector2:
	var x = hex_size * SQRT3 * (hex.x + (hex.y / 2.0))
	var y = hex_size * 1.5 * hex.y
	return Vector2(x, y)

# 初始生成：中心 + 周围一圈
func _spawn_initial_cluster() -> void:
	var center = Vector2i(0, 0)
	var coords = [center]
	
	# 六个邻居方向
	var neighbors = [
		Vector2i(1, 0), Vector2i(1, -1), Vector2i(0, -1),
		Vector2i(-1, 0), Vector2i(-1, 1), Vector2i(0, 1)
	]
	for n in neighbors:
		coords.append(center + n)
	
	for c in coords:
		if c in grid_data:
			grid_data[c] = true # 标记为填充

# 核心算法：扇区随机生成
func _on_sector_button_pressed(sector_id: int) -> void:
	# 1. 筛选候选集 (Candidate Filtering)
	var candidates: Array[Vector2i] = []
	
	for hex in grid_data:
		# 必须是未填充的
		if grid_data[hex] == false:
			# 必须属于目标扇区
			if _get_sector_id(hex) == sector_id:
				candidates.append(hex)
	
	# 2. 检查是否已满
	if candidates.is_empty():
		status_label.text = "区域 %d 已满！" % sector_id
		return
	
	status_label.text = "正在区域 %d 生成..." % sector_id
	
	# 3. 随机选择 (Shuffle & Slice)
	candidates.shuffle()
	var count = randi_range(spawn_min, spawn_max)
	count = min(count, candidates.size())
	
	for i in range(count):
		grid_data[candidates[i]] = true
		
	# 4. 更新视觉
	_update_foreground_visuals()

# 扇区判断算法
func _get_sector_id(hex: Vector2i) -> int:
	var pos = _hex_to_pixel(hex)
	# atan2 返回 (-PI, PI]，Godot 中 -PI/2 为正上 (Up)
	var angle_deg = rad_to_deg(atan2(pos.y, pos.x))
	
	# 需求映射：
	# 1: 正上 (-90) -> 右上 (-30)
	if angle_deg >= -90 and angle_deg < -30: return 1
	# 2: 右上 (-30) -> 右下 (30)
	if angle_deg >= -30 and angle_deg < 30: return 2
	# 3: 右下 (30) -> 正下 (90)
	if angle_deg >= 30 and angle_deg < 90: return 3
	# 4: 正下 (90) -> 左下 (150)
	if angle_deg >= 90 and angle_deg < 150: return 4
	# 5: 左下 (150) -> 左上 (-150) (跨越 ±180)
	if angle_deg >= 150 or angle_deg < -150: return 5
	# 6: 左上 (-150) -> 正上 (-90)
	if angle_deg >= -150 and angle_deg < -90: return 6
	
	return 1 # Fallback

# 刷新前景层 (仅更新变换数据)
func _update_foreground_visuals() -> void:
	var idx = 0
	for hex in grid_data:
		if grid_data[hex] == true:
			var pos = _hex_to_pixel(hex)
			var xform = Transform2D(0.0, pos)
			mm_foreground.multimesh.set_instance_transform_2d(idx, xform)
			idx += 1
	# 设置可见数量
	mm_foreground.multimesh.visible_instance_count = idx

# --- 核心模块 III: 摄像机与交互 ---

func _setup_camera() -> void:
	camera = Camera2D.new()
	add_child(camera)
	
	# 计算地图物理尺寸 (像素)
	# 宽 = (2N + 1) * w. 高 = (2N + 1) * 1.5R.
	var map_w = (map_radius * 2 + 1) * hex_size * SQRT3
	var map_h = (map_radius * 2 + 1) * hex_size * 1.5
	
	# 计算全貌缩放 (Overview Zoom)
	# 留出 10% 边距
	var vp_size = get_viewport_rect().size
	var zoom_x = vp_size.x / (map_w * 1.1)
	var zoom_y = vp_size.y / (map_h * 1.1)
	var z_overview = min(zoom_x, zoom_y)
	zoom_overview = Vector2(z_overview, z_overview)
	
	# 计算局部缩放 (Detail Zoom)
	# 需求：看见 1/6。全貌看见 6/6。故放大 6 倍。
	zoom_detail = zoom_overview * 6.0
	
	# 初始状态
	camera.zoom = zoom_overview
	camera.position = Vector2.ZERO

func _toggle_zoom(zoom_in: bool) -> void:
	is_zoomed = zoom_in
	
	if is_zoomed:
		# 按钮点击放大：设置目标为局部视角
		_target_zoom = zoom_detail
	else:
		# 按钮点击全貌：设置目标为全貌，并归位
		_target_zoom = zoom_overview
		# 位置归零依然可以使用 Tween，因为 _process 里没有 lerp 位置
		var tween = create_tween().set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
		tween.tween_property(camera, "position", Vector2.ZERO, 0.5)
# --- 核心模块 IV: UI 构建 ---
# 修改原有的键盘控制函数
func _handle_camera_movement(delta: float) -> void:
	var dir = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
	camera.position += dir * camera_speed * delta
	_clamp_camera_position() # <--- 改为调用函数

# [新增] 统一的边界限制函数
func _clamp_camera_position() -> void:
	var limit_x = map_radius * hex_size * SQRT3
	var limit_y = map_radius * hex_size * 1.5
	camera.position.x = clamp(camera.position.x, -limit_x, limit_x)
	camera.position.y = clamp(camera.position.y, -limit_y, limit_y)
	
func _setup_ui() -> void:
	ui_layer = CanvasLayer.new()
	add_child(ui_layer)
	
	var panel = PanelContainer.new()
	panel.position = Vector2(20, 20)
	ui_layer.add_child(panel)
	
	var vbox = VBoxContainer.new()
	panel.add_child(vbox)
	
	status_label = Label.new()
	status_label.text = "系统就绪 (半径: %d)" % map_radius
	vbox.add_child(status_label)
	
	vbox.add_child(HSeparator.new())
	
	# 6 个扇区按钮
	var grid_con = GridContainer.new()
	grid_con.columns = 3
	vbox.add_child(grid_con)
	
	for i in range(1, 7):
		var btn = Button.new()
		btn.text = "区域 %d" % i
		btn.pressed.connect(_on_sector_button_pressed.bind(i))
		grid_con.add_child(btn)
		
	vbox.add_child(HSeparator.new())
	
	# 缩放控制按钮
	var btn_zoom_in = Button.new()
	btn_zoom_in.text = "放大 (允许移动)"
	btn_zoom_in.pressed.connect(_toggle_zoom.bind(true))
	vbox.add_child(btn_zoom_in)
	
	var btn_zoom_out = Button.new()
	btn_zoom_out.text = "全貌 (重置视角)"
	btn_zoom_out.pressed.connect(_toggle_zoom.bind(false))
	vbox.add_child(btn_zoom_out)
