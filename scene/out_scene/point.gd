extends Node2D

enum State { STOP, TRACKING, SPINNING, DECELERATING, SF, SD }
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
			if spin_velocity > rotation_speed:
				spin_velocity -= spin_acceleration * delta
				rotation -= spin_velocity * delta
			else:
				var target_angle = 0.0
				var angle_diff = abs(angle_difference(rotation, target_angle))
				if angle_diff < 0.01:
					rotation = target_angle
					spin_velocity = 0.0
					current_state = State.STOP
					stopped.emit()
				else:
					rotation = lerp_angle(rotation, target_angle, 5.0 * delta)
					spin_velocity = lerp(spin_velocity, 0.0, 5.0 * delta)
					
		State.SF:
			spin_velocity += spin_acceleration * delta
			rotation += spin_velocity * delta
			
		State.SD:
			if spin_velocity > rotation_speed:
				spin_velocity -= spin_acceleration * delta
				rotation += spin_velocity * delta
			else:
				var target_angle = 0.0
				var angle_diff = abs(angle_difference(rotation, target_angle))
				if angle_diff < 0.01:
					rotation = target_angle
					spin_velocity = 0.0
					current_state = State.STOP
					stopped.emit()
				else:
					rotation = lerp_angle(rotation, target_angle, 5.0 * delta)
					spin_velocity = lerp(spin_velocity, 0.0, 5.0 * delta)

func change(new_state: int) -> void:
	current_state = new_state as State
	if current_state == State.TRACKING:
		spin_velocity = 0.0
