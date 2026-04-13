extends CanvasLayer

enum Timer_signal
{
	up = -1,
	toggle_ani_vis = -2,
	toggle_ani_invis = -3,
	up_test = -4
}

signal Ani_call(Name, Frame)

func _ready() -> void:
	Ani_call.connect(Ani_call_Handler)


func Ani_call_Handler(Name, Frame):
	#print("信号触发！正在唤起……" + Name + "  并传入信号：" + str(Frame))
	if Name == "Timer":
		Timer_call_handler(Frame)

func Timer_call_handler(Frame):
	if Frame >= 0 and Frame < 8:
		$Timer.frame = Frame
	elif Frame == Timer_signal.up:
		$Timer.Frame_Up(true)
	elif Frame == Timer_signal.toggle_ani_vis:
		$Timer.Toggle_modulate(false)
	elif Frame == Timer_signal.toggle_ani_invis:
		$Timer.Toggle_modulate(true)
	elif Frame == Timer_signal.up_test:
		$Timer.Frame_Up(false)
	else:
		push_error("无效的timerUI传参！")
