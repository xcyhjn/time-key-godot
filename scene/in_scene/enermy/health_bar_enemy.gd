extends Control

var Show_name : String
var Max_HP : float

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


func Blood_change_Handler(Blood : int):
	$EnemyHealth.value = Blood / Max_HP  * 100
	$EnemyInfo.text = Show_name + " : " + str(int(Blood)) + "/" + str(int(Max_HP))

func free_handler():
	queue_free()
	
