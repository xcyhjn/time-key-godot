extends Control

var Show_name : String
var Max_HP : float

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	#print("场景搭载成功！")
	pass

func Blood_change_Handler(Blood : float):
	$MiddleHealth.value = Blood / Max_HP * 100
	$MiddleInfo.text = Show_name + " : " + str(int(Blood)) + "/" + str(int(Max_HP))

func free_handler():
	queue_free()
