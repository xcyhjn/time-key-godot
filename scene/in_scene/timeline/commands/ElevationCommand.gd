# ==========================================
# 脚本名称: ElevationCommand.gd
# 功能概述: 地形升降执行命令，负责调度地块的高度更改和特效等待。
# ==========================================
class_name ElevationCommand extends EffectCommand

var elevation_value: int

func _init(amt: int):
	self.elevation_value = amt
func execute(tree: SceneTree) -> void:
	if not is_instance_valid(hex_map) or target_tiles.is_empty():
		return
		
	
	if hex_map.has_method("animate_elevation_change"):
		# 1. 并行触发：循环调用所有地块的升降动画
		# 此时我们不使用 await，让它们瞬间在同一帧内同时开始抖动和升起
		for tile in target_tiles:
			if is_instance_valid(tile):
				hex_map.animate_elevation_change(tile, elevation_value)
		
		# 2. 统一阻塞：由于动画都在并行，我们只需让时间轴等待一个固定时间。
		# 你的 hexmap 里震动0.2秒 + 升降0.4秒 = 0.6秒。
		# 为了视觉稳定感，等待 0.8 秒后，再执行卡牌的下一个效果（比如伤害）。
		await tree.create_timer(0.8).timeout
	else:
		pass
