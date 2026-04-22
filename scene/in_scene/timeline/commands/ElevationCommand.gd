# ==========================================
# 脚本名称: ElevationCommand.gd
# 功能概述: 地形升降执行命令，负责调度地块的高度更改和特效等待。
# ==========================================
class_name ElevationCommand extends EffectCommand

var elevation_value: int

func _init(amt: int):
	self.elevation_value = amt

func execute(tree: SceneTree) -> void:
	# 校验地块和地图是否存在
	if not is_instance_valid(hex_map) or not is_instance_valid(target_tile):
		GameLogger.warning("地块升降失败，目标地块或地图实例已丢失", "ElevationCommand")
		return
		
	GameLogger.info("执行地形改造命令：变化值 %d" % elevation_value, "ElevationCommand")
	
	# 确认地图中有你编写好的动画方法
	if hex_map.has_method("animate_elevation_change"):
		# 由于我们刚刚在 animate_elevation_change 最后加了 await，这里也能用 await 挂起时间轴
		await hex_map.animate_elevation_change(target_tile, elevation_value)
	else:
		GameLogger.error("HexMap 缺少 animate_elevation_change 方法！", "ElevationCommand")
		
	# 如果有通用的烟尘粒子特效，也可以在此时调用 VFXManager
	# await VFXManager.play_vfx("earth_shake", target_tile.global_position, tree)
