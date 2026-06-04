# 功能: 时间轴中毒命令，负责把 poison 卡牌效果结算为建筑状态。
# 核心逻辑:
# - execute(): 遍历 EffectProcessor 注入的 target_tiles，找到地块 occupant 并施加中毒层数。
# - TimelineCommandTargetRules.can_apply_poison(): 过滤空地、已损毁建筑和不支持状态接口的对象。
# - 中毒动画由 landform.add_status() 触发，避免命令层同时关心状态逻辑和表现细节。
class_name PoisonCommand
extends EffectCommand

const TARGET_RULES := preload("res://scene/in_scene/timeline/commands/TimelineCommandTargetRules.gd")

var stacks: int = 0


## 初始化中毒命令。
## 负数层数会被压到 0，避免结算阶段添加异常状态。
func _init(new_stacks: int) -> void:
	stacks = maxi(0, new_stacks)


## 执行中毒效果。
## 目标必须通过 TimelineCommandTargetRules 的 add_status/HP 校验，状态表现仍由 landform.add_status() 负责。
func execute(tree: SceneTree) -> void:
	if stacks <= 0 or not is_instance_valid(hex_map) or target_tiles.is_empty():
		return

	var applied_any: bool = false
	for tile in target_tiles:
		if not is_instance_valid(tile):
			continue

		var entity := TARGET_RULES.get_occupant(tile)
		if not TARGET_RULES.can_apply_poison(entity):
			continue

		entity.add_status(StatusDB.POISON_ID, stacks, tile, tree)
		applied_any = true

	if applied_any:
		await tree.create_timer(0.35).timeout
