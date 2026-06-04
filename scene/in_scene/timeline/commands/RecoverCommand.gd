# 功能: 时间轴回复命令，负责在结算 recover 类型卡牌效果时为目标地貌恢复生命值。
# 核心逻辑: 遍历 EffectProcessor 注入的 target_tiles，触发 VFXManager 的 recover 地块特效，再调用实体 heal(amount)。
class_name RecoverCommand
extends EffectCommand

const TARGET_RULES := preload("res://scene/in_scene/timeline/commands/TimelineCommandTargetRules.gd")

var amount: int


## 初始化回复命令。
## 负数回复量会被压到 0，避免结算阶段反向扣血。
func _init(amt: int) -> void:
	amount = maxi(0, amt)


## 执行回复效果。
## 目标必须通过 TimelineCommandTargetRules 的 heal/HP 校验，命令本身只负责 VFX 和调用 heal()。
func execute(tree: SceneTree) -> void:
	if amount <= 0 or not is_instance_valid(hex_map) or target_tiles.is_empty():
		return

	var recovered_anyone: bool = false
	for tile in target_tiles:
		if not is_instance_valid(tile):
			continue

		var entity := TARGET_RULES.get_occupant(tile)
		if not TARGET_RULES.can_recover_entity(entity):
			continue

		VFXManager.play_tile_vfx(&"recover", tile, tree)
		entity.heal(amount)
		recovered_anyone = true

	if recovered_anyone:
		await tree.create_timer(0.3).timeout
