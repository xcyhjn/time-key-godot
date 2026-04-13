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
		HealthBuffer.position = Vector2(x , y)
		HealthBuffer.scale = Vector2(2.5, 2.5)
		#print("BloodBar装载" + str(HealthBuffer.get_child(0)))
		landform_in.Blood_change.connect(HealthBuffer.Blood_change_Handler)
		HealthBuffer.Show_name = landform_in.landform_name
		HealthBuffer.Max_HP = landform_in.Max_Blood
		self.add_child(HealthBuffer)
