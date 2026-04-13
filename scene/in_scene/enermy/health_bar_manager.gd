# 原文件名: health_bar_manager(未实装血条).gd
# 功能: 未实装血条管理器
class_name HealthBarManager
extends Control

## 血条管理器
## 管理总敌人血条的显示和闪烁效果

# ==========================================
# 信号
# ==========================================
signal total_health_changed(new_health: int, delta: int)
signal health_bar_flashed(amount: int)  ## 血条闪烁信号

# ==========================================
# 导出变量
# ==========================================
@export_group("血条设置")
@export var max_total_health: int = 100  ## 最大总生命值
@export var current_total_health: int = 100  ## 当前总生命值
@export var health_bar_path: NodePath  ## 血条UI节点路径
@export var health_text_path: NodePath  ## 生命值文本节点路径

# ==========================================
# 节点引用
# ==========================================
var health_bar: ProgressBar
var health_text: Label

# ==========================================
# 状态变量
# ==========================================
var is_flashing: bool = false
var flash_timer: Timer

# ==========================================
# 生命周期
# ==========================================


func _ready() -> void:
	# 获取节点引用
	if not health_bar_path.is_empty():
		health_bar = get_node(health_bar_path)
	if not health_text_path.is_empty():
		health_text = get_node(health_text_path)

	# 初始化血条
	_update_health_display()

	# 创建闪烁计时器
	flash_timer = Timer.new()
	flash_timer.wait_time = 0.15  # 闪烁间隔
	flash_timer.one_shot = true
	flash_timer.timeout.connect(_on_flash_timeout)
	add_child(flash_timer)

# ==========================================
# 血条管理
# ==========================================


## 更新总生命值
func update_total_health(new_health: int) -> void:
	var delta = new_health - current_total_health
	current_total_health = clamp(new_health, 0, max_total_health)

	# 更新显示
	_update_health_display()

	# 发射信号
	total_health_changed.emit(current_total_health, delta)

	# 如果受到伤害，触发闪烁效果
	if delta < 0:
		flash_health_bar(abs(delta))


## 增加生命值
func add_health(amount: int) -> void:
	update_total_health(current_total_health + amount)


## 减少生命值
func take_damage(amount: int) -> void:
	update_total_health(current_total_health - amount)


## 更新血条显示
func _update_health_display() -> void:
	if health_bar:
		health_bar.max_value = max_total_health
		health_bar.value = current_total_health

	if health_text:
		health_text.text = str(current_total_health) + "/" + str(max_total_health)

# ==========================================
# 闪烁效果
# ==========================================


## 血条闪烁效果
func flash_health_bar(damage_amount: int) -> void:
	if not health_bar:
		return

	# 触发闪烁信号
	health_bar_flashed.emit(damage_amount)

	# 开始闪烁效果
	is_flashing = true
	_start_flash_effect()


## 开始闪烁效果
func _start_flash_effect() -> void:
	if not health_bar:
		return

	# 闪烁效果：红色背景闪烁
	var original_style = health_bar.get_theme_stylebox("fill").duplicate()
	var flash_style = health_bar.get_theme_stylebox("fill").duplicate()

	# 设置闪烁颜色（红色）
	flash_style.bg_color = Color(1.0, 0.3, 0.3, 0.8)
	health_bar.add_theme_stylebox_override("fill", flash_style)

	# 启动闪烁计时器
	flash_timer.start()


## 闪烁计时器结束
func _on_flash_timeout() -> void:
	if not health_bar:
		return

	# 恢复原始样式
	var original_style = health_bar.get_theme_stylebox("fill")
	health_bar.add_theme_stylebox_override("fill", original_style)

	# 闪烁结束
	is_flashing = false

# ==========================================
# 卡牌效果预览相关
# ==========================================


## 预览伤害效果（在鼠标悬浮时调用）
func preview_damage_effect(damage_amount: int) -> void:
	if not health_bar:
		return

	# 显示预览伤害
	var preview_health = max(0, current_total_health - damage_amount)
	var preview_percentage = float(preview_health) / float(max_total_health)

	# 创建预览效果（淡红色覆盖）
	var preview_style = health_bar.get_theme_stylebox("fill").duplicate()
	preview_style.bg_color = Color(1.0, 0.5, 0.5, 0.6)  # 淡红色预览
	health_bar.add_theme_stylebox_override("fill", preview_style)

	# 更新预览文本
	if health_text:
		health_text.text = str(preview_health) + "(-" + str(damage_amount) + ")/" + str(max_total_health)


## 清除预览效果
func clear_preview_effect() -> void:
	if not health_bar:
		return

	# 恢复原始样式
	var original_style = health_bar.get_theme_stylebox("fill")
	health_bar.add_theme_stylebox_override("fill", original_style)

	# 恢复原始文本
	_update_health_display()
