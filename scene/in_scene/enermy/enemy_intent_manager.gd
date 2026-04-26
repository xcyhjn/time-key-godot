extends Node

# 敌人意图管理器
# 负责敌人意图的生成、管理和可视化
# 与 TimelineManager 协同工作，专门处理敌人相关的逻辑

class_name EnemyIntentManager

@export_group("意图生成设置")
@export var max_intents_per_turn: int = 5  # 每回合最多生成多少个敌人意图
@export var intent_generation_delay: float = 0.5  # 意图生成延迟（秒）
@export var show_intent_preview: bool = true  # 是否显示意图预览

@export_group("视觉效果设置")
@export var enemy_intent_color: Color = Color(0.8, 0.2, 0.2, 0.8)  # 敌人意图颜色（红色系）
@export var intent_preview_duration: float = 2.0  # 意图预览显示时长
@export var intent_flash_speed: float = 3.0  # 意图闪烁速度

# 节点引用
@onready var timeline_manager: TimelineManager = get_parent().find_child("TimelineManager") if get_parent() else null
@onready var timeline_ui: Control = get_tree().root.find_child("TimelineUI", true, false) if get_tree() else null

# 状态变量
var current_intents: Array[TimelineAction] = []  # 当前回合的敌人意图
var is_generating_intents: bool = false  # 是否正在生成意图
var intent_generation_timer: float = 0.0  # 意图生成计时器
var pending_enemies: Array = []  # 等待生成意图的敌人列表

# 信号定义
signal intents_generated(intents: Array[TimelineAction])  # 意图生成完成
signal intent_preview_created(intent: TimelineAction)  # 意图预览创建
signal intent_preview_removed(intent: TimelineAction)  # 意图预览移除
signal enemy_target_changed(enemy: Node, target: Node)  # 敌人目标变化

# ==========================================
# ★ 核心功能：敌人意图生成
# ==========================================


## 开始生成敌人意图
func generate_enemy_intents(enemies: Array) -> void:
	if is_generating_intents:
		return

	if not timeline_manager:
		return


	# 清空当前意图
	clear_current_intents()

	# 设置状态
	is_generating_intents = true
	intent_generation_timer = 0.0
	pending_enemies = enemies.duplicate()
	pending_enemies.shuffle()  # 随机化顺序

	# 开始生成过程
	_generate_next_intent()


## 生成下一个敌人意图（递归调用）
func _generate_next_intent() -> void:
	if not is_generating_intents:
		return

	# 检查是否达到上限或没有更多敌人
	if current_intents.size() >= max_intents_per_turn or pending_enemies.is_empty():
		_finish_intent_generation()
		return

	# 取出下一个敌人
	var enemy = pending_enemies.pop_back()
	if not is_instance_valid(enemy):
		_generate_next_intent()  # 跳过无效敌人
		return

	# 检查敌人是否有意图接口
	if not enemy.has_method("get_intent_shape") or not enemy.has_method("get_intent_action"):
		_generate_next_intent()
		return

	# 获取敌人意图形状
	var shape = enemy.get_intent_shape()

	# 在时间轴中寻找可用位置
	var spot = timeline_manager.find_random_available_spot(shape)

	if spot == Vector2i(-1, -1):
		_generate_next_intent()
		return

	# 获取敌人目标（这里简化，实际应调用敌人的索敌逻辑）
	var target = _find_enemy_target(enemy)

	# 获取完整的意图行动
	var intent_action = enemy.get_intent_action(target)
	intent_action.color = enemy_intent_color  # 应用敌人专用颜色

	# 添加到当前意图列表
	current_intents.append(intent_action)

	# 在时间轴中放置意图（预览模式）
	if show_intent_preview:
		_create_intent_preview(intent_action, spot)

	# 延迟后生成下一个意图
	var timer = get_tree().create_timer(intent_generation_delay)
	timer.timeout.connect(_generate_next_intent)



## 完成意图生成
func _finish_intent_generation() -> void:
	is_generating_intents = false


	# 发射完成信号
	intents_generated.emit(current_intents)

	# 实际放置到时间轴（如果预览模式开启，则需要替换为实际放置）
	if not show_intent_preview:
		_place_intents_to_timeline()
	else:
		# 预览模式下，显示一段时间后实际放置
		var timer = get_tree().create_timer(intent_preview_duration)
		timer.timeout.connect(_replace_previews_with_real_intents)


## 将预览意图替换为实际意图
func _replace_previews_with_real_intents() -> void:
	_clear_intent_previews()
	_place_intents_to_timeline()


## 实际放置意图到时间轴
func _place_intents_to_timeline() -> void:
	if not timeline_manager:
		return

	for intent in current_intents:
		var success = timeline_manager.place_action(intent, intent.origin_grid_pos)
		if success:
			pass
		else:
			pass

# ==========================================
# ★ 意图预览系统
# ==========================================


## 创建意图预览
func _create_intent_preview(intent: TimelineAction, grid_pos: Vector2i) -> void:
	if not timeline_ui:
		return

	# 设置意图的原始位置
	intent.origin_grid_pos = grid_pos

	# 这里可以创建自定义的预览效果
	# 例如：半透明的红色方块，带有闪烁效果

	intent_preview_created.emit(intent)

	# TODO: 实际创建可视化预览对象
	# 可以创建一个特殊的 Control 节点添加到 TimelineUI


## 清除所有意图预览
func _clear_intent_previews() -> void:

	# TODO: 实际清除可视化预览对象

	# 发射信号
	for intent in current_intents:
		intent_preview_removed.emit(intent)

# ==========================================
# ★ 辅助功能
# ==========================================


## 清空当前所有意图
func clear_current_intents() -> void:
	_clear_intent_previews()
	current_intents.clear()



## 查找敌人目标（简化版）
func _find_enemy_target(enemy: Node) -> Node:
	# 这里实现你的索敌逻辑
	# 例如：寻找最近的玩家建筑、血量最低的单位等

	# 简化：返回空，由敌人自己的 get_intent_action 处理
	return null


## 获取当前回合的敌人意图数量
func get_current_intent_count() -> int:
	return current_intents.size()


## 检查是否正在生成意图
func is_generating() -> bool:
	return is_generating_intents


## 强制停止意图生成
func stop_intent_generation() -> void:
	if is_generating_intents:
		is_generating_intents = false
		pending_enemies.clear()
		_clear_intent_previews()

# ==========================================
# ★ 生命周期方法
# ==========================================


func _ready() -> void:

	# 尝试自动查找 TimelineManager
	if not timeline_manager and get_parent():
		timeline_manager = get_parent().find_child("TimelineManager")

	if timeline_manager:
		pass
	else:
		pass


func _process(delta: float) -> void:
	# 处理意图生成计时
	if is_generating_intents:
		intent_generation_timer += delta

		# 安全保护：如果生成过程卡住，超时强制完成
		if intent_generation_timer > 10.0:  # 10秒超时
			_finish_intent_generation()
