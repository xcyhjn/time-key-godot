extends Control
class_name BarManager

var HealthBar : Array[PackedScene] = [preload("res://scene/in_scene/enermy/health_bar_middle.tscn"),
									preload("res://scene/in_scene/enermy/health_bar_enemy.tscn")]
var BarArray : Array[Node]

func _ready() -> void:
	get_parent().CreateBar.connect(Create_Blood_Bar)
	
# Called when the node enters the scene tree for the first time.
func Create_Blood_Bar(landform_in : landform, situation : int, x : float , y : float):
	print(str(landform_in.position) + ": 接受信号，制作血条中……")
	if HealthBar != null:
		var HealthBuffer = HealthBar[situation].instantiate()
		HealthBuffer.z_index = 999
		# ★ 新增：为血条节点设置唯一名称或元数据，方便 HexMap 查找
		HealthBuffer.name = "HealthBar_" + str(landform_in.get_instance_id())
		HealthBuffer.set_meta("owner_landform", landform_in)
		# 这里的 x, y 是 tile.gd 传过来的 global_position
		 	# 如果 BarManager 也是 Node2D，则直接设置 position
		HealthBuffer.global_position = Vector2(x, y)
		HealthBuffer.scale = Vector2(2.5, 2.5)
		#print("BloodBar装载" + str(HealthBuffer.get_child(0)))
		landform_in.Blood_change.connect(HealthBuffer.Blood_change_Handler)
		HealthBuffer.Show_name = landform_in.landform_name
		HealthBuffer.Max_HP = landform_in.Max_Blood
		self.add_child(HealthBuffer)
		# ★ 新增：由于血条是异步生成的，我们需要通知 HexMap 重新收集一次该地块的精灵
		if landform_in.owner_battle:
			landform_in.owner_battle.call_deferred("recollect_sprites_for_landform", landform_in)
