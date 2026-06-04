# ==========================================
# 脚本名称: DamageCommand.gd
# 功能概述: 具体的伤害执行命令，负责寻址、存活校验、特效播放与实际扣血。
# ------------------------------------------
# 【数据接收】
# - 来源: EffectProcessor (实例化时传入伤害值 amount)；HexMap (执行时请求获取目标地块上的实体信息)。
# - 内容: 伤害数值 (amount)，以及目标地块上的实体对象 (entity)。
# ------------------------------------------
# 【数据处理】
# - 逻辑: 验证目标实体是否存在且存活 (HP > 0)。如果存活，则协同 VFXManager 播放受击动画，然后调用实体的 take_damage() 扣除血量。
# ------------------------------------------
# 【数据发送】
# - 目标: 目标实体 (发送扣血指令)；VFXManager (发送播放特效指令)；SignalBus (发送 damage_dealt 广播信号以刷新UI)。
# - 时机: 在自身的 execute() 方法被 EffectProcessor 的协程唤起时按顺序发送。
# ==========================================
class_name DamageCommand extends EffectCommand

const TARGET_RULES := preload("res://scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd")

var amount: int

## 初始化伤害命令。
## amount 由 EffectProcessor 从卡牌 effects.value 中传入。
func _init(amt: int):
	self.amount = amt

## 执行伤害效果。
## 每个目标地块先通过 TimelineCommandTargetRules 获取并校验 occupant，再播放受击 VFX 和扣血。
func execute(tree: SceneTree) -> void:
	if not is_instance_valid(hex_map) or target_tiles.is_empty():
		return
		
	
	var hit_anyone = false
	for tile in target_tiles:
		if not is_instance_valid(tile): continue
		var entity := TARGET_RULES.get_occupant(tile)
		
		if TARGET_RULES.can_damage_entity(entity):
			VFXManager.play_hurt_vfx(entity, tree)
			entity.take_damage(amount)
			if Signal_Bus and Signal_Bus.has_method("emit_damage_dealt"):
				Signal_Bus.emit_damage_dealt(entity, amount)

			hit_anyone = true
	
	# 如果范围内有敌人挨打了，稍微顿帧一下增加打击感
	if hit_anyone:
		await tree.create_timer(0.3).timeout
