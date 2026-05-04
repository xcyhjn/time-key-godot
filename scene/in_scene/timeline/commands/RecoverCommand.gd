# 功能: 时间轴回复命令，负责在结算 recover 类型卡牌效果时为目标地貌恢复生命值。
# 核心逻辑: 遍历 EffectProcessor 注入的 target_tiles，先播放 HexMap 的回复特效，再调用实体 heal(amount)。
class_name RecoverCommand
extends EffectCommand

var amount: int


func _init(amt: int) -> void:
	amount = maxi(0, amt)


func execute(tree: SceneTree) -> void:
	if amount <= 0 or not is_instance_valid(hex_map) or target_tiles.is_empty():
		return

	var recovered_anyone: bool = false
	for tile in target_tiles:
		if not is_instance_valid(tile):
			continue

		var entity: Node = tile.get_meta("occupant") if tile.has_meta("occupant") else null
		if not _can_recover_entity(entity):
			continue

		if hex_map.has_method("play_recover_effect_on_tile"):
			hex_map.play_recover_effect_on_tile(tile)

		entity.heal(amount)
		recovered_anyone = true

	if recovered_anyone:
		await tree.create_timer(0.3).timeout


func _can_recover_entity(entity: Node) -> bool:
	if not is_instance_valid(entity) or not entity.has_method("heal"):
		return false

	var hp: Variant = entity.get("HP")
	var max_hp: Variant = entity.get("Max_Blood")
	if hp != null and max_hp != null:
		return float(hp) < float(max_hp)

	return true
