extends "enemy_base.gd"
class_name grass

# 草敌人特定属性
@export_group("草敌人属性")
@export var regen_rate: float = 1.0  # 生命恢复速率

func _ready() -> void:
	super._ready()

# 草敌人特有的行为
func regen_health() -> void:
	if current_hp < max_hp:
		current_hp = min(current_hp + 1, max_hp)

# 可以添加草敌人的特殊能力
func special_ability() -> void:
	# 实现草敌人的特殊能力逻辑
