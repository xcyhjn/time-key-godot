extends Node2D
class_name BarManager

var HealthBar : Array[PackedScene] = [
	preload("res://scene/in_scene/enermy/health_bar_middle.tscn"),
	preload("res://scene/in_scene/enermy/health_bar_enemy.tscn")
]

func _ready() -> void:
	# 确保父节点 (HexMap) 存在此信号并连接
	var parent = get_parent()
	if parent and parent.has_signal("CreateBar"):
		parent.CreateBar.connect(Create_Blood_Bar)

func Create_Blood_Bar(landform_in : landform, situation : int, x : float , y : float):
	print(str(landform_in.position) + ": 接受信号，制作血条中……")
	if HealthBar != null and situation < HealthBar.size():
		var HealthBuffer = HealthBar[situation].instantiate()
		HealthBuffer.z_index = 999
		
		# 为血条节点设置唯一名称，方便 HexMap 查找
		HealthBuffer.name = "HealthBar_" + str(landform_in.get_instance_id())
		
		# 这里的 x, y 是 tile.gd 传过来的 global_position
		HealthBuffer.global_position = Vector2(x, y)
		HealthBuffer.scale = Vector2(2.5, 2.5)
		
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
