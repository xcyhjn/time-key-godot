# 功能: 单体血条管理器，监听 HexMap.CreateBar 并为地貌/敌人生成对应血条。
# 核心逻辑: Create_Blood_Bar 负责实例化血条、绑定生命值信号，并在地图入场动画期间把请求交给 HexMap 延后处理。
extends Node2D
class_name BarManager

var HealthBar : Array[PackedScene] = [
	preload("res://scene/in_scene/enermy/health_bar_middle.tscn"),
	preload("res://scene/in_scene/enermy/health_bar_enemy.tscn")
]

@export_group("单体血条位置")
## 单体血条相对建筑锚点的世界偏移。
## 负 Y 会将血条整体上抬，减少对地块碰撞热区的遮挡。
@export var health_bar_world_offset: Vector2 = Vector2(0.0, -56.0)
## 单体血条缩放倍率。若觉得数字或血条过大挡住地块，可适当调小。
@export var health_bar_scale: Vector2 = Vector2(2.2, 2.2)
## 为每个血条 root 预留的小尺寸范围，避免使用全屏根控件导致 hover 热区异常。
@export var health_bar_root_size: Vector2 = Vector2(240.0, 48.0)

func _ready() -> void:
	# 确保父节点 (HexMap) 存在此信号并连接
	var parent = get_parent()
	if parent and parent.has_signal("CreateBar"):
		parent.CreateBar.connect(Create_Blood_Bar)

func Create_Blood_Bar(landform_in : landform, situation : int, x : float , y : float):
	if _try_defer_for_map_intro(landform_in, situation, x, y):
		return

	print(str(landform_in.position) + ": 接受信号，制作血条中……")
	
	if HealthBar != null and situation < HealthBar.size():
		var HealthBuffer = HealthBar[situation].instantiate()
		
		
		# ==========================================
		# ★ 新增：纹理安全截断校验
		# 自动遍历内部节点，寻找 TextureProgressBar 并检查纹理
		# ==========================================
		var has_valid_texture = false
		for child in HealthBuffer.get_children():
			if child is TextureProgressBar:
				# 检查进度条的核心纹理是否加载成功
				if child.texture_progress != null:
					has_valid_texture = true
					break
		
		if not has_valid_texture:
			# 纹理缺失，直接静默销毁，中断后续所有绑定，防止报错刷屏
			# print("血条纹理缺失，取消生成：" + landform_in.landform_name)
			HealthBuffer.queue_free()
			return
		# ==========================================
		# ★ 新增：关闭血条及其所有子节点的鼠标拦截
		_set_mouse_ignore_recursive(HealthBuffer)
		HealthBuffer.z_index = 999
		if HealthBuffer is Control:
			HealthBuffer.set_anchors_preset(Control.PRESET_TOP_LEFT)
			HealthBuffer.anchor_right = 0.0
			HealthBuffer.anchor_bottom = 0.0
			HealthBuffer.size = health_bar_root_size
			HealthBuffer.custom_minimum_size = health_bar_root_size
			HealthBuffer.focus_mode = Control.FOCUS_NONE
			HealthBuffer.set_as_top_level(true)
		
		# 为血条节点设置唯一名称，方便 HexMap 查找
		HealthBuffer.name = "HealthBar_" + str(landform_in.get_instance_id())
		
		# 这里的 x, y 是 tile.gd 传过来的 global_position
		HealthBuffer.global_position = Vector2(x, y) + health_bar_world_offset
		HealthBuffer.scale = health_bar_scale
		
		# 传递初始化数据
		HealthBuffer.Show_name = landform_in.landform_name
		HealthBuffer.Max_HP = landform_in.Max_Blood
		
		# 添加到场景树
		self.add_child(HealthBuffer)
		
		# 连接生命值变化信号并立即初始化显示
		landform_in.Blood_change.connect(HealthBuffer.Blood_change_Handler)
		HealthBuffer.Blood_change_Handler(landform_in.HP)
		
		# ★ 核心修改：使用 call_deferred 延迟一帧注册！
		# 确保 HexMap 那边已经把地块完全存入字典后再进行收编
		if landform_in.owner_battle and landform_in.owner_battle.has_method("register_extra_render_node"):
			landform_in.owner_battle.call_deferred("register_extra_render_node", landform_in.location, HealthBuffer)
		
		landform_in.tree_exited.connect(HealthBuffer.queue_free)


## 地图初始涟漪入场期间不立刻生成血条，避免 UI 先于地块出现。
## 返回 true 表示本次请求已被 HexMap 缓存，动画完成后会重新调用 Create_Blood_Bar。
func _try_defer_for_map_intro(landform_in: landform, situation: int, x: float, y: float) -> bool:
	var parent = get_parent()
	if not is_instance_valid(parent):
		return false
	if not parent.has_method("should_defer_intro_health_bars"):
		return false
	if not parent.should_defer_intro_health_bars():
		return false
	if not parent.has_method("queue_intro_health_bar_request"):
		return false

	parent.queue_intro_health_bar_request(landform_in, situation, x, y)
	return true


## 统一显示/隐藏所有单体血条。
## 局外收获阶段不展示血条，但血条节点仍然保留在树上，
## 这样如果后续回到战斗态，可以直接恢复显示而不用重新生成。
func set_all_health_bars_visible(is_visible: bool) -> void:
	for child in get_children():
		if child == null:
			continue
		if child.name.begins_with("HealthBar_"):
			child.visible = is_visible

# ★ 新增：递归设置鼠标忽略函数
func _set_mouse_ignore_recursive(node: Node):
	if node is Control:
		node.mouse_filter = Control.MOUSE_FILTER_IGNORE
		node.focus_mode = Control.FOCUS_NONE
	for child in node.get_children():
		_set_mouse_ignore_recursive(child)
