# ==========================================
# 脚本名称: EffectProcessor.gd
# 功能概述: 时间轴结算的核心枢纽，负责将卡牌/敌人的静态数据转化为可执行的动态命令队列。
# ------------------------------------------
# 【数据接收】
# - 来源: TimelineManager (在 resolve_timeline 协程中调用)。
# - 内容: TimelineAction 对象（包含卡牌的原始 JSON 数据 action_data、释放者 source_node、目标地块 target_tile）以及 SceneTree。
# ------------------------------------------
# 【数据处理】
# - 逻辑: 解析卡牌/敌人的 JSON 数据。将 effects 数组转换为结构化的 EffectCommand (如 DamageCommand) 实例，并组装成一个命令队列 (Array)。
# ------------------------------------------
# 【数据发送】
# - 目标: 各种具体的 EffectCommand (例如 DamageCommand)。
# - 时机: 在解析完成后，立刻通过 for 循环依次 await 调用各个 Command 的 execute() 方法。
# ==========================================
class_name EffectProcessor
extends RefCounted

## 核心入口：处理时间轴上的行动
static func process_action(action: TimelineAction, tree: SceneTree) -> void:
	var command_queue: Array[EffectCommand] = []
	
	if action.type == TimelineAction.Type.PLAYER:
		command_queue = _parse_player_card(action, tree)
	elif action.type == TimelineAction.Type.ENEMY:
		command_queue = _parse_enemy_intent(action, tree)
		
	# 队列按顺序执行 (Command Pattern 的核心优势)
	for cmd in command_queue:
		await cmd.execute(tree)

## 解析玩家卡牌 JSON 数据，生成命令队列
static func _parse_player_card(action: TimelineAction, tree: SceneTree) -> Array[EffectCommand]:
	var queue: Array[EffectCommand] = []
	var data = action.action_data
	var map_node = tree.current_scene.get_node_or_null("map/HexMap") # 根据你的场景树获取
	
	# --- 方式A: 使用新的结构化 effects 数组 (推荐) ---
	if data.has("effects") and typeof(data["effects"]) == TYPE_ARRAY:
		for eff in data["effects"]:
			if eff["type"] == "damage":
				var cmd = DamageCommand.new(eff["value"])
				cmd.source = action.source_node
				cmd.target_tile = action.target_tile
				cmd.hex_map = map_node
				queue.append(cmd)
			# 此处可以扩展 elif eff["type"] == "heal" 等
			
	# --- 方式B: 兼容老版本，正则/字符串暴力解析你的 "造成1点伤害" ---
	else:
		pass
	return queue

## 解析敌方意图，生成命令队列
static func _parse_enemy_intent(action: TimelineAction, tree: SceneTree) -> Array[EffectCommand]:
	var queue: Array[EffectCommand] = []
	# 敌人的行为解析逻辑（例如：扩张、攻击）
	# TODO: 返回相应的 EnemyExpandCommand, EnemyAttackCommand 等
	return queue
