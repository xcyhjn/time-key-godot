extends Node2D

enum State { STOP, TRACKING, SPINNING, DECELERATING }
@export var rotation_speed : float = 20.0
@export var spin_acceleration : float = 12.0
static var current_state : State = State.TRACKING
static var spin_velocity : float = 0.0

signal stopped

func _ready() -> void:
	if Global.clock:
		Global.clock.connect(change)

func _process(delta: float) -> void:
	match current_state:
		State.STOP:
			return

		State.TRACKING:
			var target_direction = get_global_mouse_position() - global_position
			var target_angle = target_direction.angle() + PI/2
			rotation = lerp_angle(rotation, target_angle, rotation_speed * delta)
			
		State.SPINNING:
			spin_velocity += spin_acceleration * delta
			rotation -= spin_velocity * delta
			
		State.DECELERATING:
			# 阶段 1：高速物理减速
			if spin_velocity > rotation_speed:
				spin_velocity -= spin_acceleration * delta
				rotation -= spin_velocity * delta
			else:
				# 阶段 2：精准归位阶段
				var target_angle = 0.0 # 初始状态角度
				
				# 计算当前角度与目标的差距（使用此函数可处理跨越 360 度的旋转）
				var angle_diff = abs(angle_difference(rotation, target_angle))
				
				# 如果差距已经极小，则直接锁定并停止
				if angle_diff < 0.01:
					rotation = target_angle
					spin_velocity = 0.0
					current_state = State.STOP
					stopped.emit()
				else:
					# 使用 lerp_angle 实现“缓慢吸附”效果
					# 这里的 5.0 是平滑系数，数值越大吸附越快
					rotation = lerp_angle(rotation, target_angle, 5.0 * delta)
					# 同时也让残余速度视觉化（可选，让指针看起来还有点惯性）
					spin_velocity = lerp(spin_velocity, 0.0, 5.0 * delta)

func change(new_state: int) -> void:
	current_state = new_state as State
	if current_state == State.TRACKING:
		spin_velocity = 0.0
