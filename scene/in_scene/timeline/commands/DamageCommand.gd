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

var amount: int

func _init(amt: int):
	self.amount = amt

func execute(tree: SceneTree) -> void:
	if not is_instance_valid(hex_map) or not is_instance_valid(target_tile):
		return
		
	# 1. 寻址与安全校验：找目标地块上到底站着什么
	# 假设 target_tile 有一个记录六边形坐标的方法或属性，或者直接使用 global_position 倒推
	var entity = null
	if target_tile.has_meta("occupant"):
		entity = target_tile.get_meta("occupant")
	
	# 2. 存活校验：防止对空气输出
	if is_instance_valid(entity) and entity.has_method("take_damage"):
		if entity.get("HP") != null and entity.HP > 0:
			GameLogger.info("执行伤害命令：造成 %d 点伤害" % amount, "DamageCommand")
			
			# 3. 动画解耦：播放打击特效并等待
			await VFXManager.play_vfx("slash", target_tile.global_position, tree)
			
			# 4. 实际扣血与UI刷新
			entity.take_damage(amount)
			if Engine.has_singleton("SignalBus"):
				Signal_Bus.emit_damage_dealt(entity, amount)
				
			# 受击后的停顿顿帧感
			await tree.create_timer(0.15).timeout
		else:
			GameLogger.debug("目标已死亡，伤害无效化", "DamageCommand")
	else:
		GameLogger.debug("目标地块没有可被攻击的实体", "DamageCommand")
