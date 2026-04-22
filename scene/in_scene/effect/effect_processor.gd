# ==========================================
# 脚本名称: EffectProcessor.gd
# 功能概述: 时间轴结算的核心枢纽，负责将卡牌/敌人的静态数据转化为可执行的动态命令队列。
# ------------------------------------------
# 【数据接收】
# - 来源: TimelineManager (在 resolve_timeline 协程中调用)。
# - 内容: TimelineAction 对象（包含卡牌的原始 JSON 数据 action_data、释放者 source_node、目标地块 target_tile）以及 SceneTree。

# 功能概述: 时间轴结算的核心枢纽，使用工厂模式解耦效果的创建与依赖注入
# ==========================================
class_name EffectProcessor
extends RefCounted

## 优化版：支持多命令完美的并行执行 (Fire and Forget 模式)
static func process_action(action: TimelineAction, tree: SceneTree) -> void:
	var command_queue: Array[EffectCommand] = []
	
	if action.type == TimelineAction.Type.PLAYER:
		command_queue = _parse_player_card(action, tree)
	elif action.type == TimelineAction.Type.ENEMY:
		command_queue = _parse_enemy_intent(action, tree)
		
	if command_queue.is_empty():
		return
		
	# --- Godot 4 并行执行改进 ---
	for cmd in command_queue:
		cmd.execute(tree)
		
	# 由于我们上面没有去 await 它们，时间轴不知道它们什么时候播完。
	# TODO 我们在此处统一让时间轴等待一个固定的时间（例如整个动画耗时 1.2 秒），
	# 等动画集体播完后，时间轴再移动到下一格。
	await tree.create_timer(1.2).timeout

## 解析玩家卡牌 JSON 数据，生成命令队列
static func _parse_player_card(action: TimelineAction, tree: SceneTree) -> Array[EffectCommand]:
	var queue: Array[EffectCommand] = []
	var data = action.action_data
	var map_node = tree.current_scene.get_node_or_null("map/HexMap") 
	
	if data.has("effects") and typeof(data["effects"]) == TYPE_ARRAY:
		for eff in data["effects"]:
			# 1. 工厂生产模块：仅根据关键词生成对应的基础命令实例
			var cmd = _create_command_from_type(eff)
			
			if cmd != null:
				# 2. 统一注入模块：把重复的参数打包赋值
				_inject_common_dependencies(cmd, action, map_node)
				queue.append(cmd)
	return queue

## 【工厂方法】根据 JSON 里的 type 返回对应的实体对象
static func _create_command_from_type(eff: Dictionary) -> EffectCommand:
	if not eff.has("type"): 
		return null
		
	var type = eff["type"]
	var value = eff.get("value", 0) # 如果没有value默认给0
	
	match type:
		"damage":
			return DamageCommand.new(value)
		"elevation":
			return ElevationCommand.new(value)
		# 未来如果要加回血，只需要在这里加两行：
		# "heal":
		#     return HealCommand.new(value)
		_:
			GameLogger.warning("未知的效果类型: " + str(type), "EffectProcessor")
			return null

## 【依赖注入】把各个指令所需的相同基础环境统一赋值
static func _inject_common_dependencies(cmd: EffectCommand, action: TimelineAction, map_node: Node) -> void:
	cmd.source = action.source_node
	cmd.target_tile = action.target_tile
	cmd.hex_map = map_node

## 解析敌方意图，生成命令队列 (同理可以应用上述优化)
static func _parse_enemy_intent(action: TimelineAction, tree: SceneTree) -> Array[EffectCommand]:
	var queue: Array[EffectCommand] = []
	# 敌人的行为解析逻辑
	return queue
