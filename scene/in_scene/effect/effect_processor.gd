# 路径: scene/in_scene/timeline/EffectProcessor.gd
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
