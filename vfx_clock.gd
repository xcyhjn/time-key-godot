extends Sprite2D

@export var clock_target_scale: Vector2 = Vector2(1.5, 1.5)

@onready var shards = $"../VFX_GlassShards"
@onready var ashes = $"../VFX_Ashes"

signal clock_broken

func _ready():
	# 强力重置：确保场景加载瞬间没有任何粒子产生
	if shards:
		shards.emitting = false
		shards.one_shot = true # 确保碎片只喷一次
		shards.restart()
	if ashes:
		ashes.emitting = false
		ashes.restart()
	
	# 初始隐藏时钟，防止闪现
	hide()
	scale = Vector2.ZERO

## 执行时钟死亡序列
func play_death_sequence(slow_s: float):
	# 启动前双重保险：再次关闭粒子
	if shards: shards.emitting = false
	if ashes: ashes.emitting = false
	
	show()
	scale = Vector2.ZERO
	modulate.a = 1.0
	self_modulate = Color.WHITE 
	
	var tw = create_tween().set_parallel(false).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	
	# A. 弹出放大
	tw.tween_property(self, "scale", clock_target_scale, 0.8 * slow_s).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	
	# B. 变灰暗
	tw.tween_property(self, "self_modulate", Color(0.25, 0.25, 0.25, 1.0), 0.5 * slow_s)
	
	# C. 回调：只有在这里才准许破裂
	tw.tween_callback(_trigger_break_vfx)
	
	await tw.finished
	# 异步消失
	create_tween().set_pause_mode(Tween.TWEEN_PAUSE_PROCESS).tween_property(self, "modulate:a", 0.0, 0.1)

func _trigger_break_vfx():
	if shards:
		shards.restart()
		shards.emitting = true # 此时触发
	if ashes:
		ashes.restart()
		ashes.emitting = true # 此时触发
