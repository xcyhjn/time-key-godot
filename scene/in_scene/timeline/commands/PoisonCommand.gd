# 功能: 时间轴中毒命令，负责把 poison 卡牌效果结算为建筑状态。
# 核心逻辑:
# - execute(): 遍历 EffectProcessor 注入的 target_tiles，找到地块 occupant 并施加中毒层数。
# - _can_apply_poison(): 过滤空地、已损毁建筑和不支持状态接口的对象。
# - 中毒动画由 landform.add_status() 触发，避免命令层同时关心状态逻辑和表现细节。
class_name PoisonCommand
extends EffectCommand

var stacks: int = 0


func _init(new_stacks: int) -> void:
	stacks = maxi(0, new_stacks)


func execute(tree: SceneTree) -> void:
	if stacks <= 0 or not is_instance_valid(hex_map) or target_tiles.is_empty():
		return

	var applied_any: bool = false
	for tile in target_tiles:
		if not is_instance_valid(tile):
			continue

		var entity: Node = tile.get_meta("occupant") if tile.has_meta("occupant") else null
		if not _can_apply_poison(entity):
			continue

		entity.add_status(StatusDB.POISON_ID, stacks, tile, tree)
		applied_any = true

	if applied_any:
		await tree.create_timer(0.35).timeout


func _can_apply_poison(entity: Node) -> bool:
	if not is_instance_valid(entity):
		return false
	if not entity.has_method("add_status"):
		return false
	var hp: Variant = entity.get("HP")
	return hp == null or float(hp) > 0.0
