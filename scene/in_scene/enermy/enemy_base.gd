class_name EnemyBase
extends Node2D

# ==========================================
# ★ 核心魔法：导出变量 (Export)
# 这些变量会自动出现在所有子场景的属性面板里，供你随意调整！
# ==========================================
@export_group("基础属性")
@export var enemy_name: String = "未知生物"
@export var max_hp: int = 10
@export var attack_power: int = 2

var current_hp: int


func _ready() -> void:
	add_to_group("Enemies") # ★ 新增：实体敌人也自动加入组
	current_hp = max_hp


# 通用受伤逻辑
func take_damage(amount: int) -> void:
	current_hp -= amount
	# 这里可以播放通用的受伤动画，比如 $AnimationPlayer.play("hurt")
	if current_hp <= 0:
		die()



func die() -> void:
	queue_free()
# 虚函数：专用逻辑（留给子类重写）
func execute_unique_action():
	pass

# ==========================================
# ★ 新增：卡牌效果预览相关功能
# ==========================================


## 显示卡牌效果预览（敌人变红效果）
func show_card_effect_preview(damage_amount: int = 0) -> void:
	# 应用变红Shader效果
	if has_method("_apply_red_effect"):
		_apply_red_effect()

	# 显示伤害预览
	if damage_amount > 0:
		_show_damage_preview(damage_amount)


## 清除卡牌效果预览
func clear_card_effect_preview() -> void:
	# 清除变红效果
	if has_method("_clear_red_effect"):
		_clear_red_effect()

	# 清除伤害预览
	if has_method("_clear_damage_preview"):
		_clear_damage_preview()


## 应用变红效果（子类实现）
func _apply_red_effect() -> void:
	# 默认实现：修改材质颜色或应用Shader
	if material:
		material.set_shader_parameter("effect_color", Color.RED)
		material.set_shader_parameter("effect_intensity", 0.7)  # 70%的红色效果强度


## 清除变红效果（子类实现）
func _clear_red_effect() -> void:
	# 默认实现：恢复原始颜色
	if material:
		material.set_shader_parameter("effect_color", Color.WHITE)
		material.set_shader_parameter("effect_intensity", 0.0)  # 清除效果


## 显示伤害预览（子类实现）
func _show_damage_preview(amount: int) -> void:
	# 默认实现：在敌人上方显示伤害数字
	pass


## 清除伤害预览（子类实现）
func _clear_damage_preview() -> void:
	# 默认实现：清除伤害数字显示
	pass
